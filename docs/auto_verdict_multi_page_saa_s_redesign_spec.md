# AutoVerdict — Multi-Page SaaS Redesign Specification

## Purpose of This Document

This document describes the redesigned AutoVerdict product structure, user experience, page layouts, visual direction, and implementation instructions for an AI agent or frontend developer.

The goal is to evolve AutoVerdict from a single-page MVP tool into a clearer, more scalable SaaS product with:

- a public SEO-friendly home page;
- a private authenticated workspace called Garage;
- a dedicated car-check flow;
- a dedicated reports list;
- full report pages;
- a future-ready structure for payments, PDF export, and admin operations.

This document intentionally focuses on user experience, interface behavior, content structure, and design expectations. The implementing agent should handle the technical details according to the existing frontend/backend architecture.

---

# 1. Product Positioning

AutoVerdict helps used-car buyers avoid expensive mistakes by turning listing details, seller messages, Otomoto links, photos, VINs, inspection notes, and user questions into a structured AI-assisted risk analysis.

The product must not feel like a generic AI chatbot. It should feel like a calm, focused SaaS assistant for car-buying decisions.

## Core Promise

```text
Avoid expensive used-car mistakes before contacting the seller.
```

## Product Role

AutoVerdict is not a mechanic, legal advisor, or official history report provider. It is a preliminary AI screening tool that helps users decide what to verify before spending time or money on a car.

## Emotional Goal

The user should feel:

- less anxious;
- more informed;
- more prepared;
- more confident about what to ask the seller;
- protected from obvious risks;
- clear about the next step.

---

# 2. Recommended SaaS Structure

The app should move from one monolithic page to a classic SaaS structure:

```text
/                         Public home page
/auth/callback            OAuth callback handler
/garage                   Authenticated app redirect / overview
/garage/check             Check car page
/garage/reports           My reports page
/garage/reports/:id       Full report page
/admin                    Future admin area, separate from buyer UI
```

## Important Principle

Separate marketing intent from product usage intent.

The public home page sells the product and supports SEO.

The Garage area is the authenticated workspace where users actually use the product.

---

# 3. High-Level User Flows

## 3.1 New Visitor Flow

1. User visits `/`.
2. User sees the public home page.
3. User understands what AutoVerdict does.
4. User clicks `Continue with Google`.
5. OAuth flow starts.
6. User is redirected back through `/auth/callback`.
7. User lands in `/garage/check`.
8. User creates their first car check.

## 3.2 Returning Logged-In User Flow

1. User visits `/`.
2. Instead of Google login, user sees a primary button: `Go to Garage`.
3. User clicks it.
4. User lands in `/garage/check`.

## 3.3 Check Car Flow

1. User goes to `/garage/check`.
2. User enters context into the main multi-line AI briefing field.
3. User optionally adds an Otomoto link.
4. User optionally adds photos.
5. User clicks `Analyze with AI`.
6. The check is submitted.
7. The page shows a progress tracker.
8. When completed, user is shown a clear path to open the report.
9. User can go to the report page or check another car.

## 3.4 Reports Flow

1. User goes to `/garage/reports`.
2. User sees a compact paginated list of previous checks.
3. User clicks a report.
4. User opens `/garage/reports/:id`.
5. The report is displayed in full page format.
6. User may export to PDF if the feature is available.

---

# 4. Visual Design Direction

## 4.1 Style

AutoVerdict should feel like an ultra-minimal SaaS product.

Use:

- dark-first interface;
- calm graphite surfaces;
- restrained blue accents;
- generous spacing;
- clean typography;
- low visual noise;
- premium but not flashy cards;
- practical, decision-oriented layouts.

Avoid:

- neon AI visuals;
- automotive racing styling;
- excessive gradients;
- dashboard clutter;
- huge decorative illustrations;
- unnecessary animation;
- playful colors;
- generic template look.

## 4.2 Design Mood

The design mood should be:

```text
quiet confidence
```

Not aggressive. Not over-designed. Not sterile. The product should feel trustworthy enough for a user making a multi-thousand-euro purchase decision.

---

# 5. Color Palette

Use a consistent token-based palette.

## 5.1 Backgrounds

```css
--bg-page: #0B0D10;
--bg-shell: #0E1116;
--bg-surface: #111419;
--bg-surface-raised: #171B22;
--bg-surface-soft: #1D222B;
--bg-input: #0F1217;
```

