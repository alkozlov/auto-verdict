# AutoVerdict — Ultra-Minimal SaaS UX/UI Design Specification

## 1. Product Direction

AutoVerdict is an ultra-minimal SaaS tool that helps inexperienced used-car buyers quickly understand whether a listing deserves attention, caution, or avoidance.

The product should feel like a calm AI analyst, not a car marketplace, not a complex dashboard, and not a generic chatbot.

The entire MVP experience happens on one authenticated app page. Future admin functionality may live on a separate page, but the buyer-facing product remains single-page.

### Core Product Promise

“Paste everything you know about a car. AutoVerdict turns it into a clear risk analysis and practical next steps.”

### Main UX Principle

The user’s primary working component is the multi-line input field. It is not just for copied ad text. It accepts:

- listing text;
- seller messages;
- VIN;
- inspection notes;
- service history notes;
- copied specifications;
- user questions;
- concerns;
- any context not available on the listing page.

The Otomoto link and images are optional supporting inputs.

---

## 2. Product Personality

### Desired Feeling

AutoVerdict should feel:

- minimal;
- calm;
- precise;
- trustworthy;
- practical;
- premium but not flashy;
- intelligent without looking like “AI hype.”

### Avoid

Avoid the following design directions:

- neon AI aesthetics;
- cyberpunk visuals;
- heavy automotive/racing style;
- dense dashboard UI;
- giant markdown document feeling;
- pure black background;
- decorative gradients as the main visual language;
- excessive animation;
- complicated navigation.

### Reference Feel

The product should borrow principles from:

- Linear: quiet SaaS polish;
- Vercel: minimal dark interface;
- Stripe: clarity and trust;
- Notion AI: simple interaction model;
- Perplexity: answer-first information hierarchy.

---

## 3. Information Architecture

### Public Route

`/`

Unauthenticated users see the landing/login screen.

### Authenticated Route

`/`

Authenticated users see the single-page app containing:

1. header;
2. main analysis input;
3. optional Otomoto link attachment;
4. optional image attachments;
5. submit button;
6. processing state;
7. latest/completed report view;
8. analysis history.

### Technical Route

`/auth/callback`

No designed UI required. It stores the JWT and redirects to `/`.

### Future Route

`/admin`

Separate future admin area. Not part of the buyer-facing MVP design.

---

## 4. Visual Design System

## 4.1 Theme

Primary theme: dark ultra-minimal SaaS.

The dark theme should not be pure black. Use layered graphite tones so the UI has depth without looking heavy.

---

## 4.2 Color Palette

### Backgrounds

| Token | Hex | Usage |
|---|---:|---|
| `bg-page` | `#0B0D10` | Main page background |
| `bg-surface` | `#111419` | Primary cards and editor background |
| `bg-surface-raised` | `#171B22` | Elevated cards, report sections |
| `bg-surface-soft` | `#1D222B` | Hover states, secondary blocks |
| `bg-input` | `#0F1217` | Textarea/editor field |

### Borders

| Token | Hex | Usage |
|---|---:|---|
| `border-subtle` | `#252B35` | Default borders |
| `border-strong` | `#343B49` | Focused containers, important sections |
| `border-focus` | `#6EA8FF` | Input focus state |

### Text

| Token | Hex | Usage |
|---|---:|---|
| `text-primary` | `#F4F6F8` | Main text |
| `text-secondary` | `#B6C0CC` | Body/helper text |
| `text-muted` | `#7F8A99` | Metadata, timestamps |
| `text-disabled` | `#596270` | Disabled text |

### Brand / Action

| Token | Hex | Usage |
|---|---:|---|
| `brand` | `#6EA8FF` | Primary action, links, focus |
| `brand-hover` | `#8BBAFF` | Button hover |
| `brand-soft` | `#162A45` | Soft brand background |

### Status / Risk

| Token | Hex | Usage |
|---|---:|---|
| `success` | `#4ADE80` | Buy, completed, positive signals |
| `success-soft` | `#12301F` | Success badge background |
| `warning` | `#FBBF24` | Buy with caution, missing info |
| `warning-soft` | `#33260A` | Warning badge background |
| `danger` | `#F87171` | Avoid, failed, high risk |
| `danger-soft` | `#3A1518` | Danger badge background |
| `info` | `#60A5FA` | Processing, neutral info |
| `info-soft` | `#10243D` | Info badge background |
| `unknown` | `#A78BFA` | Unknown/confidence-limited findings |
| `unknown-soft` | `#251B3D` | Unknown badge background |

