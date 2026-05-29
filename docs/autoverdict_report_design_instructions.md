# AutoVerdict — Report Design and Markdown Generation Upgrade Instructions

## 1. Goal

Improve the visual quality, readability, and perceived value of AutoVerdict AI reports.

The report must remain **markdown-only** as the persisted report format. However, the application should stop displaying the report as plain markdown with default styles. Instead, implement a dedicated read-only report viewer that renders markdown as a polished SaaS-style buyer report.

The AI-generated report must also become more structured, more actionable, and easier for a non-expert buyer to understand.

This task includes both:

1. changes to AI report generation instructions;
2. frontend changes for rendering completed reports.

---

## 2. Important Product Context

AutoVerdict helps private buyers evaluate used-car listings before they contact the seller, travel to inspect a car, pay a reservation fee, or commit to financing.

The report is not a legal guarantee, mechanic inspection, or official vehicle history report. It is an AI-assisted preliminary screening report.

The report must help the user answer these questions:

- Is this listing worth further attention?
- What are the main risks?
- What information is missing?
- What should I ask the seller?
- What should I verify before inspection or purchase?
- What are the estimated costs?
- Did AutoVerdict answer my personal questions from the input?

---

## 3. Key User Input Requirement

The user input is free-form.

It may contain:

- copied listing text;
- seller messages;
- VIN/details;
- inspection notes;
- screenshots/images;
- a listing URL;
- the user's own personal questions.

The AI service must detect and answer relevant user questions when they are related to:

- the car;
- the listing;
- purchase risks;
- price/value;
- seller communication;
- documents;
- inspection;
- ownership costs;
- whether the listing is worth pursuing.

The final report must include a clearly visible section that shows whether the user's questions were answered.

Do not answer questions unrelated to car purchase analysis. If the user asks unrelated questions, mention that they are outside the scope of the car purchase analysis and ignore them.

---

## 4. Current Problem

Current reports technically satisfy the required markdown structure, but visually they look like raw AI output.

Observed issues:

- The first screen does not immediately communicate the decision.
- The verdict looks like normal text instead of the main outcome.
- Too many sections look visually identical.
- Risk tables are dense and hard to scan.
- There is no polished executive summary.
- There is no clear answer-tracking block for the user's own questions.
- The report list shows truncated input text instead of meaningful report summaries.
- The markdown renderer does not create the feeling of a premium SaaS report.

The goal is to make the report feel like a finished expert document.

---

## 5. Required Report Structure

Keep the existing required section order so the current validation model remains compatible.

Current required structure:

1. Verdict
2. Key Risks
3. Technical Risks
4. Listing Risks
5. Deal Risks
6. Missing Information
7. Questions for the Seller
8. Inspection Checklist
9. Vehicle Facts
10. Estimated Costs
11. Summary

Add the user's personal-question answers inside the existing structure to avoid breaking heading validation.

Recommended placement:

- Add a subsection inside `Verdict` or `Key Risks` named `Your questions answered`.
- Prefer placing it near the top of the report, after the verdict explanation and before detailed risk tables.

Example:

```md
## Verdict

**🟡 Buy with caution**

Short explanation...

### At a glance

| Item | Result |
|---|---|
| Overall risk | Medium |
| Main concern | Unclear service history |
| Technical risk | Medium |
| Listing transparency | Medium |
| Deal risk | Low |
| Recommended next step | Ask for VIN and service invoices before visiting |

### Your questions answered

| Your question | Answer | Status |
|---|---|---|
| Is this car worth visiting? | Yes, but only after the seller provides VIN and service records. | Answered |
| Is the price suspicious? | The price is not clearly suspicious, but it should be compared with similar vehicles. | Answered |
```

If the user did not ask explicit personal questions:

```md
### Your questions answered

No explicit buyer questions were found in the input. The report focuses on listing risks, missing information, seller questions, and inspection points.
```

If the user asked unrelated questions:

```md
### Your questions answered

| Your question | Answer | Status |
|---|---|---|
| Can you write a poem about this car? | This is outside the scope of AutoVerdict. The report only covers car purchase analysis and risk assessment. | Out of scope |
```

Important:

- Do not create a new top-level required heading unless the validator is also updated.
- Use localized subsection title in the selected report language.
- Preserve the current required localized top-level headings.

---

## 6. Report Generation Prompt Changes

Update `ReportGenerationStage` prompt.

The final report must still be markdown-only, but it should be written as a premium SaaS-style buyer report.

Add these rules to the report generation instructions.

