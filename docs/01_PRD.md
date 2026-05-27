# Product Requirements Document

## 1. Product Name

Working name: **Auto Verdict**.

Domain candidate: `auto-verdict.app`.

The name is not final. During MVP, validating the product hypothesis is more important than final branding.

---

## 2. Product Vision

Build a SaaS product that helps inexperienced used car buyers make safer preliminary decisions about car listings by detecting potential risks, inconsistencies, missing information, suspicious wording, and practical follow-up questions.

The product acts as an AI-assisted screening tool. It does not replace:

- professional vehicle inspection;
- mechanic diagnostics;
- official vehicle history reports;
- legal verification;
- independent expert evaluation.

---

## 3. Problem Statement

Buying a used car involves significant financial risk. Inexperienced buyers often struggle to:

- notice suspicious wording in listings;
- compare seller claims with vehicle history;
- understand common problems of a specific model;
- know which documents to request;
- decide whether a listing is worth further attention;
- prepare for a seller call or vehicle inspection.

Many listings look attractive but may hide problematic history: company ownership, import, accidents, incomplete service records, mileage inconsistencies, short ownership periods, vague claims, or mismatches between listing text and public records.

---

## 4. Target Users

### Primary Persona: Inexperienced Private Buyer

A private buyer who:

- is looking for a family car;
- is not a car expert;
- has limited experience with the used car market;
- may have previously bought only new cars from dealerships;
- fears dishonest sellers or hidden defects;
- is willing to pay a few euros for a preliminary listing check;
- compares multiple listings and wants to filter risky ones quickly.

### Secondary Persona: Cautious Buyer

A user who has some experience but wants a structured second opinion before contacting sellers.

### Future Persona: Car Selection Assistant / Small Broker

A user who reviews many listings for clients and needs quick, structured pre-screening.

This persona is outside MVP scope.

---

## 5. Initial Market

- Country: Poland.
- Primary marketplace: Otomoto.pl.
- Vehicle segment: relatively recent used cars, approximately model year 2022 or newer.
- Language: English (UI, reports, internal documentation).

---

## 6. Value Proposition

The system gives users a practical, structured AI report that answers:

- Does this listing look risky?
- What information is missing?
- Are there inconsistencies?
- What questions should I ask the seller?
- What should I verify before inspection?
- What model-specific issues should I know about?
- Should I proceed, request more information, or avoid the listing?

---

## 7. Current MVP State

The following features are **implemented and functional**.

### Authentication

- Users sign in with Google (OAuth 2.0).
- The backend issues a JWT after completing the OAuth handshake.
- The frontend stores the token and sends it as a Bearer header on all API requests.
- New users are created automatically on first sign-in.
- New users receive a configurable number of free analysis credits.

### Check Submission

Users can submit a car check by providing:

| Field | Required | Constraints |
|-------|----------|-------------|
| Description | Yes | Free-form text; any combination of copied listing text, seller messages, specs, VIN, inspection notes |
| Listing URL | No | Otomoto.pl URLs only; page is auto-crawled by the system |
| Images | No | Up to 5 files; JPEG, PNG, or WEBP; max 2 560 KB each |

- Submitting a check costs one credit.
- If the user has no credits, the API returns HTTP 402 and the check is not created.
- The system auto-generates a title from the first 120 characters of the description.

### Analysis Pipeline

When a check is submitted:

1. The API saves the check record (status: Pending) and queues a message via NATS JetStream.
2. The Processing Service picks up the message.
3. If a listing URL was provided and belongs to Otomoto.pl, the service crawls the page with a headless Chromium browser (Playwright), extracting structured listing data and a full-page screenshot.
4. Crawled data, the screenshot, and any user-uploaded images are sent to the Claude AI API.
5. Claude generates a structured markdown report.
6. The report is saved to blob storage; the check record is updated (status: Completed).
7. On failure, the check is marked Failed with an error reason.

The Processing Service applies per-domain rate limiting when crawling: configurable min/max delay between requests and per-domain concurrency cap.

Crawl failures degrade gracefully — if the page cannot be crawled, the AI analysis proceeds using the user-provided description and images only.

### AI Report

The AI generates a structured markdown document with the following nine sections in fixed order:

1. **Car Summary** — one-paragraph overview of the vehicle.
2. **Listing Facts** — key facts: make, model, year, mileage, price, seller type, location, URL.
3. **Model Risks** — known model-specific technical issues and common faults.
4. **Listing Risks** — red flags found in the listing text or images.
5. **Deal Risks** — financial, legal, and transactional risks.
6. **Estimated Costs** — table of one-time and first-year purchase costs in PLN.
7. **Questions for the Seller** — numbered list of questions to ask before buying.
8. **Inspection Checklist** — checkboxes for physical inspection items.
9. **Recommendation** — a direct verdict: **buy**, **buy with caution**, or **avoid**.

