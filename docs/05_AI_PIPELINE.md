# AI Pipeline

## 1. Goal

The AI pipeline transforms unstructured car listing data into a cautious, useful, structured report for used car buyers.

The system should help identify:

- inconsistencies;
- missing information;
- suspicious wording;
- model-specific risks;
- seller questions;
- inspection checklist items;
- final recommendation.

## 2. Provider Strategy

MVP uses Claude API.

The business logic must not depend directly on Claude.

For MVP, model-specific risks may rely on Claude's general knowledge, but must be written cautiously and with explicit uncertainty. They must not be presented as confirmed facts unless supported by provided listing, user, or vehicle history data.

Required abstraction:

```csharp
public interface IAiAnalysisProvider
{
    Task<CarAnalysisResult> AnalyzeAsync(CarAnalysisRequest request, CancellationToken cancellationToken);
}
```

First implementation:

```txt
ClaudeAiAnalysisProvider
```

Possible future implementations:

```txt
OpenAiAnalysisProvider
GeminiAnalysisProvider
LocalLlmAnalysisProvider
```

## 3. Input Sources

The AI pipeline may receive:

- listing URL;
- listing title;
- listing text;
- price;
- mileage;
- year;
- fuel type;
- transmission;
- seller type;
- VIN;
- registration number;
- first registration date;
- screenshots;
- pasted official vehicle history data;
- user notes.

MVP does not require automatic marketplace scraping.

## 4. Pipeline Stages

### Stage 1: Input Normalization

Normalize user-provided input into a consistent structure.

Tasks:

- trim text;
- remove duplicate sections;
- detect missing fields;
- prepare screenshot references;
- prepare official history text if provided.

### Stage 2: Fact Extraction

Extract structured facts from listing and user-provided data.

Example output:

```json
{
  "brand": "Toyota",
  "model": "Corolla",
  "year": 2022,
  "mileage": 45000,
  "fuelType": "Hybrid",
  "transmission": "Automatic",
  "sellerType": "Private",
  "vinProvided": true,
  "claimedFirstOwner": true,
  "claimedAccidentFree": true
}
```

### Stage 3: Consistency Analysis

Compare claims and available data.

Examples:

- first owner claim vs vehicle history;
- accident-free claim vs damage records;
- private seller vs company ownership history;
- domestic car claim vs import history;
- mileage vs age;
- vague wording vs missing documents;
- short ownership period;
- missing VIN for a relatively expensive car.

### Stage 4: Model-Specific Risk Analysis

Identify known risk areas for the model, engine, transmission, and year.

The system must be cautious and avoid unsupported claims.

Model-specific risks should be described as items to verify, not definitive defects.

### Stage 5: Report Generation

Generate a structured report matching the required schema.

MVP reports must be generated in English.

### Stage 6: Response Validation

The backend must validate the AI response before saving it.

If AI response is invalid:

- retry with correction prompt, or
- mark processing as failed after retry exhaustion.

Invalid AI JSON after retry exhaustion is a technical failure and may trigger an automatic credit refund.

## 5. Report Schema

```json
{
  "riskLevel": "low | medium | high | unknown",
  "confidence": "low | medium | high",
  "summary": "string",
  "vehicleFacts": {
    "brand": "string | null",
    "model": "string | null",
    "year": "number | null",
    "mileage": "number | null",
    "fuelType": "string | null",
    "transmission": "string | null",
    "sellerType": "string | null",
    "countryOfOrigin": "string | null",
    "vinProvided": "boolean"
  },
  "positiveSignals": ["string"],
  "riskSignals": [
    {
      "severity": "low | medium | high",
      "title": "string",
      "explanation": "string",
      "source": "listing | vehicle_history | model_knowledge | user_provided_data | ai_inference"
    }
  ],
  "missingInformation": ["string"],
  "sellerQuestions": ["string"],
  "inspectionChecklist": ["string"],
  "modelSpecificRisks": [
    {
      "component": "string",
      "risk": "string",
      "howToCheck": "string"
    }
  ],
  "recommendation": {
    "decision": "proceed | proceed_with_caution | request_more_info | avoid",
    "explanation": "string"
  },
  "disclaimer": "string"
}
```

Reports must clearly distinguish between:

- facts extracted from the listing;
- facts provided by the user;
- possible model-specific risks;
- recommendations and seller questions.

## 6. Risk Level Guidance

### Low Risk

Use when:

- listing is detailed;
- key data is present;
- claims are consistent;
- no major red flags are visible;
- remaining risks are normal for used cars.

### Medium Risk

Use when:

- important information is missing;
- some claims require clarification;
- model has known areas to check;
- seller wording is vague;
- history data is incomplete.

### High Risk

Use when:

- strong inconsistencies are present;
- seller claims conflict with provided history;
- important ownership or damage details appear suspicious;
- critical information is missing for an expensive/recent car;
- the listing gives multiple red flags.

### Unknown

Use when input data is insufficient for a meaningful conclusion.

## 7. Safety Rules

The AI must not:

- accuse the seller of fraud;
- claim certainty without evidence;
- state that a car is definitely safe;
- replace professional inspection;
- invent vehicle history;
- invent model-specific issues without basis;
- provide legal guarantees.

Preferred wording:

- “This may indicate...”
- “This should be clarified...”
- “The available data is insufficient...”
- “A professional inspection is recommended...”

## 8. Prompting Strategy

Use structured prompts with clear sections:

- role and task;
- input data;
- analysis rules;
- safety rules;
- required JSON schema;
- output constraints.

Temperature should be low because the goal is careful analysis, not creativity.

Prompts and JSON schema should be in English for MVP.

## 9. AI Request Logging

For each request, store:

- provider;
- model;
- prompt version;
- request timestamp;
- response timestamp;
- status;
- error message;
- token usage, if available;
- related check id.

Do not store full prompt bodies, raw AI input, or raw AI output by default.

The system may support a development-only option to store raw prompt/input/output for debugging. This option must be disabled by default and must not be enabled in production unless explicitly configured.

## 10. Future Improvements

- Dedicated model knowledge base.
- Retrieval-augmented generation for model-specific issues.
- Country-specific vehicle history parsing.
- Multi-language reports.
- Car comparison reports.
- Seller message generation.
- PDF export.