### 6.1 General Report Style

The final report must:

- look like a polished buyer report, not a generic AI essay;
- use short paragraphs;
- use structured tables, grouped bullets, and clear action points;
- avoid long uninterrupted prose;
- avoid generic filler;
- be practical and decision-oriented;
- explain risks in plain language for non-expert buyers;
- never guarantee safety;
- never accuse the seller of fraud or dishonesty;
- never claim certainty without evidence;
- clearly separate known facts from assumptions;
- preserve brand names, VINs, URLs, model names, and technical identifiers exactly;
- stay fully localized in the requested report language.

### 6.2 Verdict Section

Under the `Verdict` heading, always include:

1. one localized verdict label only;
2. a short plain-language explanation of the decision;
3. an `At a glance` markdown table;
4. a `Your questions answered` subsection.

The `At a glance` table must include:

| Item | Description |
|---|---|
| Overall risk | Low / Medium / High / Unknown |
| Main concern | The single biggest issue to verify |
| Technical risk | Low / Medium / High / Unknown |
| Listing transparency | Low / Medium / High / Unknown |
| Deal risk | Low / Medium / High / Unknown |
| Recommended next step | One concrete action |

Use severity labels with icons:

- 🟢 Low
- 🟠 Medium
- 🔴 High
- ⚪ Unknown

Localize the words, but keep the icons.

### 6.3 User Questions Handling

The AI must inspect the user's free-form input for explicit questions.

Examples of explicit questions:

- “Should I buy this car?”
- “Is the mileage suspicious?”
- “What should I ask the seller?”
- “Is this price too high?”
- “Can this car fit my family?”
- “Is this engine reliable?”
- “Should I go inspect it?”

The final report must include a `Your questions answered` subsection near the top.

Rules:

- Answer only car-purchase-related questions.
- Keep answers short and practical.
- If the evidence is insufficient, say what is needed to answer properly.
- If the question is already answered elsewhere, still include a short answer and point the user to the relevant section indirectly.
- If no personal questions were found, state that no explicit buyer questions were found.
- If a question is unrelated to car purchase analysis, mark it as out of scope.

Recommended table format:

```md
### Your questions answered

| Your question | Short answer | Status |
|---|---|---|
| ... | ... | Answered / Partially answered / Not enough data / Out of scope |
```

Do not invent questions. Only include questions clearly present in the user input.

### 6.4 Key Risks Section

Under `Key Risks`, include:

1. `Top decision points` with 3–5 numbered points.
2. A compact `Risk overview` table.

Each top decision point must have:

- a bold title;
- one short explanation sentence;
- one short action sentence.

Example:

```md
### Top decision points

1. **Service history must be verified**  
   The listing mentions service, but does not provide invoices or a clear maintenance timeline. Ask for service invoices before visiting.
```

The `Risk overview` table must include:

| Area | Risk level | What it means |
|---|---|---|
| Technical condition | 🟠 Medium | Service history and inspection are needed before purchase |
| Listing transparency | 🟢 Low | The listing provides most key facts |
| Deal structure | ⚪ Unknown | Financing and total cost are not described |

### 6.5 Technical Risks, Listing Risks, Deal Risks

For each risk section, use a markdown table.

Required columns:

| Column | Purpose |
|---|---|
| Risk | Short title |
| Severity | Low / Medium / High / Unknown with icon |
| Evidence | What in the provided data supports this risk |
| Why it matters | Why the buyer should care |
| How to verify | Concrete next step |

Rules:

- Keep cells concise.
- Do not write large paragraphs inside table cells.
- If no meaningful risk exists in a category, write one short sentence instead of forcing a fake risk.
- Never write “no risk” or “the car is safe”.
- Prefer “No major issue was visible from the available evidence, but this still requires verification during inspection.”

### 6.6 Missing Information Section

Use a markdown table.

Required columns:

| Missing item | Why it matters | Priority |
|---|---|---|
| VIN | Needed for history checks and document verification | High |

Priority values:

- High
- Medium
- Low

Localize priority values.

### 6.7 Questions for the Seller Section

Group questions under relevant subheadings.

Allowed subgroups:

- Price and payment
- Vehicle history
- Documents
- Inspection
- Logistics
- Warranty
- Financing

Only include groups that are relevant.

Questions must be ready to copy and send to the seller.

Avoid overly long lists. Prefer 6–10 strong questions.

### 6.8 Inspection Checklist Section

Use grouped markdown checkboxes.

Allowed subgroups:

