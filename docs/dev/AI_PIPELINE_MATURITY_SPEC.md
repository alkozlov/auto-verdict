# AutoVerdict AI Pipeline Maturity Specification

## 1. Purpose

This document defines the target design for replacing the current single linear AI call in
`AutoVerdict.ProcessingService` with a more mature, cost-controlled, multi-stage AI pipeline.

The goal is to improve report quality, observability, maintainability, and cost control while
preserving the current product model:

- users buy a single AutoVerdict car check;
- users do not choose between standard and premium analysis;
- internally, the system may use different AI models and multiple AI calls;
- the final user-facing output remains one structured markdown report.

This specification is intended as implementation guidance for an AI coding agent.

## 2. Current State

The current processing flow is mostly linear:

```text
NATS message
  -> optional Otomoto crawl
  -> build one AiAnalysisRequest
  -> one Claude call
  -> save markdown report to SeaweedFS
  -> update check status
```

Relevant current code:

- `AutoVerdict.ProcessingService/Consumers/CarCheckConsumer.cs`
- `AutoVerdict.ProcessingService/Pipeline/CarCheckAnalysisPipeline.cs`
- `AutoVerdict.Infrastructure/AI/ClaudeAiAnalysisProvider.cs`
- `AutoVerdict.Application/AI/IAiAnalysisProvider.cs`
- `AutoVerdict.Application/AI/AiAnalysisRequest.cs`
- `AutoVerdict.Application/AI/AiAnalysisResult.cs`

The current design has important strengths:

- the AI provider is abstracted behind `IAiAnalysisProvider`;
- the final report is markdown, not JSON, which keeps the user-facing report simple;
- the pipeline degrades gracefully if Otomoto crawling fails;
- reports are stored in object storage instead of a large database text field.

The main limitation is that one AI call is responsible for too much:

- understanding raw user input;
- interpreting crawled listing data;
- reading images/screenshots;
- extracting facts;
- reasoning about risks;
- choosing a recommendation;
- writing the final report.

That makes quality harder to control and cost harder to reason about.

## 3. Target Concept

Replace the single large AI call with a deterministic orchestrator that runs several bounded stages.

This is a multi-stage AI pipeline, not a group of autonomous agents.

The processing service should decide the workflow. AI models should not autonomously decide which
tools to call, how many times to loop, or when to stop.

Target mental model:

```text
Evidence Builder -> Fact Extractor -> Risk Analyzer -> Report Writer -> Validator -> Optional Repair
```

Some stages use AI. Some stages are normal deterministic code.

## 4. Business Requirements

### 4.1 Product Model

- A user purchases a car check, not a model tier.
- The frontend must not expose "standard", "premium", "Sonnet", "Opus", or similar AI choices.
- The backend may use model routing internally.
- The final output must remain a single report for one car check.
- The current pricing model remains:
  - single check;
  - package of three checks at a discount.

### 4.2 Unit Economics

Current business assumption:

- gross user price: about 4 EUR per check;
- estimated net remaining after taxes and payment/platform fees: about 2 EUR per check;
- AI cost must leave room for infrastructure, failed jobs, retries, support, and future operating costs.

Recommended internal AI budget:

- normal target AI cost per check: 0.20-0.70 EUR;
- acceptable complex-check AI cost: 1.00-1.50 EUR;
- hard emergency ceiling: 2.00 EUR per check.

These values should be configurable and should not be hard-coded.

### 4.3 Model Strategy

The production system should support combined models within a single check.

Recommended routing:

- Haiku:
  - cheap extraction;
  - summarization;
  - evidence compression;
  - validation/repair for simple formatting failures.
- Sonnet:
  - default production reasoning model;
  - risk analysis;
  - final report generation for normal checks.
- Opus:
  - selective escalation;
  - complex/high-risk reasoning;
  - critique of weak reports;
  - final report generation only when justified by budget and complexity.

The user must receive consistent product quality regardless of internal model choices.

### 4.4 Cost-Aware Escalation

Opus should not be used by default for every full report unless measured costs prove it is safe.

Escalate to Opus only when one or more of the following is true:

- the input contains many contradictions;
- the vehicle appears expensive or unusually risky;
- the Sonnet risk analysis reports low confidence;
- deterministic validation finds a weak or unsafe report;
- many useful images/documents are present;
- crawled data conflicts with user-provided data;
- the check remains under the configured AI budget.

Escalation must be explainable in internal logs and persisted AI run metadata.

## 5. Target Processing Flow

### 5.1 High-Level Flow