### Light Report Surface

Reports may be easier to read on a very light surface inside the dark app, but this should be used carefully. Preferred MVP option: keep reports dark with structured cards. If a light report mode is used later:

| Token | Hex | Usage |
|---|---:|---|
| `report-bg-light` | `#F8FAFC` | Report paper surface |
| `report-text-light` | `#1F2937` | Light report text |
| `report-border-light` | `#E5E7EB` | Light report separators |

---

## 4.3 Typography

Recommended font stack:

```css
font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
```

Alternative premium option:

```css
font-family: Geist, Inter, ui-sans-serif, system-ui, sans-serif;
```

### Type Scale

| Role | Size | Weight | Line Height |
|---|---:|---:|---:|
| Page title | 32px | 650 | 1.15 |
| Section title | 22px | 650 | 1.25 |
| Card title | 17px | 600 | 1.35 |
| Body | 15px | 400 | 1.6 |
| Small body | 14px | 400 | 1.5 |
| Metadata | 12px | 500 | 1.4 |
| Button | 14px | 600 | 1.2 |

### Typography Rules

- Use sentence case, not all caps, except small status labels.
- Keep line length comfortable: 65–85 characters in reports.
- Use strong hierarchy: verdict first, details second.
- Avoid oversized headings inside report content.

---

## 4.4 Spacing

Use an 8px spacing system.

| Token | Value |
|---|---:|
| `space-1` | 4px |
| `space-2` | 8px |
| `space-3` | 12px |
| `space-4` | 16px |
| `space-5` | 20px |
| `space-6` | 24px |
| `space-8` | 32px |
| `space-10` | 40px |
| `space-12` | 48px |

Page padding:

- desktop: 32px;
- tablet: 24px;
- mobile: 16px.

Main content max width:

- `960px` for the single-page tool;
- report content inside this width;
- avoid full-width stretched text.

---

## 4.5 Radius

| Token | Value | Usage |
|---|---:|---|
| `radius-sm` | 8px | Small badges, pills |
| `radius-md` | 12px | Inputs, buttons |
| `radius-lg` | 16px | Cards |
| `radius-xl` | 24px | Main panels/modals |

---

## 4.6 Shadows

Use shadows very lightly. Dark SaaS interfaces should rely mostly on borders and surface contrast.

```css
--shadow-soft: 0 12px 40px rgba(0, 0, 0, 0.28);
--shadow-focus: 0 0 0 3px rgba(110, 168, 255, 0.18);
```

---

## 5. Components

## 5.1 Header

### Purpose

Persistent lightweight account/status bar.

### Desktop Layout

Left:

- AutoVerdict logo/name.

Right:

- user email;
- credits pill;
- sign out button.

### Mobile Layout

Left:

- AutoVerdict.

Right:

- credits pill;
- user menu icon or compact sign out.

Hide full email on small screens.

### Visual Style

- height: 56–64px;
- background: `bg-page` or translucent `rgba(11,13,16,0.82)`;
- bottom border: `1px solid border-subtle`;
- sticky header optional, not required.

### Credits Pill

Default:

```text
Credits: 2
```

Zero state:

```text
Credits: 0
```

Zero credits should use warning styling, not danger. It is a commercial prompt, not an error unless the user tries to submit.

---

## 5.2 Landing / Login Screen

### Goal

Convert unauthenticated visitors into Google sign-in.

### Layout

Centered panel with minimal content.

### Content

Product name:

```text
AutoVerdict
```

Headline:

```text
Avoid expensive used-car mistakes.
```

Subheadline:

```text
Paste listing details, seller messages, an Otomoto link, or photos. Get a structured AI risk analysis before you contact the seller.
```

CTA:

```text
Continue with Google
```

Trust note:

```text
AI-assisted screening only. Always verify documents and arrange a professional inspection before buying.
```

### Visual

No complex marketing site for MVP. Keep it one clear card.

---

## 5.3 Main Analysis Input Card

This is the most important component in the product.

### Purpose

Let users brief the AI analyst with all known information.

### Title

```text
Tell AutoVerdict what to analyze
```

### Helper Text

