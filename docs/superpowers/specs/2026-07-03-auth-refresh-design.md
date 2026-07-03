# Auth Session Refresh — Design

**Date:** 2026-07-03 · **Status:** Implemented

## Problem

The backend issues a single 30-day JWT and has no refresh mechanism. The frontend stores it in localStorage forever and never reacts to 401/403, so after expiry every API call fails until the user manually signs in again. The token is also delivered via `/auth/callback?token=...`, leaking it into browser history and proxy logs.

## Decisions (made with user)

- **Refresh model:** DB-backed, rotating refresh tokens (revocable; reuse detection).
- **Access token storage:** in-memory only on the frontend; no localStorage, no token in URLs.
- **Approach:** hand-rolled minimal flow on the existing JWT bearer setup (no ASP.NET Identity, no cookie-auth-everything).

## Token model

- **Access token:** existing HS256 JWT, lifetime **30 minutes** (`Auth:JwtExpirationMinutes`, replaces `JwtExpirationDays`). Claims unchanged (`sub`, `email`, `jti`).
- **Refresh token:** 256-bit cryptographically random value. Only its **SHA-256 hash** is stored. Lifetime **30 days, sliding**: every refresh issues a new token (new 30-day window) and revokes the old row.
- **Rotation & theft detection:** tokens belong to a `FamilyId` (one family per login). Presenting an already-rotated/revoked token **more than 60 seconds after its revocation** revokes the entire family → user must re-login. Reuse **within 60 seconds** is treated as a legitimate concurrent refresh (multi-tab restore, double-fire) and rotates normally, issuing another child in the same family. The grace path applies **only** to rotation-revoked tokens (`ReplacedByTokenHash` set) and **only while the family still has at least one live token** — tokens killed by family revocation (logout/theft) always fail, and a dead family can never mint a new token. *(Amended 2026-07-03 after review: strict revocation caused spurious logouts for multi-tab users and left an unguarded rotation race.)*

### `refresh_tokens` table

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid PK | |
| `UserId` | uuid FK → users | index |
| `TokenHash` | text | unique index; SHA-256 hex |
| `FamilyId` | uuid | index |
| `ExpiresAt` | timestamptz | |
| `CreatedAt` | timestamptz | |
| `RevokedAt` | timestamptz null | null = active |
| `ReplacedByTokenHash` | text null | audit trail |

## Cookie

`av_refresh`: HttpOnly, `Secure`, `SameSite=Lax`, `Path=/api/auth`, `Max-Age` = 30 days. Scoped so it is sent only to auth endpoints. `POST /api/auth/refresh` is CSRF-safe under Lax (cross-site POSTs do not carry Lax cookies).

## Endpoints

- `GET /api/auth/google/complete` — after user upsert: create refresh token family, set `av_refresh` cookie, redirect to `/auth/callback` (no query params).
- `POST /api/auth/refresh` — read cookie → hash → look up. Valid: rotate (revoke old row, insert replacement, set new cookie), return `200 { accessToken }`. Missing/expired/revoked: revoke family on reuse, clear cookie, `401`. Never 500s on bad input.
- `POST /api/auth/logout` — revoke the presented token's family, clear cookie, `204`.

Backed by a `RefreshTokenService` in `AutoVerdict.Infrastructure/Auth` (create family / rotate / revoke family), used only by these endpoints.

## Frontend

- `lib/auth.ts` → in-memory holder: `getAccessToken()`, `setAccessToken()`, `clearAccessToken()`, and `refreshAccessToken()` with **single-flight** semantics (concurrent 401s await one shared refresh promise). localStorage code removed.
- `lib/api.ts` → one `authFetch` wrapper used by **all** calls including the FormData paths: attach bearer; on 401 → `refreshAccessToken()` → retry once; if refresh fails → clear state → redirect to `/` with `?session=expired`.
- `app/auth/callback/page.tsx` → call `refreshAccessToken()`; success → `/garage/check`, failure → `/?error=auth_failed`.
- `app/garage/layout.tsx` boot → try refresh (cookie is the session marker), then `me()`; failure → redirect `/`.

## Rollout

- One EF migration (new table). No data changes; `JWT_SECRET` and VPS_ENV unchanged.
- Deploying logs every existing user out once (old localStorage tokens are simply unused). Accepted.
- `JwtExpirationDays` removed from `AuthOptions`.

## Testing

First tests in `AutoVerdict.Api.Tests` (xunit): RefreshTokenService — happy rotation, expired token → 401-path result, reuse revokes family, logout revokes family; JwtService — token expires in 30 minutes. Frontend verified manually (no FE test infra; out of scope).

## Error handling

- Refresh/logout endpoints treat all invalid input as 401/204 respectively; no stack traces to clients.
- TEST_MODE whitelist middleware unchanged.
- Expired access tokens during an active session are invisible to the user (silent refresh + retry).