## 5.2 Borders

```css
--border-subtle: #252B35;
--border-strong: #343B49;
--border-focus: #6EA8FF;
```

## 5.3 Text

```css
--text-primary: #F4F6F8;
--text-secondary: #B6C0CC;
--text-muted: #7F8A99;
--text-disabled: #596270;
```

## 5.4 Brand / Action

```css
--brand: #6E8FEA;
--brand-hover: #7C9AF0;
--brand-soft: #162A45;
```

## 5.5 Status / Risk

```css
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
```

## 5.6 General Color Rules

- Do not introduce random colors.
- Use blue only for primary action, links, and focus states.
- Use amber for credits, caution, missing info, and pending states.
- Use green for completed/safe-positive states.
- Use red only for avoid, failed, or serious error states.
- Do not use pure black as the main background.

---

# 6. Typography

## 6.1 Font

Use Inter or Geist.

Recommended stack:

```css
font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
```

## 6.2 Type Scale

```css
--text-xs: 12px;
--text-sm: 14px;
--text-md: 15px;
--text-lg: 18px;
--text-xl: 22px;
--text-2xl: 30px;
--text-3xl: 42px;
```

## 6.3 Usage

Public home hero headline:

```css
font-size: 42px;
line-height: 1.08;
font-weight: 680;
```

Dashboard page title:

```css
font-size: 28px;
line-height: 1.15;
font-weight: 650;
```

Section title:

```css
font-size: 18px;
font-weight: 650;
```

Body text:

```css
font-size: 15px;
line-height: 1.6;
```

Metadata:

```css
font-size: 13px;
color: var(--text-muted);
```

## 6.4 Typography Rules

- Use sentence case, not all caps, for most headings.
- Avoid oversized text inside authenticated app pages.
- Keep public landing page more expressive than app pages.
- Report pages should prioritize readability over decoration.

---

# 7. Layout System

## 7.1 Public Home Layout

Home page max width:

```css
max-width: 1120px;
margin: 0 auto;
padding: 0 32px;
```

Mobile padding:

```css
padding: 0 20px;
```

## 7.2 Garage Layout

Authenticated app should use a shell with sidebar.

```text
+------------------------------------------------+
| Sidebar | Main content                         |
|         |                                      |
|         |                                      |
+------------------------------------------------+
```

Sidebar width:

```css
width: 260px;
```

Main content max width:

```css
max-width: 980px;
```

Report page max width:

```css
max-width: 1040px;
```

Main content padding:

```css
padding: 40px;
```

Tablet:

```css
padding: 28px;
```

Mobile:

```css
padding: 20px;
```

---

# 8. Page 1 — Public Home Page

Route:

```text
/
```

## 8.1 Purpose

The public home page has two jobs:

1. Convert visitors into Google sign-in.
2. Provide enough product information to support SEO and trust.

It should not feel like a huge corporate marketing website. It should be a clean SaaS landing page.

## 8.2 Header

Desktop header:

Left:

```text
AutoVerdict
```

Right, if not logged in:

```text
Continue with Google
```

Right, if logged in:

```text
Go to Garage
```

Optional secondary link:

```text
How it works
```

Do not show full app navigation on the public home page.

## 8.3 Hero Section

### Hero Headline

```text
Avoid expensive used-car mistakes.
```

### Hero Subheadline

```text
Paste listing text, seller messages, an Otomoto link, photos, VIN, or your own questions. AutoVerdict gives you a structured AI risk analysis before you contact the seller.
```

### Primary CTA

If logged out:

```text
Continue with Google
```

If logged in:

```text
Go to Garage
```

### Secondary Microcopy

```text
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

## 8.4 Hero Visual

Use a minimal preview card, not a complex illustration.

The preview card should show a simplified report summary:

```text
Buy with caution

Main concerns
• Missing service history
• Imported vehicle history unclear
• Seller description lacks details