```text
Paste the listing text, seller messages, VIN, inspection notes, or ask specific questions. Add an Otomoto link or photos if you have them.
```

### Editor Placeholder

```text
Example: I’m considering this 2022 Toyota Corolla from Otomoto. The seller says it has full service history, but the ad does not show invoices. What risks should I check before calling?

Paste listing text, specs, seller replies, VIN, or your own questions here…
```

### Editor Behavior

- Required field.
- Multi-line rich markdown editor.
- Minimum visible height: 220px desktop, 180px mobile.
- Maximum comfortable height before internal scroll: 420px.
- Paste HTML should convert to markdown.
- Toolbar should be minimal and visually quiet.
- Toolbar can be hidden behind a small “Formatting” control if it makes the UI noisy.
- Focus state should be visible and premium.

### Validation

Empty submit:

```text
Add at least a short description, question, or copied listing text before analyzing.
```

---

## 5.4 Optional Attachments Row

Attachments sit under the main editor. They should feel optional, supportive, and lightweight.

### Buttons

Primary optional buttons:

```text
Add Otomoto link
Add photos
```

Do not use “Attach Link” because the product currently supports Otomoto crawling only.

Do not say “automoto.” Use “Otomoto.”

### Button Style

- secondary ghost/dashed buttons;
- small icon optional;
- border: dashed `border-strong`;
- hover background: `bg-surface-soft`.

---

## 5.5 Otomoto Link Attachment

### Add State

Clicking “Add Otomoto link” reveals an inline compact URL form.

Input placeholder:

```text
Paste Otomoto listing URL
```

Confirm button:

```text
Add link
```

Cancel button:

```text
Cancel
```

### Validation

Invalid URL:

```text
Enter a valid URL.
```

Non-Otomoto URL:

```text
For now, AutoVerdict can only crawl Otomoto.pl listings. You can still paste text from other sites into the main field.
```

This message is important. It prevents user frustration and explains the workaround.

### Added Link State

Show pill:

```text
Otomoto link added
```

With:

- domain preview;
- remove `×`;
- “Change” action.

Example:

```text
Otomoto link added · otomoto.pl/.../ID6...
```

---

## 5.6 Image Attachment

### Button Label

```text
Add photos
```

With counter after images are added:

```text
Add photos (2/5)
```

When max is reached, hide the button or show disabled:

```text
5 photos added
```

### Thumbnail Grid

- square thumbnails;
- 72px desktop;
- 64px mobile;
- rounded 12px;
- hover remove button;
- click opens lightbox.

### Validation Messages

Too many images:

```text
You can add up to 5 photos.
```

Wrong format:

```text
Photos must be JPEG, PNG, or WEBP.
```

Too large:

```text
Each photo must be 2.5 MB or smaller.
```

---

## 5.7 Submit Button

### Primary Label

```text
Analyze with AI
```

Alternative acceptable:

```text
Analyze listing
```

Preferred: “Analyze with AI” because the input is broader than a listing.

### Loading Label

```text
Submitting…
```

### Disabled Rules

Disable when:

- description is empty;
- currently submitting;
- no credits, unless payment prompt is shown instead.

### No Credits State

If user has zero credits and submits:

```text
You’re out of credits. Buy a check to analyze this listing.
```

When Stripe is implemented, show payment CTA inline.

---

## 5.8 Processing State

Processing should make the product feel intelligent and reduce uncertainty.

### Display Location

Immediately below the submit card, or replacing the submit card’s lower area after submission.

### Content

```text
Analyzing your car details…
```

Steps:

```text
Reading your notes
Checking Otomoto listing data
Reviewing photos
Detecting missing information
Generating recommendation
```

Important: if no Otomoto link or no photos were submitted, those steps should adapt.

Examples:

No link:

```text
Using your provided text and questions
```

No photos:

```text
Continuing without photos
```

### Status Badges

- Pending: yellow;
- Processing: blue;
- Completed: green;
- Failed: red.

---

## 5.9 Report Display on Same Page

Because the product must remain single-page, completed reports should open inline, not on a separate report page.

### Recommended Pattern

When a user clicks a history item or when a new analysis completes, show the report in an inline report panel below the submission area and above history.

This avoids a giant modal while still keeping everything on one page.

### Report Panel Structure

1. report header;
2. verdict summary card;
3. key risks/missing info;
4. structured report sections;
5. disclaimer.