```text
1. Consume CarCheckRequestedMessage
2. Load check data and user image keys
3. Crawl Otomoto if URL is supported
4. Build EvidenceBundle
5. Run FactExtraction stage
6. Run RiskAnalysis stage
7. Run ReportGeneration stage
8. Run deterministic ReportValidation stage
9. Optionally run ReportRepair or OpusReview stage
10. Save final markdown report as ai-analysis-result.md
11. Persist AI run metadata and token/cost data
12. Mark check Completed or Failed
13. Publish completion/failure event
```

### 5.2 Important Constraint

Do not send all raw evidence to every AI stage.

The expensive models should receive compact, relevant evidence whenever possible.

Bad pattern:

```text
Every stage receives full user text, full screenshot, all images, full crawled attributes.
```

Preferred pattern:

```text
Early stages see raw evidence.
Later stages see compact facts, selected evidence snippets, and risk notes.
```

## 6. Data Contracts

The exact C# shape can be adapted to existing project conventions, but the pipeline should introduce
clear internal contracts.

### 6.1 EvidenceBundle

Purpose: normalized, bounded input for AI stages.

Suggested shape:

```csharp
public sealed record EvidenceBundle(
    Guid CheckId,
    string UserDescriptionMarkdown,
    string? ListingUrl,
    CarListingSnapshot? CrawledListing,
    IReadOnlyList<UserImageContent> UserImages,
    ImageContent? ListingScreenshot,
    EvidenceLimits Limits);
```

The implementation may keep image bytes out of this record if that is better for memory usage.

### 6.2 ExtractedVehicleFacts

Purpose: structured facts extracted from user input and crawled listing data.

Suggested fields:

```csharp
public sealed record ExtractedVehicleFacts(
    string? Make,
    string? Model,
    int? Year,
    int? MileageKm,
    decimal? Price,
    string? Currency,
    string? FuelType,
    string? Transmission,
    string? Engine,
    string? SellerType,
    string? Location,
    string? Vin,
    bool VinPresent,
    bool ServiceHistoryMentioned,
    bool AccidentFreeClaimed,
    bool ImportedMentioned,
    bool FirstOwnerClaimed,
    IReadOnlyDictionary<string, string> RawAttributes,
    IReadOnlyList<string> EvidenceNotes,
    IReadOnlyList<string> MissingFields,
    string Confidence);
```

### 6.3 RiskAnalysisResult

Purpose: internal reasoning output, not user-facing final report.

Suggested fields:

```csharp
public sealed record RiskAnalysisResult(
    string OverallRiskLevel,
    string RecommendedVerdict,
    string Confidence,
    IReadOnlyList<RiskItem> TechnicalRisks,
    IReadOnlyList<RiskItem> ListingRisks,
    IReadOnlyList<RiskItem> DealRisks,
    IReadOnlyList<string> MissingInformation,
    IReadOnlyList<string> SellerQuestions,
    IReadOnlyList<string> InspectionChecklist,
    IReadOnlyList<string> CostAssumptions,
    IReadOnlyList<string> Inconsistencies,
    bool NeedsEscalation,
    string? EscalationReason);

public sealed record RiskItem(
    string Severity,
    string Title,
    string Explanation,
    string Source,
    string HowToVerify);
```

### 6.4 FinalReportResult

Purpose: final user-facing markdown plus report metadata.

```csharp
public sealed record FinalReportResult(
    string Markdown,
    string Verdict,
    string Confidence,
    bool WasRepaired,
    IReadOnlyList<string> ValidationWarnings);
```

## 7. AI Stages

### 7.1 Stage: Evidence Preparation

Type: deterministic code.

Responsibilities:

- normalize user markdown;
- trim excessive whitespace;
- remove duplicated crawled/user sections where possible;
- cap input length;
- choose whether the listing screenshot should be sent;
- downscale or compress images before AI calls;
- preserve the original user intent;
- build a compact text representation of crawled listing data.

Implementation notes:

- do not mutate stored user input;
- keep a separate prepared evidence representation for AI;
- if text is very long, summarize or chunk before the main analysis stages;
- prefer structured parsed OTOMOTO facts over screenshot OCR when available.

### 7.2 Stage: Fact Extraction

Type: AI call, usually Haiku or Sonnet.

Input:

- user description;
- compact crawled listing data;
- selected image/screenshot evidence only when needed.

Output:

- `ExtractedVehicleFacts` JSON.

Business goal:

- establish what is known before reasoning about risks;
- separate facts from assumptions;
- identify missing information.

Rules:

- do not invent facts;
- use `null` or `Unknown` when unavailable;
- distinguish user-provided facts from crawled facts;
- mark confidence.

### 7.3 Stage: Risk Analysis

Type: AI call, usually Sonnet.