Recommended next step
Ask for VIN, invoices, and accident history before inspection.
```

This preview should communicate the value in five seconds.

## 8.5 “How It Works” Section

Three cards:

### Step 1

```text
Paste what you know
```

Description:

```text
Add listing text, seller replies, VIN, notes, photos, or an Otomoto link.
```

### Step 2

```text
AI reviews the risks
```

Description:

```text
AutoVerdict checks for missing information, suspicious wording, deal risks, and model-specific concerns.
```

### Step 3

```text
Get practical next steps
```

Description:

```text
Receive seller questions, inspection points, estimated costs, and a clear recommendation.
```

## 8.6 “What AutoVerdict Checks” Section

Use a two-column section with compact items.

Items:

- suspicious wording;
- missing service history;
- mileage concerns;
- import-related uncertainty;
- accident or repair ambiguity;
- unclear seller claims;
- model-specific common issues;
- first-year ownership costs;
- questions to ask before viewing;
- inspection checklist.

## 8.7 “Who It Is For” Section

Title:

```text
Built for cautious used-car buyers.
```

Content:

```text
AutoVerdict is designed for private buyers who are not car experts and want a structured second opinion before calling the seller, arranging inspection, or spending money on a risky listing.
```

## 8.8 Safety / Disclaimer Section

Title:

```text
A screening tool, not a guarantee.
```

Content:

```text
AutoVerdict helps you identify questions and risks. It does not replace professional diagnostics, legal verification, official vehicle history reports, or an independent inspection.
```

This section is important for trust and legal positioning.

## 8.9 SEO Content Section

Add SEO-friendly copy below the main marketing sections.

Suggested heading:

```text
AI used-car listing analysis for buyers in Poland
```

Suggested copy:

```text
Buying a used car in Poland often means comparing listings, checking seller claims, reviewing service history, and deciding whether a car is worth inspecting. AutoVerdict helps buyers analyze Otomoto listings and other seller-provided information by highlighting possible risks, missing details, practical seller questions, and inspection points.
```

Suggested additional subheadings:

```text
Why analyze a used-car listing before contacting the seller?
```

```text
What information should you check before buying a used car?
```

```text
How AutoVerdict helps with Otomoto listings
```

Keep SEO content useful and readable. Do not keyword-stuff.

## 8.10 Home Page Footer

Footer links:

```text
AutoVerdict
Privacy
Terms
Contact
```

If Privacy/Terms pages do not exist yet, either omit the links or create simple placeholder pages before public launch.

---

# 9. Authenticated App Shell — Garage

Base route:

```text
/garage
```

## 9.1 Purpose

Garage is the user’s private workspace.

It should feel like:

```text
your saved place for car checks and reports
```

Not a complex dashboard.

## 9.2 Default Redirect

`/garage` should redirect to:

```text
/garage/check
```

unless a future overview page is intentionally added.

## 9.3 Sidebar Layout

Desktop sidebar should be fixed or sticky on the left.

Sidebar content:

Top:

```text
AutoVerdict
```

Navigation:

```text
Check car
My reports
```

Bottom:

```text
Credits: 2
user@email.com
Sign out
```

## 9.4 Sidebar Visual Style

```css
width: 260px;
background: #0E1116;
border-right: 1px solid rgba(255,255,255,0.06);
padding: 24px;
```

Navigation item:

```css
height: 42px;
border-radius: 12px;
padding: 0 12px;
font-size: 14px;
font-weight: 500;
color: var(--text-secondary);
```

Active item:

```css
background: var(--bg-surface-raised);
color: var(--text-primary);
```

Hover:

```css
background: rgba(255,255,255,0.04);
```

## 9.5 Mobile Navigation

On mobile, do not use a permanent sidebar.

Use:

- top app bar;
- compact menu button;
- slide-out drawer or bottom navigation.

Mobile top bar:

Left:

```text
AutoVerdict
```

Right:

```text
Credits: 2
Menu
```

Mobile drawer items:

```text
Check car
My reports
Sign out
```

---

# 10. Page 2 — Check Car

Route:

```text
/garage/check
```

## 10.1 Purpose

This page is the primary product action page.

It allows the user to create a new AI analysis.

## 10.2 Page Header

Title:

```text
Check car
```

Subtitle:

```text
Paste everything you know about the car. AutoVerdict will turn it into a structured risk analysis.
```

Optional right-side credit pill:

```text
Credits: 2
```

## 10.3 Main Composer Card

The multi-line input remains the hero component.

### Card Title

```text
Tell AutoVerdict what to analyze
```

### Helper Text

```text
Paste listing text, seller messages, VIN, inspection notes, or ask specific questions. Add an Otomoto link or photos if you have them.
```

### Editor Placeholder

```text
Paste listing text, seller messages, VIN, concerns, inspection notes, or ask AutoVerdict specific questions.