- Documents
- Exterior
- Interior
- Test drive
- Electronics
- Final handover

Use markdown checkbox syntax only:

```md
- [ ] Check VIN on the body and documents
```

Keep items practical and inspection-oriented.

### 6.9 Vehicle Facts Section

Use a clean markdown table.

Rules:

- Use localized “Unknown” when unavailable.
- Do not invent facts.
- Preserve VIN, model names, engine names, and transmission names exactly.
- Distinguish explicit facts from assumptions where needed.

### 6.10 Estimated Costs Section

Use a markdown table.

Required columns:

| Cost item | Estimated amount | Notes | Priority / When to pay |
|---|---|---|---|

Rules:

- Use PLN unless evidence clearly uses another currency.
- If the estimate is uncertain, say so.
- Do not present estimates as guaranteed prices.

### 6.11 Summary Section

The summary must include:

- 2–3 sentence final interpretation;
- a clear recommended next action;
- a reminder that the report is preliminary and does not replace professional inspection.

---

## 7. Risk Analysis Stage Changes

Update `RiskAnalysisStage` to better support structured report rendering.

Current risk items already include:

- severity;
- title;
- explanation;
- source;
- howToVerify.

Improve the JSON contract if practical.

Recommended risk object shape:

```json
{
  "severity": "low" | "medium" | "high" | "unknown",
  "title": "string",
  "explanation": "string",
  "source": "string",
  "howToVerify": "string",
  "evidenceStrength": "weak" | "medium" | "strong"
}
```

Recommended top-level additions:

```json
{
  "mainConcern": "string | null",
  "recommendedNextStep": "string",
  "userQuestions": [
    {
      "question": "string",
      "answer": "string",
      "status": "answered" | "partially_answered" | "not_enough_data" | "out_of_scope"
    }
  ]
}
```

Rules for `userQuestions`:

- Extract only explicit user questions from the input.
- Include only questions relevant to car purchase analysis, plus out-of-scope entries if the user clearly asked something unrelated.
- Do not invent questions.
- Keep answers short.
- If there are no explicit questions, return an empty array.

This structured data should then be used by `ReportGenerationStage` to build the `Your questions answered` subsection.

---

## 8. Report Validator Changes

Current validation checks required headings, disclaimer, allowed verdict, estimated costs table, checkboxes, and unsafe certainty language.

Update validation carefully.

Recommended changes:

1. Keep existing required top-level heading validation.
2. Do not require `Your questions answered` as a top-level heading.
3. Add a warning if the report does not include the localized equivalent of `Your questions answered`.
4. Add a warning if the report contains explicit user questions in the analysis input but no answer-tracking subsection was generated. This may require passing metadata into validation, so it can be deferred if inconvenient.
5. Keep unsafe wording checks.
6. Add more unsafe wording patterns if needed:
   - “definitely safe”
   - “100% safe”
   - “certainly accident-free”
   - “seller is lying”
   - “fraud” unless presented as a general risk category and not an accusation.

Do not make the new subsection an error at first. Make it a warning to avoid blocking report generation during rollout.

---

## 9. Report Repair Stage Changes

Update `ReportRepairStage` so it preserves the new structure.

Add repair instructions:

- Preserve the `At a glance` table if present.
- Preserve the `Your questions answered` subsection if present.
- If user question answers are missing but the original report had them, restore them.
- Do not invent new user questions during repair.
- Do not re-analyze the vehicle.
- Only fix structure, localization, unsafe wording, missing required sections, markdown validity, and disclaimer.

---

## 10. Frontend: Create Dedicated Report Viewer

Create a dedicated component:

```txt
ReportMarkdownViewer
```

Purpose:

Render completed markdown reports as polished, read-only SaaS reports.

Do not rely on default markdown/browser styles.

### 10.1 Recommended Packages

Use:

```bash
npm install react-markdown remark-gfm rehype-sanitize github-slugger
```

If some packages already exist, reuse them.

Requirements:

- Use `react-markdown`.
- Enable GitHub-flavored markdown with `remark-gfm`.
- Sanitize rendered output with `rehype-sanitize`.
- Do not allow raw HTML from AI-generated reports.
- Provide custom renderers for headings, paragraphs, tables, lists, checkboxes, links, blockquotes, and horizontal rules.
- The component must be read-only.

### 10.2 Component Behavior

The component should:

- render headings with clear hierarchy;
- render tables with product-style UI;
- render markdown checkboxes as disabled checklist items;
- make wide tables horizontally scrollable on small screens;
- wrap long table cell content;
- highlight severity labels visually;
- style links safely;
- support dark theme;
- keep good spacing between sections.