Input:

- `ExtractedVehicleFacts`;
- compact evidence notes;
- inconsistencies;
- relevant crawled listing attributes.

Output:

- `RiskAnalysisResult` JSON.

Business goal:

- produce useful buyer reasoning before writing polished prose;
- identify what the buyer should verify;
- avoid unsupported accusations or guarantees.

Rules:

- all risks must be phrased as possibilities or verification points;
- model-specific risks must be framed cautiously;
- distinguish listing risks, technical risks, and deal risks;
- include only useful seller questions;
- include inspection checklist items that a normal buyer can understand.

### 7.4 Stage: Report Generation

Type: AI call, usually Sonnet; Opus may be used if escalation is justified.

Input:

- extracted facts;
- risk analysis result;
- selected evidence snippets;
- required report format.

Output:

- final markdown report.

Required markdown sections:

```text
# Verdict
# Key Risks
## Technical Risks
## Listing Risks
## Deal Risks
# Missing Information
# Questions for the Seller
# Inspection Checklist
# Vehicle Facts
# Estimated Costs
# Summary
```

The report must end with:

```text
---

*Disclaimer: AutoVerdict provides AI-assisted preliminary screening only. Always verify documents and arrange an independent inspection before purchasing.*
```

The final report must be written for a non-expert private buyer.

### 7.5 Stage: Report Validation

Type: deterministic code first.

Responsibilities:

- verify all required headings are present;
- verify the disclaimer is present;
- verify one of the allowed verdicts is present:
  - Buy;
  - Buy with caution;
  - Avoid.
- verify the report is not empty or too short;
- verify there are no obvious forbidden certainty phrases;
- verify markdown is renderable enough for the frontend;
- verify estimated costs table exists;
- verify inspection checklist uses markdown checkboxes.

Forbidden or suspicious wording examples:

- "This car is safe."
- "The seller is dishonest."
- "This vehicle was definitely damaged."
- "Guaranteed."
- "No risk."

The validator should return warnings and errors.

Validation errors may trigger repair. Validation warnings may be stored without failing the check.

### 7.6 Stage: Report Repair

Type: AI call only if validation fails.

Input:

- invalid markdown;
- validation errors;
- required format.

Output:

- repaired markdown.

Rules:

- do not re-analyze the vehicle from scratch unless necessary;
- fix structure, unsafe wording, and missing sections;
- keep factual content consistent with prior risk analysis;
- stay within remaining budget.

### 7.7 Optional Stage: Opus Review

Type: AI call only for selected cases.

Recommended use:

- critique a Sonnet-generated report;
- identify unsupported claims;
- identify missed high-impact risks;
- suggest targeted corrections.

Preferred output:

- short review JSON or concise correction notes, not a complete rewritten report.

The system can then:

- apply deterministic changes if simple;
- ask Sonnet to repair the report using the review notes;
- ask Opus to rewrite only if budget allows and quality benefit is significant.

## 8. Provider and Interface Design

The current `IAiAnalysisProvider.AnalyzeAsync(AiAnalysisRequest)` is too coarse for a staged pipeline.

Introduce a lower-level chat/completion abstraction and stage-specific services.

Suggested design:

```csharp
public interface IAiClient
{
    Task<AiTextResponse> CreateTextAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AiTextRequest(
    string Stage,
    string Model,
    string PromptVersion,
    string SystemPrompt,
    IReadOnlyList<AiMessageContent> Messages,
    int MaxTokens,
    decimal? BudgetLimitEur);

public sealed record AiTextResponse(
    string Text,
    string Provider,
    string Model,
    long InputTokens,
    long OutputTokens,
    TimeSpan Duration);
```

Then build stage services:

```csharp
public interface IFactExtractionStage
{
    Task<ExtractedVehicleFacts> ExecuteAsync(EvidenceBundle evidence, CancellationToken ct);
}

public interface IRiskAnalysisStage
{
    Task<RiskAnalysisResult> ExecuteAsync(
        EvidenceBundle evidence,
        ExtractedVehicleFacts facts,
        CancellationToken ct);
}

public interface IReportGenerationStage
{
    Task<FinalReportResult> ExecuteAsync(
        EvidenceBundle evidence,
        ExtractedVehicleFacts facts,
        RiskAnalysisResult risks,
        CancellationToken ct);
}
```

The implementation does not have to use these exact names, but it should separate stages clearly.

## 9. Model Routing

Introduce configurable routing rules.

Example configuration:

```json
{
  "AiPipeline": {
    "Enabled": true,
    "Currency": "EUR",
    "DefaultBudgetEur": 0.70,
    "ComplexBudgetEur": 1.50,
    "HardBudgetEur": 2.00,
    "Stages": {
      "FactExtraction": {
        "Model": "claude-haiku-4-5",
        "MaxTokens": 2500
      },
      "RiskAnalysis": {
        "Model": "claude-sonnet-4-6",
        "MaxTokens": 5000
      },
      "ReportGeneration": {
        "Model": "claude-sonnet-4-6",
        "MaxTokens": 8000
      },
      "ReportRepair": {
        "Model": "claude-haiku-4-5",
        "MaxTokens": 8000
      },
      "OpusReview": {
        "Model": "claude-opus-4-1",
        "MaxTokens": 3000,
        "Enabled": true
      }
    }
  }
}
```

Model names and prices must be configurable. Do not hard-code current provider prices.

The implementation should support environment variable overrides for production.

## 10. Cost Tracking

### 10.1 Required Persistence

Add persistence for AI run metadata.

Suggested table/entity: `ai_runs`.

Fields:

- `Id` int primary key or GUID, following project convention;
- `CheckId` GUID;
- `Stage` string;
- `Provider` string;
- `Model` string;
- `PromptVersion` string;
- `InputTokens` long;
- `OutputTokens` long;
- `EstimatedCostEur` decimal;
- `DurationMs` long;
- `Status` string;
- `ErrorMessage` string nullable;
- `StartedAt` timestamp;
- `CompletedAt` timestamp nullable;
- `CreatedAt` timestamp.

Optional fields:

- `EscalationReason`;
- `ValidationWarningsJson`;
- `RawInputStorageKey` for development only;
- `RawOutputStorageKey` for development only.

### 10.2 Cost Estimation

Add configurable model price metadata.

Example:

```json
{
  "AiPricing": {
    "claude-haiku-4-5": {
      "InputPerMillionTokensUsd": 1.00,
      "OutputPerMillionTokensUsd": 5.00
    },
    "claude-sonnet-4-6": {
      "InputPerMillionTokensUsd": 3.00,
      "OutputPerMillionTokensUsd": 15.00
    },
    "claude-opus-4-1": {
      "InputPerMillionTokensUsd": 15.00,
      "OutputPerMillionTokensUsd": 75.00
    }
  }
}
```

Important:

- verify current provider pricing before production deployment;
- pricing changes over time;
- the app should not require code changes when pricing changes.

### 10.3 Budget Enforcement

Before each optional stage:

- calculate already spent estimated cost;
- estimate maximum cost of the next stage;
- skip optional escalation if it may exceed the hard budget;
- log why escalation was skipped.

For required stages:

- enforce max input size;
- enforce max output tokens;
- fail gracefully if the expected cost would exceed hard budget.

## 11. Prompt Management

Move large prompts out of provider implementation if practical.

Recommended structure:

```text
src/backend/src/AutoVerdict.Infrastructure/AI/Prompts/
  fact-extraction.v1.md
  risk-analysis.v1.md
  report-generation.v1.md
  report-repair.v1.md
  opus-review.v1.md
```

Each prompt must have:

- a stable version;
- a clearly defined input contract;
- a clearly defined output contract;
- safety rules;
- business positioning.

Prompt versions must be stored in `ai_runs`.

## 12. Image and Screenshot Optimization

Images can dominate cost and latency.

Requirements:

- cap number of user images already accepted by API;
- resize large images before AI calls;
- prefer JPEG/WebP compression for AI input when acceptable;
- do not send listing screenshot to later stages if parsed facts are sufficient;
- include screenshot only in fact extraction or visual review stages;
- skip unreadable or failed image downloads instead of failing the whole check;
- record skipped image count in logs or metadata.

Suggested initial policy:

- send listing screenshot only to fact extraction when crawled structured data is weak;
- send at most 3 user images to default analysis;
- send up to 5 only if budget allows or the evidence appears image-heavy;
- use compact image dimensions suitable for visual understanding, not archival quality.

The original uploaded images should remain in storage unchanged.

## 13. Safety and Business Quality Rules

The AI output must:

- be cautious;
- avoid certainty where evidence is incomplete;
- avoid accusing sellers of fraud;
- not claim the car is safe;
- not replace professional inspection;
- not invent vehicle history;
- distinguish facts from assumptions;
- include practical next steps;
- use language understandable by non-experts;
- use PLN for cost estimates unless otherwise justified.

The report should help the buyer answer:

- Is this listing worth further attention?
- What should I verify before contacting the seller?
- What questions should I ask?
- What should be checked during inspection?
- What risks could make this car expensive or unsafe to buy?

## 14. Failure Handling

### 14.1 Required Stage Failure