Example:
"I'm considering this Toyota Corolla from Otomoto. What should I verify before contacting the seller?"
```

### Editor Requirements

- Required field.
- Multi-line editor.
- Markdown support may remain.
- Toolbar should be visually quiet.
- Minimum height: 220px desktop, 180px mobile.
- Empty validation should be inline.

### Empty Validation Message

```text
Add at least a short description, question, or copied listing text before analyzing.
```

## 10.4 Optional Otomoto Link

Button label:

```text
Add Otomoto link
```

When clicked, reveal inline URL field.

Input placeholder:

```text
Paste Otomoto listing URL
```

Validation for invalid URL:

```text
Enter a valid URL.
```

Validation for unsupported domain:

```text
For now, AutoVerdict can only crawl Otomoto.pl listings. You can still paste text from other sites into the main field.
```

Added state:

```text
Otomoto link added · otomoto.pl/...
```

Actions:

```text
Change
Remove
```

## 10.5 Optional Photos

Button label:

```text
Add photos
```

After adding:

```text
Add photos (2/5)
```

Rules:

- up to 5 images;
- JPEG, PNG, WEBP;
- max 2.5 MB each;
- show square thumbnails;
- clicking thumbnail opens lightbox;
- remove button on each thumbnail.

## 10.6 Submit Button

Primary label:

```text
Analyze with AI
```

Loading label:

```text
Submitting…
```

No credits message:

```text
You’re out of credits. Buy a check to analyze this car.
```

Future payment buttons:

```text
Buy 1 check
Buy 5 checks
```

## 10.7 Progress Tracker

After submission, show a progress panel on the same page.

Title:

```text
Analyzing your car details…
```

Possible steps:

```text
Reading your notes
Checking Otomoto listing data
Reviewing uploaded photos
Detecting missing information
Generating recommendation
```

Adapt steps to submitted data.

If no Otomoto link:

```text
Using your provided text and questions
```

If no photos:

```text
Continuing without photos
```

## 10.8 Completed State

When completed, show success panel:

```text
Analysis complete
Your report is ready.
```

Buttons:

```text
Open report
Check another car
```

`Open report` navigates to:

```text
/garage/reports/:id
```

`Check another car` resets composer.

## 10.9 Failed State

Show:

```text
Analysis failed
We couldn’t complete this check.
```

Then show error reason if available.

If retry is not available to users, do not show retry.

---

# 11. Page 3 — My Reports

Route:

```text
/garage/reports
```

## 11.1 Purpose

This page lists the user’s previous checks.

It should be compact, useful, and easy to scan.

## 11.2 Page Header

Title:

```text
My reports
```

Subtitle:

```text
Review your previous car analyses and open completed reports.
```

Primary action:

```text
Check another car
```

This button links to:

```text
/garage/check
```

## 11.3 Empty State

If no reports:

```text
No reports yet
Your completed car checks will appear here.
```

CTA:

```text
Check your first car
```

## 11.4 Report List Item

Each list item should show:

- title;
- status;
- created date;
- optional Otomoto domain/link indicator;
- optional verdict if available;
- optional risk level if available.

Do not show unexplained numbers.

### Example Completed Item

```text
Ford C-MAX 2005 diesel
Completed · May 27, 2026
Avoid · High risk
```

### Example Processing Item

```text
Toyota Corolla 2022 Hybrid
Processing · Started 2 minutes ago
Analysis in progress
```

### Example Failed Item

```text
Nissan Ariya listing
Failed · May 27, 2026
Could not complete analysis
```

## 11.5 Card Style

```css
background: var(--bg-surface);
border: 1px solid rgba(255,255,255,0.06);
border-radius: 18px;
padding: 20px 22px;
```

Hover:

```css
background: var(--bg-surface-raised);
transform: translateY(-1px);
transition: 150ms ease;
```

## 11.6 Pagination

Use current pagination logic.

Display:

```text
Previous    Page 1 of 3    Next
```

Disabled buttons should be visibly disabled.

---

# 12. Page 4 — Full Report Page

Route:

```text
/garage/reports/:id
```

## 12.1 Purpose

Display one completed report in a full page layout.

This should replace the old large modal approach.

## 12.2 Page Header

Top area should include:

- back link;
- report title;
- status badge;
- created date;
- Otomoto link if available;
- PDF export button if available.

### Back Link

```text
← Back to reports
```

### Title Example

```text
Ford C-MAX 2005 diesel
```

### Metadata Example

```text
Completed · May 27, 2026, 7:42 PM
```

### Otomoto Link

```text
View listing
```

### PDF Button

```text
Export PDF
```

If PDF export is not implemented yet, either hide the button or show disabled with tooltip:

```text
PDF export coming soon
```

Do not show a fake working feature.

## 12.3 Report Content

For now, report content can continue rendering markdown.

However, full page report layout should improve readability:

```css
max-width: 760px;
line-height: 1.7;
```

The report content should not stretch across the whole screen.

## 12.4 Report Page Layout Option

Recommended layout:

```text
Report page container