### Do Not

Do not show the report as one giant markdown blob in a white modal. That makes the product feel less polished and harder to scan.

---

## 5.10 Report Header

### Content

- generated title;
- created timestamp;
- status badge;
- optional Otomoto link if provided;
- close/collapse action.

### Example

```text
Ford C-MAX 2005 diesel
Completed · May 27, 2026, 7:42 PM
```

Action:

```text
Collapse report
```

---

## 5.11 Verdict Summary Card

This is the most important part of the report.

### Purpose

Give the user the answer first.

### Verdicts

Use three verdicts:

```text
Buy
Buy with caution
Avoid
```

### Visual Treatment

Buy:

- green badge;
- success-soft background.

Buy with caution:

- amber badge;
- warning-soft background.

Avoid:

- red badge;
- danger-soft background.

### Summary Card Example

```text
Avoid

This listing has multiple unresolved risks and missing proof. Proceed only if the seller provides documentation and accepts a meaningful price reduction.

Main concerns
• Missing service history
• High mileage for age
• Timing belt replacement not confirmed

Recommended next step
Ask for VIN, service invoices, and timing belt proof before arranging inspection.
```

### Optional Risk Score

For MVP, a numeric risk score is optional. If implemented, use it carefully:

```text
Risk level: High
```

Prefer risk level over exact score unless the AI reliably produces structured scores.

Risk level values:

- Low;
- Medium;
- High;
- Unknown.

---

## 5.12 Report Sections

The report should preserve the nine PRD sections but present them as structured cards or accordions.

Recommended order in UI:

1. Verdict summary;
2. Missing information;
3. Listing risks;
4. Deal risks;
5. Model risks;
6. Estimated costs;
7. Questions for the seller;
8. Inspection checklist;
9. Listing facts;
10. Car summary;
11. Disclaimer.

The AI may generate the original markdown order, but the UI should prioritize decision-making. If the backend cannot produce structured fields yet, use markdown headings to parse and reorder later. For immediate MVP, render markdown cleanly but add a summary card above it.

### Section Card Style

- background: `bg-surface-raised`;
- border: `1px solid border-subtle`;
- radius: 16px;
- padding: 20–24px;
- spacing between cards: 16px.

### Accordion Behavior

Default expanded:

- Verdict summary;
- Missing information;
- Listing risks;
- Questions for the seller.

Default collapsed:

- Listing facts;
- Car summary;
- Model risks;
- Estimated costs;
- Inspection checklist.

This keeps the UI minimal while preserving detail.

---

## 5.13 Missing Information Section

This should become a first-class UX element.

### Why

In used-car buying, missing information is often as important as visible risk.

### Example

```text
Missing information

Ask the seller for:
• VIN
• service history invoices
• confirmation of accident-free status
• latest inspection report
• timing belt replacement proof
```

### Visual

Use amber/unknown styling. Missing information is not always a red flag, but it requires action.

---

## 5.14 Questions for Seller

Render as a clean checklist-like numbered list.

Each question should be easy to copy.

Optional future enhancement:

- copy all questions;
- copy individual question.

MVP can omit copy controls.

---

## 5.15 Inspection Checklist

Render markdown checkboxes as visual checklist rows.

The checkboxes may look interactive, but if they are not persisted, avoid implying saved state.

Preferred behavior:

- checkboxes can be toggled locally during the session;
- no persistence claim.

---

## 5.16 Estimated Costs

Render tables with strong readability.

Table style:

- no heavy grid;
- row separators only;
- right-align monetary values;
- keep PLN visible;
- allow horizontal scroll on mobile.

---

## 5.17 Analysis History

### Purpose

Let users reopen previous checks.

### Section Title

```text
Recent analyses
```

### Empty State

```text
No analyses yet.
Your completed checks will appear here.
```

### Card Content

Each item should show:

- title;
- status badge;
- created timestamp;
- short preview;
- verdict if available;
- optional risk level if available.

### Card Layout Example

```text
Ford C-MAX 2005 diesel
Completed · Avoid
May 27, 2026, 7:42 PM

High mileage, missing service proof, timing belt uncertainty…
```

### Click Behavior

Clicking a history item loads the report into the inline report panel on the same page.

### Pagination

Keep existing 5-per-page pagination.

Buttons:

```text
Previous
Page 1 of 3
Next
```

