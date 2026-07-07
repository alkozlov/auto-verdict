# Pipeline & Billing Hardening — Design

**Date:** 2026-07-06 · **Status:** Implemented

## Problem

Four defects from the 2026-07-03 technical audit:
1. Any pipeline failure NAKs the JetStream message, which redelivers immediately up to `MaxDeliver=5`, re-running the full AI pipeline each time — up to ~10 EUR of Claude spend for one doomed check, with no backoff and no distinction between permanent and transient failures. Every intermediate attempt also marks the check Failed and publishes a failure event.
2. `ClaudeAiClient` has no retry for transient API errors (429/529/5xx/transport) — a single blip fails the whole pipeline into defect 1's loop.
3. The credit is deducted at completion (`RecordSuccessAsync`); a user with 0 balance at that moment turns a successful, already-paid-for-in-AI-costs report into a failure.
4. The LemonSqueezy webhook handler is ~90 untestable lines inline in `Program.cs`; a duplicate delivery that races the idempotency check dies on the (existing) unique index with a 500 instead of an idempotent success. There are zero tests for any billing path.

## Decisions (made with user)

- **Scope: pipeline/billing only.** Auth follow-ups (token cleanup job, rotate-race guard, logout retry) remain a separate future item.
- **Retries: classify + cross-attempt budget cap** (not "never retry").
- **Credits: reserve at submission, refund on terminal failure** (not "best-effort deduct").
- Retry scheduling via explicit `NakAsync(delay)` (code-visible, testable) rather than JetStream consumer `Backoff` config.
- Webhook logic extracted to a service rather than tested through a full HTTP host.

## 1. Failure classification (CarCheckConsumer + pipeline)

- New `PermanentCheckFailureException` (ProcessingService/Pipeline). Thrown for business-final failures:
  - report still invalid after the repair stage (today an `InvalidOperationException`);
  - cross-attempt AI budget already exhausted at attempt start (§2).
- Consumer semantics:
  - **Permanent** → `RecordFailureAsync` + publish `CarCheckFailed` + **ACK**. Never redelivered.
  - **Transient** (any other exception) → if `msg.Metadata.NumDelivered >= MaxDeliver(5)`: treat as final (record + publish + ACK). Otherwise: log and `NakAsync(delay)` — **no** failure recording, **no** failure event on intermediate attempts.
  - Backoff schedule by delivery number: `1m, 4m, 16m, 30m` (pure function, capped at 30m).
- At attempt start the consumer sets `Status = Processing` (+`UpdatedAt`). The enum value exists and the frontend renders it; it was simply never set.
- Classification and backoff live in small pure units (`CheckFailureClassifier`, `RetryDelays` or equivalent) so they are unit-testable without NATS.

## 2. Cross-attempt budget cap

At pipeline start, seed the existing `AiBudgetTracker` with `SUM(EstimatedCostEur)` of this check's persisted `ai_runs` rows (data already recorded per stage by `AiStageRunner`). Consequences:
- Total Claude spend per check ≤ the 2 EUR hard budget across ALL attempts combined.
- If the budget is already exhausted when an attempt starts → `PermanentCheckFailureException` ("AI budget exhausted").

## 3. Claude API retry (AiRetryPolicy)

Small class in Infrastructure/AI, `TimeProvider`-injected for testability:
- Up to **3 attempts** per stage call; delays 2 s then 8 s, ±20 % jitter.
- Retry only on: rate-limit (429), overloaded (529), other 5xx, transport-level errors (`HttpRequestException`, timeout-style `TaskCanceledException` when the caller's token is NOT cancelled).
- Never retry: 4xx client errors, user cancellation.
- `ClaudeAiClient.CreateTextAsync` wraps the SDK call with this policy. (Exact SDK exception types resolved at implementation time; the policy takes a predicate so the mapping is one place.)

## 4. Credit reservation

- `CarCheckService.CreateAsync` (non-whitelisted users): atomic `UPDATE user_credits SET Balance = Balance - 1 WHERE UserId = @u AND Balance >= 1`; 0 rows → `InsufficientCreditsException` (existing 402 mapping in the API). Ledger entry `car_check_reserved` (−1, ReferenceId=checkId). Same transaction as check + outbox row — submission is all-or-nothing. Whitelisted users: no reservation (unchanged behavior).
- `RecordSuccessAsync`: all credit logic **deleted** — marks Completed only. The success path can no longer fail on credits.
- `RecordFailureAsync` (terminal failures only, per §1): **ledger-driven idempotent refund** — if a `car_check_reserved` entry exists for this check AND no `car_check_refunded` entry does: `Balance + 1` + ledger entry `car_check_refunded` (+1, ReferenceId=checkId). Runs in a transaction with the status update.
- Ledger stays append-only; historical `car_check_completed` entries untouched; no schema changes.

## 5. Webhook extraction + graceful idempotency

- New `LemonSqueezyWebhookProcessor` (Infrastructure/Payments): takes the raw body + signature, performs today's exact validation/parsing/idempotency/grant logic; returns an outcome enum (Processed / Ignored / InvalidSignature / DuplicateOrder). The `Program.cs` endpoint becomes a thin shell mapping outcomes to 200/401.
- Duplicate race: the unique index on `payment_orders.ExternalOrderId` (already exists) remains the hard guarantee; the processor catches the unique-violation `DbUpdateException` and returns DuplicateOrder → 200 (transaction already rolled back the credit UPDATE, so no double grant).

## 6. Tests (AutoVerdict.Api.Tests, SQLite in-memory + FakeTimeProvider, patterns already established)

- Reservation: deducts + writes ledger on create; throws `InsufficientCreditsException` at 0 balance (and check+outbox not persisted); whitelisted user skips reservation.
- Refund: terminal failure refunds exactly once (second `RecordFailureAsync` call refunds nothing); no refund when no reservation exists (whitelisted / pre-deploy checks).
- Success: `RecordSuccessAsync` never touches balance/ledger.
- Webhook processor: valid order grants once; duplicate `ExternalOrderId` (sequential AND unique-violation path) grants once and reports DuplicateOrder; bad signature rejected; non-`order_created`/unpaid events ignored.
- `AiRetryPolicy`: retries transient then succeeds; gives up after 3; throws immediately on non-retryable; no retry when caller's token cancelled; delays follow 2s/8s (fake clock).
- Classification/backoff: permanent exception classified permanent; others transient; delay schedule `1m,4m,16m,30m`.
- Budget seeding: prior `ai_runs` rows for the check seed the tracker; other checks' rows don't.

## Out of scope

Auth follow-ups, crawler changes, Opus-review logic, frontend changes (Processing status already renders), JetStream stream/consumer config changes beyond the NAK delay.

## Rollout

No migrations (all tables/columns/enum values exist; ledger reasons are new strings). Mixed-version safety: checks created pre-deploy have no reservation entry → the ledger-driven refund correctly refunds nothing if they fail post-deploy. Normal image deploy; no env/secret changes.