[Back to reports]

[Report header card]

[Report content card]
```

On larger screens, optional right sidebar may be added later for:

- verdict summary;
- export actions;
- metadata;
- table of contents.

Do not add right sidebar now unless it is simple and useful.

## 12.5 In-Progress Report Page

If user opens a processing report:

```text
Analysis in progress
AutoVerdict is still generating this report.
```

Show status tracker.

## 12.6 Failed Report Page

If failed:

```text
Analysis failed
We couldn’t complete this report.
```

Show reason if available.

---

# 13. PDF Export

## 13.1 MVP Recommendation

PDF export is useful, but it should not block the SaaS restructure.

Implement only if it can be done reliably.

## 13.2 Button Behavior

If available, show on report page only:

```text
Export PDF
```

Do not show PDF export in the report list.

## 13.3 PDF Design

The PDF should be clean and document-like:

- white background;
- black text;
- AutoVerdict logo/name at top;
- report title;
- generated date;
- report content;
- disclaimer at bottom.

Do not export the dark UI directly.

---

# 14. Navigation Behavior

## 14.1 Auth Guard

Authenticated routes:

```text
/garage
/garage/check
/garage/reports
/garage/reports/:id
/admin
```

If unauthenticated user visits an authenticated route, redirect to `/`.

Optionally preserve intended destination after login.

## 14.2 Logged-In Home Page Behavior

If logged-in user visits `/`, do not show Google login as primary action.

Show:

```text
Go to Garage
```

Secondary action may still be:

```text
Sign out
```

## 14.3 After Login

After successful Google login, send user to:

```text
/garage/check
```

This gets them directly to the product action.

---

# 15. Component System

## 15.1 Required Components

The AI agent should create or refactor toward this component structure:

```text
PublicHomePage
PublicHeader
HeroSection
HowItWorksSection
FeatureSection
SafetySection
SeoContentSection
Footer

GarageLayout
Sidebar
MobileGarageNav
CreditsPill
UserMenu

CheckCarPage
AnalysisComposer
MarkdownEditor
OtomotoLinkInput
PhotoUploader
PhotoThumbnailGrid
ProgressTracker
SubmissionResultPanel

ReportsPage
ReportsList
ReportListItem
Pagination
EmptyReportsState

ReportPage
ReportHeader
ReportContent
ReportStatusPanel
PdfExportButton

Shared
Button
Card
Badge
Input
Textarea
StatusBadge
Lightbox
```

## 15.2 Status Badge Labels

Use clear labels:

```text
Pending
Processing
Completed
Failed
```

Never display raw numeric statuses or unexplained values.

## 15.3 Verdict Badges

If verdict is available:

```text
Buy
Buy with caution
Avoid
```

If verdict is not available, do not invent one.

---

# 16. Microcopy

## 16.1 Public Home

Headline:

```text
Avoid expensive used-car mistakes.
```

Subheadline:

```text
Paste listing text, seller messages, an Otomoto link, photos, VIN, or your own questions. AutoVerdict gives you a structured AI risk analysis before you contact the seller.
```

CTA logged out:

```text
Continue with Google
```

CTA logged in:

```text
Go to Garage
```

Trust note:

```text
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

## 16.2 Garage Sidebar

Navigation:

```text
Check car
My reports
```

## 16.3 Check Car Page

Page title:

```text
Check car
```

Subtitle:

```text
Paste everything you know about the car. AutoVerdict will turn it into a structured risk analysis.
```