### 10.3 Example Component Skeleton

```tsx
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeSanitize from "rehype-sanitize";

export function ReportMarkdownViewer({ markdown }: { markdown: string }) {
  return (
    <article className="report-viewer">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeSanitize]}
        components={{
          h1: ({ children }) => <h1 className="report-h1">{children}</h1>,
          h2: ({ children }) => <h2 className="report-h2">{children}</h2>,
          h3: ({ children }) => <h3 className="report-h3">{children}</h3>,
          p: ({ children }) => <p className="report-paragraph">{children}</p>,
          table: ({ children }) => (
            <div className="report-table-wrap">
              <table className="report-table">{children}</table>
            </div>
          ),
          th: ({ children }) => <th className="report-th">{children}</th>,
          td: ({ children }) => <td className="report-td">{children}</td>,
          ul: ({ children }) => <ul className="report-list">{children}</ul>,
          ol: ({ children }) => <ol className="report-ordered-list">{children}</ol>,
          li: ({ children }) => <li className="report-list-item">{children}</li>,
          hr: () => <hr className="report-divider" />,
          input: ({ checked }) => (
            <input
              type="checkbox"
              checked={checked}
              readOnly
              disabled
              className="report-checkbox"
            />
          ),
        }}
      >
        {markdown}
      </ReactMarkdown>
    </article>
  );
}
```

Adapt class names to the existing Tailwind/component system.

---

## 11. Frontend Visual Design Requirements

Use a premium dark report design.

Suggested colors:

```txt
Page background:      #070A0F
Report background:    #0F141B
Card background:      #111823
Border:               #243041
Main text:            #E5EAF2
Muted text:           #94A3B8
Accent blue:          #7EA2FF
Success:              #22C55E
Warning:              #F59E0B
Danger:               #EF4444
Unknown/Neutral:      #94A3B8
```

Suggested report layout:

```txt
max-width: 960px;
margin: 0 auto;
padding: 32px 24px 80px;
```

Suggested report card:

```txt
background: linear-gradient(180deg, #111823 0%, #0D121A 100%);
border: 1px solid rgba(148, 163, 184, 0.16);
border-radius: 24px;
box-shadow: 0 24px 80px rgba(0, 0, 0, 0.35);
padding: 40px;
```

Mobile:

```txt
padding: 20px;
border-radius: 18px;
max-width: 100%;
```

Typography:

```txt
h1: 32px, bold
h2: 26px, bold, margin-top 48px
h3: 18px, semibold, margin-top 28px
paragraph: 15–16px, line-height 1.75
```

---

## 12. Severity Styling

Implement visual styling for severity labels.

Recognize these patterns in rendered text/table cells:

- 🟢 Low
- 🟠 Medium
- 🔴 High
- ⚪ Unknown

Also support localized equivalents if simple and safe:

- English: Low, Medium, High, Unknown
- Polish: Niski, Średni, Wysoki, Nieznany
- German: Niedrig, Mittel, Hoch, Unbekannt
- Ukrainian: Низький, Середній, Високий, Невідомо
- French: Faible, Moyen, Élevé, Inconnu

Do not implement fragile full-report regex transformations unless necessary.

Preferred approach:

- add a small helper that detects severity text inside table cells or paragraphs;
- render a small badge component when the cell content is a simple severity value;
- otherwise leave text unchanged.

Badge styles:

```txt
Low: green border/background tint
Medium: amber border/background tint
High: red border/background tint
Unknown: slate/gray border/background tint
```

---

## 13. Report Page Shell

Do not show the markdown document alone.

The report page should have a shell around the markdown.

Recommended structure:

```txt
Back to reports

Report Header Card
- Generated title
- Created date
- Listing URL if available
- Status
- Verdict badge if available

Main Report Card
- Rendered markdown report

Bottom Actions
- Check another car
- Back to reports
```

The report header card should not duplicate the entire report. It should provide quick context.

---

## 14. Optional: Report Navigation

For longer reports, add simple section navigation.

Desktop:

- sticky sidebar with links:
  - Verdict
  - Key Risks
  - Missing Info
  - Questions
  - Checklist
  - Costs
  - Summary

Mobile:

- horizontal sticky tab row below the header.

Implementation can be done later if too large for this task, but the report viewer should be structured so this is easy to add.

---

## 15. My Reports List Improvements

Current list items use truncated input text, which is not ideal.

Improve the report list so completed reports show meaningful summary information.