If fact extraction, risk analysis, or report generation fails:

- retry according to configured policy;
- record failed `ai_runs`;
- if retries are exhausted, mark check Failed;
- store a useful failure reason.

### 14.2 Optional Stage Failure

If Opus review or repair fails:

- do not automatically fail the check if a valid report already exists;
- log and persist the optional stage failure;
- use the best valid report available.

### 14.3 Crawl Failure

Keep current behavior:

- if Otomoto crawling fails, continue using user description and uploaded images;
- include a note in internal evidence that crawled listing data was unavailable;
- do not expose crawler technical details in the final report unless useful to the buyer.

## 15. Observability

Add structured logs for:

- check id;
- stage;
- model;
- prompt version;
- input tokens;
- output tokens;
- estimated cost;
- duration;
- escalation decision;
- validation result.

Recommended metrics:

- `ai_stage_duration_ms`;
- `ai_stage_input_tokens_total`;
- `ai_stage_output_tokens_total`;
- `ai_stage_estimated_cost_eur_total`;
- `ai_stage_failures_total`;
- `ai_report_validation_failures_total`;
- `ai_opus_escalations_total`;
- `ai_budget_skips_total`.

## 16. Storage Outputs

Continue storing the final report as:

```text
{checkId}/ai-analysis-result.md
```

Optional additional debug/development outputs:

```text
{checkId}/ai/facts.json
{checkId}/ai/risks.json
{checkId}/ai/report-validation.json
{checkId}/ai/opus-review.json
```

Production storage of raw prompts or raw AI outputs should be disabled by default.

Do not store sensitive raw prompt bodies unless explicitly configured for development.

## 17. Database Changes

Minimum recommended changes:

- add `ai_runs` table;
- optionally add `AiPipelineVersion` to `car_checks`;
- optionally add `EstimatedAiCostEur` to `car_checks`;
- optionally add `ValidationWarningsJson` to `car_checks`.

Avoid storing the full final markdown report in the database.

## 18. Migration Strategy

Implement in small steps.

### Phase 1: Observability First

- keep current single-call behavior;
- add `ai_runs` persistence for the existing call;
- record token usage and estimated cost;
- add prompt version metadata.

### Phase 2: Extract Pipeline Interfaces

- introduce pipeline stage interfaces;
- move current one-call prompt into `ReportGenerationStage`;
- keep behavior functionally equivalent.

### Phase 3: Add Fact Extraction

- add `FactExtractionStage`;
- persist or log facts;
- feed extracted facts into report generation;
- compare report quality and cost.

### Phase 4: Add Risk Analysis

- add `RiskAnalysisStage`;
- generate final report from facts + risks;
- add report validation.

### Phase 5: Add Repair and Escalation

- add deterministic validation;
- add repair stage;
- add optional Opus review based on configured routing and budget.

### Phase 6: Optimize

- image compression;
- prompt caching if supported by provider/client;
- input trimming;
- model routing improvements;
- cost dashboards or admin inspection.

## 19. Backward Compatibility

The API response shape should not change for the frontend.

Existing frontend behavior should continue:

- submit check;
- poll status;
- open final markdown report.

Existing storage key for the final markdown report should remain stable.

## 20. Security Requirements

- Do not commit live AI API keys to `appsettings.json`.
- Read provider keys from environment variables or user secrets.
- Rotate any key that was committed or exposed.
- Do not log full user descriptions, full prompts, images, or raw model responses in production.
- Do not expose internal model names or costs to end users.

## 21. Acceptance Criteria

The implementation is complete when:

1. `AutoVerdict.ProcessingService` uses a staged AI pipeline instead of one monolithic analysis call.
2. The final user-facing report is still saved as markdown in SeaweedFS.
3. The frontend API contract remains unchanged.
4. At least these stages exist:
   - evidence preparation;
   - fact extraction;
   - risk analysis;
   - report generation;
   - report validation.
5. Optional repair/escalation exists or is explicitly configurable as disabled.
6. AI run metadata is persisted for every AI call.
7. Token usage and estimated cost are recorded per stage.
8. Per-check AI budget limits are configurable and enforced.
9. Model routing is configurable.
10. Opus usage, if enabled, is selective and budget-aware.
11. Validation prevents reports with missing required sections from being saved as successful.
12. Crawl failure still degrades gracefully.
13. Raw prompts and raw outputs are not stored in production by default.
14. Existing tests/build pass.

## 22. Non-Goals

Do not implement autonomous multi-agent orchestration in this phase.

Do not expose model selection to users.

Do not redesign frontend flows.

Do not change the payment model.

Do not require PDF generation for this work.

Do not make Opus mandatory for every request.