The report ends with a standard disclaimer. All monetary estimates are in PLN.

### Check History

- Users can view their past checks in reverse-chronological order.
- The list is paginated (5 per page).
- Clicking a check opens a modal displaying the full report.
- The frontend polls for updates every 5 seconds so the status changes (Pending → Processing → Completed) appear automatically without a page refresh.

### Credit System

- Each check costs one credit.
- Credits are shown in the header.
- Users with zero credits see an error on submission.
- An admin whitelist bypasses the credit check (for internal accounts).

### Infrastructure

- **Database:** PostgreSQL — user accounts, credits, check records.
- **Messaging:** NATS JetStream — reliable, at-least-once delivery between API and Processing Service.
- **Blob storage:** SeaweedFS (S3-compatible) — uploaded images, crawled screenshots, AI reports.
- **Reverse proxy:** Nginx routes `/api/*` to the backend and all other paths to the frontend.

---

## 8. Planned for MVP Completion

The following features are **not yet built** but are required before public launch.

### Payment (Stripe)

- Single check purchase.
- Package of five checks.
- Stripe webhooks are authoritative for granting credits.
- No subscription billing.

### Admin Operations (Minimal)

- View check processing status.
- Retry failed checks.
- Manual credit adjustment.
- Basic credit balance inspection.

### Free Credit Lifecycle

New users must receive free credits automatically (already implemented). The full payment flow will be added after the free-credit lifecycle is verified end to end.

---

## 9. Explicitly Excluded from MVP

- Full automatic Otomoto scraping (the current crawler handles single on-demand listings).
- Guaranteed official vehicle history integration.
- Browser extension.
- Mobile app.
- Subscription billing.
- B2B workflows.
- Multi-marketplace Europe-wide support (Mobile.de, etc.).
- Automatic VIN lookup on paid third-party services.
- PDF report export.
- Complex admin panel.
- Presigned direct browser uploads to object storage.
- Report language selection (English only for now).

---

## 10. Screens and User Flows

This section describes the required screens for the UI/UX design.

### 10.1 Screen: Landing / Login

**Route:** `/`  
**Shown to:** unauthenticated users

**Purpose:** Entry point for new and returning users who are not signed in.

**Elements:**
- Product name: **AutoVerdict**
- Tagline: "AI-powered car listing analysis. Spot risks, verify facts, and get a purchase recommendation."
- Primary CTA: **Sign in with Google** (redirects to `/api/auth/google`)

**States:**
- Default (single state — no loading, no errors)

---

### 10.2 Screen: Main App

**Route:** `/`  
**Shown to:** authenticated users

**Purpose:** The core product screen. Users submit car checks here and view their history.

#### 10.2.1 Header (persistent)

| Element | Notes |
|---------|-------|
| Logo / product name | Left-aligned |
| User email | Right area, hidden on small screens |
| Credit balance | Right area; label "Credits:" + number |
| Sign out | Right area; clears token, returns to login screen |

#### 10.2.2 Submission Form

The form is the primary action area. It lets users provide all the information needed for an AI analysis.

**Description editor (required)**

- A rich markdown editor with toolbar (bold, italic, lists, etc.).
- Placeholder / hint text: "Paste the listing text here — ad copy, seller messages, specs, VIN, inspection notes, anything relevant."
- Pasting HTML content (e.g., from a browser) automatically converts it to markdown.
- Validation: if empty on submit, shows an inline error.

**Image attachment (optional)**

- Up to 5 images can be attached.
- Accepted formats: JPEG, PNG, WEBP. Max size: 2 560 KB per file.
- Attached images shown as square thumbnails with a hover-to-remove × button.
- Clicking a thumbnail opens a lightbox (full-size preview).
- A dashed "Attach Images" button toggles a hidden file input.
- Counter shows current / max (e.g., "Attach Images (2/5)").
- Button disappears when 5 images are attached.

**Listing link attachment (optional)**

- A dashed "Attach Link" button reveals a URL input inline.
- Accepted URLs: any valid URL (backend enforces Otomoto.pl domain restriction).
- After confirming, the link is shown as a pill with a × to remove.
- "Change Link" replaces the existing link.
- Inline validation shows if the entered text is not a valid URL.

**Error messages**

- Displayed below the attachment buttons area.
- Cover: missing description, image validation failures, API errors.