---

## 5.18 Modal Usage

Avoid using a large modal for full reports.

Allowed modals:

- image lightbox;
- maybe payment checkout prompt before Stripe redirect;
- small confirmation/error dialogs if absolutely needed.

Do not use report modal for long reports.

---

## 5.19 Image Lightbox

### Behavior

- full-screen overlay;
- dark backdrop;
- centered image;
- close button top-right;
- click backdrop closes;
- Escape closes.

### Style

- backdrop: `rgba(0,0,0,0.84)`;
- image max width: 92vw;
- image max height: 88vh;
- rounded corners optional.

---

## 6. Single-Page UX Flow

## 6.1 First-Time Authenticated User

1. User lands on main app page.
2. Header shows credits.
3. Main card explains what to paste.
4. User enters context in the multi-line field.
5. User optionally adds Otomoto link.
6. User optionally adds photos.
7. User clicks “Analyze with AI.”
8. Processing state appears.
9. History item appears with Pending/Processing.
10. When completed, inline report panel opens automatically.

---

## 6.2 Returning User

1. User sees main input card.
2. Recent analyses shown below.
3. User can create a new analysis or open a previous one.
4. Opening previous analysis displays report inline.

---

## 6.3 Failed Analysis

History item shows Failed.

Inline report panel displays:

```text
Analysis failed

We couldn’t complete this check.
Reason: [error reason]
```

If retry is not user-facing yet, do not show retry button.

If admin retry exists only internally, keep it admin-only.

---

## 6.4 No Credits Flow

When user has no credits and tries to submit:

```text
You’re out of credits.
Buy a check to analyze this car.
```

Future payment CTAs:

```text
Buy 1 check
Buy 5 checks
```

Until Stripe is implemented, show a clear disabled/payment-pending message or hide paid prompts from production.

---

## 7. Layout Specification

## 7.1 Desktop Layout

```text
Header

Main container, max-width 960px

[Intro microcopy]
[Main analysis input card]
[Processing state if active]
[Selected/latest report panel if open]
[Recent analyses]
```

### Container

```css
max-width: 960px;
margin: 0 auto;
padding: 32px;
```

---

## 7.2 Tablet Layout

```css
max-width: 760px;
padding: 24px;
```

The editor remains central.

---

## 7.3 Mobile Layout

```css
padding: 16px;
```

Rules:

- header email hidden;
- attachment buttons stack if needed;
- report tables horizontally scroll;
- cards use 16px padding;
- editor minimum height 180px;
- history cards full width.

---

## 8. Microcopy System

## 8.1 Brand Voice

AutoVerdict should sound:

- direct;
- cautious;
- helpful;
- non-alarmist;
- practical.

Avoid:

- “guaranteed safe”;
- “seller is lying”;
- “definitely damaged”;
- “scam detected.”

Prefer:

- “may indicate a risk”;
- “should be verified”;
- “available information is insufficient”;
- “ask the seller to clarify.”

---

## 8.2 Key UI Copy

### Landing Headline

```text
Avoid expensive used-car mistakes.
```

### Landing Subheadline

```text
Paste listing details, seller messages, an Otomoto link, or photos. Get a structured AI risk analysis before you contact the seller.
```

### Main App Intro

```text
Get a second opinion before contacting the seller.
```

### Main Input Title

```text
Tell AutoVerdict what to analyze
```

### Main Input Helper

```text
Paste the listing text, seller messages, VIN, inspection notes, or ask specific questions. Add an Otomoto link or photos if you have them.
```

### Otomoto Button

```text
Add Otomoto link
```

### Photos Button

```text
Add photos
```

### Submit Button

```text
Analyze with AI
```

### Processing Title

```text
Analyzing your car details…
```

### Empty History

```text
No analyses yet.
Your completed checks will appear here.
```

### Disclaimer

```text
AutoVerdict provides AI-assisted preliminary screening only. It does not replace professional inspection, vehicle history verification, legal checks, or independent expert advice.
```

---

## 9. Accessibility Requirements

- Minimum contrast ratio should meet WCAG AA.
- All buttons need visible focus states.
- Editor must have a proper label.
- Error messages should be associated with relevant inputs.
- Image remove buttons need accessible labels, e.g. “Remove photo.”
- Lightbox must support Escape to close.
- Status badges should not rely on color alone; always include text.
- Loading/processing state should be announced to screen readers using `aria-live="polite"`.