Recommended list card:

```txt
[🟠 Buy with caution] [Medium risk]

Kia Sorento 2.2 CRDi 2021 · 7 seats
Main concern: imported vehicle, service history must be verified

Created: May 29, 2026
View report →
```

If structured summary fields are not yet available, keep the current title but improve visual styling.

Recommended future database fields:

```txt
verdict
overallRiskLevel
mainConcern
recommendedNextStep
confidence
```

These can be populated from `RiskAnalysisResult` or report generation output.

For MVP, avoid complex markdown parsing if it creates risk. Prefer storing structured fields from the AI pipeline.

---

## 16. Backend / Data Model Optional Improvement

If practical, save a structured report summary alongside the markdown report.

Suggested fields:

```txt
verdict
risk_level
main_concern
recommended_next_step
report_confidence
answered_user_questions_count
```

Benefits:

- better report list UI;
- easier filtering later;
- no need to parse markdown for badges;
- future analytics.

This is optional for the first implementation if it requires too much schema work.

---

## 17. Markdown Constraints

The AI report must follow these constraints:

- Markdown only.
- No raw HTML.
- No code fences.
- No internal implementation details.
- No model names.
- No prompt/stage/validator references.
- No confidence machinery explanation.
- No unsupported claims.
- No absolute safety guarantees.
- No direct accusations against the seller.
- Required localized top-level headings must remain in the required order.
- Subheadings are allowed inside sections.
- Tables should be readable and concise.
- Risk tables should normally contain no more than 5 rows unless necessary.

---

## 18. Language Requirements

The entire final report must be written in the requested report language.

This includes:

- headings;
- subheadings;
- verdict labels;
- table headers;
- table content;
- questions;
- checklist items;
- disclaimer;
- `Your questions answered` subsection.

Preserve exactly:

- VINs;
- URLs;
- brand names;
- model names;
- technical identifiers;
- engine/transmission names.

---

## 19. Safety Requirements

The report must avoid unsafe certainty language.

Do not use:

- “This car is safe.”
- “No risk.”
- “Zero risk.”
- “The seller is dishonest.”
- “The car was definitely damaged.”
- “Guaranteed accident-free.”
- “100% safe.”

Preferred language:

- “No major issue is visible from the available evidence.”
- “This should be verified during inspection.”
- “The available data is insufficient.”
- “This may indicate a risk.”
- “Ask the seller to confirm this in writing.”
- “A professional inspection is recommended.”

---

## 20. Implementation Order

Recommended order:

1. Create `ReportMarkdownViewer` with custom markdown rendering.
2. Replace current completed report rendering with `ReportMarkdownViewer`.
3. Improve report page shell: header card, metadata, actions.
4. Update `ReportGenerationStage` prompt with the new report rules.
5. Update `RiskAnalysisStage` to extract user questions and structured summary fields if practical.
6. Update `ReportRepairStage` to preserve new report blocks.
7. Add validator warnings for missing `Your questions answered` subsection if feasible.
8. Improve `My reports` list styling.
9. Optionally persist structured summary fields in DB.
10. Add sticky section navigation later if not part of first pass.

---

## 21. Acceptance Criteria

The task is complete when:

- Completed reports are rendered with a dedicated read-only markdown viewer.
- Tables are styled and readable.
- Checklists are displayed cleanly as disabled/read-only checklist items.
- Severity labels are visually distinguishable.
- The report page looks like a polished SaaS report, not raw markdown.
- The AI report includes an `At a glance` summary near the top.
- The AI report includes a `Your questions answered` subsection near the top.
- User questions from free-form input are answered if related to car purchase analysis.
- Unrelated user questions are marked as out of scope.
- Existing required headings and localized disclaimer still pass validation.
- The final report remains markdown-only in storage.
- No raw HTML from AI reports is rendered.
- The report remains fully localized according to selected report language.

---

## 22. Non-Goals

Do not implement in this task unless explicitly requested:

- PDF export;
- editable reports;
- public share links;
- full admin report editor;
- official vehicle history integration;
- complex charting;
- replacing markdown storage with JSON-only storage;
- changing the whole AI pipeline architecture.

---

## 23. Final Product Direction

AutoVerdict reports should feel like a practical expert pre-purchase memo.

The user should quickly understand:

1. the verdict;
2. the main risk;
3. what information is missing;
4. what questions to send to the seller;
5. what to check during inspection;
6. whether their own questions were answered.

The markdown remains the data format, but the browser experience must feel like a premium report viewer.