**Submit button**

- Label: "Analyze Listing"
- Full-width.
- Disabled during submission with label "Submitting…".
- Deducts one credit and queues the analysis.
- Shows HTTP 402 error if the user has insufficient credits.

#### 10.2.3 Analysis History

Below the form, a list of the user's previous checks.

**Empty state:** "No analyses yet."

**List item (per check):**

| Element | Notes |
|---------|-------|
| Title | First 120 chars of description, or listing URL if no description title generated, or "Listing analysis" as fallback |
| Status badge | Colour-coded pill: Pending (yellow), Processing (blue), Completed (green), Failed (red) |
| Created date | Localised timestamp below the title |

Clicking a list item opens the Check Modal.

**Pagination:**

- 5 items per page.
- Previous / Next buttons appear when there is more than one page.
- Current page number shown between buttons.

---

### 10.3 Screen: Check Modal

**Trigger:** clicking any check in the history list  
**Type:** overlay modal, scrollable

**Purpose:** Display the full AI analysis report or the current status if processing is still in progress.

#### States

**Loading state**
- Shown while fetching check details from the API.
- Simple "Loading…" text.

**In-progress state** (Pending or Processing)
- Shows: check title + status badge + "— analysis in progress" text.
- No report content yet.

**Completed state**
- Renders the full markdown report.
- Nine sections as defined in Section 7 of this document.
- Uses a markdown renderer (preserves headings, bullets, tables, checkboxes).
- The Inspection Checklist section renders interactive-looking checkboxes.

**Failed state**
- Shows: "Analysis failed" label + error reason text.

**Common elements:**
- Close button (×) in the top-right corner.
- Clicking the backdrop closes the modal.

---

### 10.4 Screen: Auth Callback

**Route:** `/auth/callback`  
**Type:** Technical redirect handler — no user-visible design needed.

After Google OAuth completes, the backend redirects to this page with the JWT token in the URL query string. The page extracts and stores the token, then redirects to `/`.

---

### 10.5 Image Lightbox

**Trigger:** clicking an image thumbnail in the submission form

**Type:** full-screen overlay

- Shows the full-size image centred on a dark backdrop.
- Close button (×) top-right.
- Clicking the backdrop closes it.

---

## 11. Check Status Flow

```
[Form submitted]
       │
       ▼
   Pending          ← check saved, NATS message queued
       │
       ▼
  Processing        ← ProcessingService picked up the message
       │
       ├─ success ──► Completed    ← report saved to blob storage
       │
       └─ failure ──► Failed       ← reason stored in DB
```

The frontend polls every 5 seconds and automatically reflects status changes in the history list without requiring a page refresh. The modal also shows live status when a processing check is open.

---

## 12. Pricing Model

MVP uses a credit system.

- New users receive a configurable number of free credits on first sign-in (default: 1–2).
- One credit = one car check.
- Credits are shown in the header at all times.
- When the balance reaches zero, submitting a new check shows a payment prompt (design TBD, Stripe integration pending).

Payment options (planned, not yet implemented):
- Single check — e.g., €2–3.
- Package of five checks — e.g., €8–10.

Stripe webhooks will be the authoritative signal for granting credits. Exact pricing is subject to market validation.

---

## 13. Positioning and Safety

The product must not present its output as a final guarantee.

Avoid absolute statements such as:

- "This car is safe."
- "This seller is dishonest."
- "This car was definitely damaged."

Prefer cautious wording:

- "This may indicate a risk."
- "This should be clarified with the seller."
- "The available data is insufficient."
- "A professional inspection is recommended."

Every AI report ends with a standard disclaimer confirming the analysis does not substitute for professional inspection.

---

## 14. Success Metrics

MVP success can be measured by:

- number of registered users;
- number of created checks;
- percentage of users who use free checks;
- percentage of users who buy paid credits;
- average checks per user;
- report completion rate (Completed vs. total);
- AI processing failure rate;
- user feedback on report usefulness.

---

## 15. Key Risks

- AI hallucination or unsupported claims.
- Low-quality user input leading to shallow reports.
- Otomoto.pl scraping limitations (CAPTCHAs, layout changes).
- Legal risk from overly definitive recommendations.
- Payment / webhook processing errors.
- Storage and data cleanup at scale.
- NATS message processing failures and retries.
- Claude API cost and availability.

---

## 16. MVP Hypothesis

A private buyer considering a used car worth thousands of euros is willing to pay a small fee for a clear AI-assisted preliminary analysis that helps avoid risky listings and prepares them for seller communication.