---

## 10. Implementation Notes for AI Agent

## 10.1 Recommended Component Structure

```text
App
├── AuthenticatedLayout
│   ├── Header
│   ├── MainContainer
│   │   ├── AppIntro
│   │   ├── AnalysisComposer
│   │   │   ├── MarkdownEditor
│   │   │   ├── OtomotoLinkAttachment
│   │   │   ├── PhotoAttachmentGrid
│   │   │   ├── FormErrors
│   │   │   └── SubmitButton
│   │   ├── ProcessingPanel
│   │   ├── InlineReportPanel
│   │   │   ├── ReportHeader
│   │   │   ├── VerdictSummaryCard
│   │   │   ├── ReportSectionAccordion
│   │   │   └── Disclaimer
│   │   └── AnalysisHistory
│   └── ImageLightbox
└── LoginScreen
```

---

## 10.2 Data Handling

The current backend can continue returning markdown reports.

For best UI quality, future backend should return structured JSON in addition to markdown:

```json
{
  "verdict": "avoid",
  "riskLevel": "high",
  "confidence": "medium",
  "summary": "...",
  "mainConcerns": ["..."],
  "recommendedNextStep": "...",
  "missingInformation": ["..."],
  "sections": {
    "carSummary": "...",
    "listingFacts": "...",
    "modelRisks": "...",
    "listingRisks": "...",
    "dealRisks": "...",
    "estimatedCosts": "...",
    "questionsForSeller": ["..."],
    "inspectionChecklist": ["..."],
    "recommendation": "..."
  }
}
```

MVP fallback:

- render markdown;
- parse headings if possible;
- add summary UI only if verdict can be extracted reliably.

---

## 10.3 CSS Tokens

```css
:root {
  --bg-page: #0B0D10;
  --bg-surface: #111419;
  --bg-surface-raised: #171B22;
  --bg-surface-soft: #1D222B;
  --bg-input: #0F1217;

  --border-subtle: #252B35;
  --border-strong: #343B49;
  --border-focus: #6EA8FF;

  --text-primary: #F4F6F8;
  --text-secondary: #B6C0CC;
  --text-muted: #7F8A99;
  --text-disabled: #596270;

  --brand: #6EA8FF;
  --brand-hover: #8BBAFF;
  --brand-soft: #162A45;

  --success: #4ADE80;
  --success-soft: #12301F;
  --warning: #FBBF24;
  --warning-soft: #33260A;
  --danger: #F87171;
  --danger-soft: #3A1518;
  --info: #60A5FA;
  --info-soft: #10243D;
  --unknown: #A78BFA;
  --unknown-soft: #251B3D;

  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-xl: 24px;

  --shadow-soft: 0 12px 40px rgba(0, 0, 0, 0.28);
  --shadow-focus: 0 0 0 3px rgba(110, 168, 255, 0.18);
}
```

---

## 11. MVP Design Priorities

### Must Do Now

1. Make the multi-line AI briefing field the hero component.
2. Rename link action to “Add Otomoto link.”
3. Keep image upload optional and secondary.
4. Replace report modal with inline same-page report panel.
5. Add clear verdict-first report summary.
6. Improve color system, spacing, typography, and visual hierarchy.
7. Add helpful processing states.
8. Improve history cards with status/verdict clarity.

### Should Do Soon

1. Add structured AI JSON output.
2. Add missing-information section as a first-class output.
3. Add confidence level.
4. Add copy-friendly seller questions.
5. Add payment prompt UX once Stripe is implemented.

### Avoid for MVP

1. Multi-page user dashboard.
2. Complex onboarding.
3. Comparison tools.
4. PDF export.
5. Marketplace expansion UI.
6. Heavy admin features in buyer UI.
7. Chat-style conversation interface.

---

## 12. Final Design Concept

AutoVerdict should be a one-page, ultra-minimal SaaS product centered around one powerful action:

```text
Tell AutoVerdict what you know about the car.
```

Everything else is optional support.

The strongest MVP experience is:

1. user provides context;
2. AI analyzes it;
3. user receives a verdict-first report;
4. report gives risks, missing information, seller questions, and inspection steps;
5. history stores previous analyses.

The UI should never compete with the analysis. It should make the analysis feel credible, calm, readable, and immediately useful.