Composer title:

```text
Tell AutoVerdict what to analyze
```

Composer helper:

```text
Paste listing text, seller messages, VIN, inspection notes, or ask specific questions. Add an Otomoto link or photos if you have them.
```

Submit:

```text
Analyze with AI
```

## 16.4 Reports Page

Title:

```text
My reports
```

Subtitle:

```text
Review your previous car analyses and open completed reports.
```

Primary action:

```text
Check another car
```

Empty state:

```text
No reports yet
Your completed car checks will appear here.
```

## 16.5 Report Page

Back link:

```text
Back to reports
```

PDF:

```text
Export PDF
```

Unavailable PDF tooltip/message:

```text
PDF export coming soon
```

---

# 17. Accessibility Requirements

- All routes must have meaningful page titles.
- Sidebar navigation must indicate active page.
- All buttons must have visible focus states.
- Form inputs must have associated labels.
- Error messages must be placed near relevant fields.
- Status badges must use text, not color alone.
- Progress tracker should use `aria-live="polite"`.
- Mobile drawer must trap focus when open.
- Escape should close drawer and image lightbox.
- Report content should have readable contrast and line height.
- PDF export button should communicate disabled/unavailable state clearly.

---

# 18. Responsive Behavior

## 18.1 Desktop

- Sidebar visible.
- Main content centered within available area.
- Public home uses multi-column hero if enough width.
- Reports list uses full-width cards.
- Report page uses readable content width.

## 18.2 Tablet

- Sidebar may remain if space allows.
- Reduce main padding.
- Home hero can become single column.

## 18.3 Mobile

- Replace sidebar with top bar + drawer or bottom navigation.
- Forms are single column.
- Buttons become full width where appropriate.
- Report content uses full width with comfortable padding.
- Tables inside reports should horizontally scroll.

---

# 19. Implementation Priorities

## Phase 1 — SaaS Structure

1. Create public home page.
2. Create Garage layout with sidebar.
3. Move current input experience to `/garage/check`.
4. Move history list to `/garage/reports`.
5. Create full report route `/garage/reports/:id`.
6. Replace modal report opening with route navigation.
7. Add auth guards.
8. Adjust login redirect to `/garage/check`.

## Phase 2 — UI Polish

1. Apply design tokens.
2. Improve typography.
3. Improve spacing.
4. Polish sidebar.
5. Polish report list cards.
6. Improve progress tracker.
7. Add mobile navigation.

## Phase 3 — Report Experience

1. Improve report readability.
2. Add verdict-first summary when structured data is available.
3. Add missing information block.
4. Add seller questions copy controls.
5. Add PDF export.

## Phase 4 — Monetization

1. Add zero-credit payment prompt.
2. Add Stripe checkout for one check.
3. Add Stripe checkout for five-check package.
4. Add successful payment return state.
5. Add billing/credits history if needed.

---

# 20. Things Not To Do

Do not:

- turn Garage into a complex dashboard;
- add unnecessary overview charts;
- add chat UI;
- add marketplace browsing;
- add unsupported websites as if they can be crawled;
- show PDF export if it does not work;
- create fake verdicts when the backend does not provide them;
- make the home page visually loud;
- overuse gradients;
- use unexplained numbers in the UI;
- bury the primary action under marketing content for logged-in users.

---

# 21. Final Target Experience

The final product should feel like this:

## Public Home

A clear, trustworthy landing page that explains why AutoVerdict exists and helps visitors understand the value before signing in.

## Garage

A calm private workspace where the user can quickly check a car and review previous reports.

## Check Car

A focused page centered around one powerful input: everything the user knows about the car.

## My Reports

A compact archive of previous analyses.

## Report Page

A readable, full-page report that feels serious enough to guide a real purchase decision.

---

# 22. Success Criteria

The redesign is successful if:

1. A new visitor understands the product within 5 seconds.
2. A logged-in user immediately knows where to check a car.
3. The app feels like a real SaaS product, not a local MVP tool.
4. Reports are easy to find and reopen.
5. Navigation feels obvious.
6. The product is ready for SEO and paid traffic.
7. The structure can support payments, PDF export, and admin features later.

The design should communicate:

```text
AutoVerdict is a calm, practical AI assistant that helps buyers avoid bad used-car decisions.
```

