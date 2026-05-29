# AutoVerdict UI Redesign Guide for AI Implementation Agent

## 0. Document Purpose

This document is a detailed implementation guide for redesigning the public-facing AutoVerdict UI and improving the first-release SaaS presentation, conversion flow, SEO readiness, and SMM readiness.

The goal is not only to make the product visually nicer. The goal is to make the value proposition immediately understandable to a private used-car buyer and to prepare the product for a public MVP launch.

The UI must explain clearly:

- what AutoVerdict does;
- what the user can submit;
- what the user receives;
- why the report is useful before contacting a seller or booking an inspection;
- what the tool can and cannot guarantee;
- how credits and pricing work;
- how the product differs from a generic chatbot;
- why the user can trust the product enough to try the first check.

The output of this task should be a production-ready public UI for the first MVP release.

---

## 1. Product Context

AutoVerdict is a SaaS product for preliminary used-car purchase risk screening.

Users can submit a car listing, seller messages, listing text, VIN, notes, vehicle history text, and images. The system generates a structured AI risk report that helps the user decide whether to continue with the listing, request more information, arrange an inspection, or avoid the car.

The product is not limited to Otomoto or Poland. The public positioning must target private used-car buyers across Europe.

The UI must avoid language that makes the product sound marketplace-specific or country-specific.

Use neutral wording such as:

- used-car listing;
- car marketplace listing;
- seller messages;
- vehicle history data;
- private buyer;
- buyers in Europe;
- European used-car market;
- before contacting the seller;
- before booking an inspection.

Avoid making the product look limited to:

- Otomoto only;
- Poland only;
- one specific marketplace;
- one specific legal jurisdiction.

The product can still mention that marketplace URLs are supported, but do not position the product around a single marketplace.

---

## 2. Current Problems to Fix

The current landing page is too minimal and does not yet look like a production SaaS page.

Main problems:

1. The hero section is too empty and abstract.
2. The CTA label “Go to Garage” is not clear enough for new users.
3. There is no visible sample report or product preview.
4. The page does not explain what the user receives after submitting a listing.
5. Pricing and credits are not visible before registration.
6. Trust and limitations are underexplained.
7. The page does not sufficiently answer the question: “Why should I use this instead of a generic chatbot?”
8. SEO content exists only in a weak, text-heavy form.
9. The visual style feels like an internal developer tool rather than a buyer-facing SaaS product.
10. The page has too much empty dark space and not enough meaningful visual hierarchy.

---

## 3. Redesign Goals

### 3.1 Primary Product Goal

Convert an inexperienced or cautious private used-car buyer into a registered user who submits the first car analysis.

### 3.2 Secondary Product Goal

Make the product understandable in less than 10 seconds.

A new visitor should immediately understand:

- “I paste a used-car listing or seller information.”
- “AutoVerdict finds risks and missing details.”
- “I receive questions, checklist, and a recommendation.”
- “This helps me decide whether the listing is worth my time.”

### 3.3 SEO Goal

Create public pages that can rank for broad European used-car screening intent, not only Polish or Otomoto-specific intent.

Important search intent categories:

- used car listing analysis;
- AI used car check;
- used car risk report;
- questions to ask used car seller;
- used car inspection checklist;
- check used car before buying;
- car listing red flags;
- vehicle history risk analysis;
- car buying second opinion;
- used car buyer checklist Europe.

### 3.4 SMM Goal

Make the landing page and sample report easy to share in social posts, ads, short videos, and creator content.

The UI should support clear screenshots for social media:

- risk report preview;
- seller questions preview;
- inspection checklist preview;
- pricing cards;
- before/after value proposition.

---

## 4. Required Page Set

Create or redesign the following public pages:

```txt
/
Main landing page

/how-it-works
Detailed product explanation

/sample-report
Example AI report for a used-car listing

/pricing
Pricing and credits

/privacy
Privacy policy page

/terms
Terms / legal terms page

/contact
Contact page
```

Do not create these pages:

```txt
/used-car-checklist-poland
/otomoto-listing-analysis
```

Reason: the product is not limited to Poland or Otomoto. It targets used-car buyers across Europe and may support different marketplaces and input sources.

---

## 5. Global Design Direction

### 5.1 Brand Feeling

The visual style should feel:

- professional;
- calm;
- trustworthy;
- analytical;
- practical;
- modern SaaS;
- buyer-friendly;
- not overly automotive/aggressive;
- not “crypto dashboard”;
- not “developer admin panel”.

The product deals with a stressful and financially risky decision. The UI should reduce anxiety and give the user a sense of control.

### 5.2 Design Keywords

Use these as design references:

- structured second opinion;
- buyer safety;
- risk screening;
- calm decision-making;
- clear next steps;
- practical checklist;
- premium but accessible SaaS;
- serious but not cold.

### 5.3 Visual Metaphor

Use the idea of a “structured report” rather than a generic chatbot.

The UI should visually emphasize:

- report cards;
- risk badges;
- checklists;
- evidence/source labels;
- seller questions;
- inspection steps;
- recommendation states.

Avoid relying on generic AI sparkles, robot icons, or vague magic metaphors.

---

## 6. Color Palette

The current dark theme can be kept, but it needs more depth, better contrast, and stronger semantic colors.

### 6.1 Base Colors

Use a dark SaaS palette:

```txt
Background primary:      #070A0F
Background secondary:    #0B1017
Surface primary:         #101722
Surface secondary:       #151E2B
Surface elevated:        #1A2433
Border subtle:           #223044
Border strong:           #334155
Text primary:            #F8FAFC
Text secondary:          #CBD5E1
Text muted:              #94A3B8
Text faint:              #64748B
```

### 6.2 Brand Accent

Use a calm blue accent:

```txt
Brand primary:           #7C9CFF
Brand primary hover:     #9AB3FF
Brand primary active:    #6486F0
Brand soft background:   rgba(124, 156, 255, 0.12)
Brand border:            rgba(124, 156, 255, 0.35)
```

### 6.3 Risk Colors

Use clear but not overly saturated risk colors:

```txt
Risk low:                #22C55E
Risk low background:     rgba(34, 197, 94, 0.12)
Risk low border:         rgba(34, 197, 94, 0.35)

Risk medium:             #F59E0B
Risk medium background:  rgba(245, 158, 11, 0.14)
Risk medium border:      rgba(245, 158, 11, 0.40)

Risk high:               #EF4444
Risk high background:    rgba(239, 68, 68, 0.14)
Risk high border:        rgba(239, 68, 68, 0.40)

Risk unknown:            #94A3B8
Risk unknown background: rgba(148, 163, 184, 0.12)
Risk unknown border:     rgba(148, 163, 184, 0.30)
```

### 6.4 Recommendation Colors

```txt
Proceed:                 green
Proceed with caution:    amber
Request more info:       blue
Avoid:                   red
```

### 6.5 Usage Rules

- Use blue for primary actions and neutral product structure.
- Use amber only for caution/risk emphasis.
- Use red sparingly for serious risk or “avoid”.
- Do not make the entire UI amber/red; the product should feel safe and rational, not alarming.
- Keep enough contrast for accessibility.

---

## 7. Typography

### 7.1 Font

Use a modern sans-serif font. If the project already uses a font, keep it unless it looks poor. Good options:

- Inter;
- Geist Sans;
- Manrope;
- Satoshi, if already available.

Do not introduce a paid font.

### 7.2 Type Scale

Desktop:

```txt
Hero H1:       56–64px / line-height 1.05–1.12 / font-weight 700–800
Hero subtitle: 18–20px / line-height 1.55
Section H2:    36–44px / line-height 1.15
Section lead:  17–19px / line-height 1.6
Card title:    16–18px / font-weight 600–700
Body:          15–17px / line-height 1.55
Small text:    13–14px
Tiny label:    11–12px / uppercase / letter-spacing 0.08em
```

Mobile:

```txt
Hero H1:       38–44px
Hero subtitle: 16–17px
Section H2:    28–34px
Body:          15–16px
```

### 7.3 Text Style Rules

- Avoid long paragraphs in hero and cards.
- Use short, direct statements.
- Use practical buyer language, not abstract SaaS jargon.
- Avoid excessive AI buzzwords.
- Use “AI-assisted” or “structured AI report” rather than “AI magic”.

---

## 8. Layout System

### 8.1 Page Width

Use a consistent max width:

```txt
Main content max width: 1120–1200px
Wide visual sections:   1280px max
```

### 8.2 Spacing

Desktop section spacing:

```txt
Hero top padding:        96–128px after header
Hero bottom padding:     96–128px
Standard section padding: 88–112px vertical
Compact section padding:  56–72px vertical
```

Mobile section spacing:

```txt
Hero padding:            56–72px vertical
Section padding:         56–72px vertical
```

### 8.3 Grid

Use responsive grids:

- Hero: 2 columns on desktop, single column on mobile.
- Cards: 3 columns on desktop, 1 column on mobile, 2 columns on tablet where appropriate.
- SEO content: readable width, around 760–820px.
- Pricing: 2 cards centered.

### 8.4 Background Treatment

Use subtle gradients:

```txt
Hero background:
radial-gradient(circle at 20% 20%, rgba(124,156,255,0.16), transparent 32%),
radial-gradient(circle at 80% 10%, rgba(245,158,11,0.08), transparent 26%),
#070A0F
```

Use section separators with subtle borders:

```txt
border-top: 1px solid rgba(148, 163, 184, 0.10)
```

Do not overuse heavy shadows. Use subtle elevation.

---

## 9. Global Components

### 9.1 Header

The header should be sticky or fixed at the top with a subtle translucent background.

Desktop layout:

```txt
Left: Logo / AutoVerdict
Center or right: navigation links
Right: language selector + primary CTA
```

Header content:

```txt
Logo: AutoVerdict
Nav links:
- How it works
- Sample report
- Pricing

Right:
- Language selector
- Sign in / Go to Garage / Analyze listing
```

CTA logic:

Unauthenticated user:

```txt
Analyze listing
```

Authenticated user:

```txt
Go to Garage
```

If the app currently only supports “Go to Garage”, keep it for authenticated users, but use a clearer public CTA for unauthenticated users.

Header style:

```txt
height: 64–72px
background: rgba(7, 10, 15, 0.78)
backdrop-filter: blur(14px)
border-bottom: 1px solid rgba(148, 163, 184, 0.10)
```

Logo style:

```txt
font-weight: 800
font-size: 18px
color: text primary
```

Navigation style:

```txt
font-size: 14px
color: text muted
hover: text primary
```

Primary CTA:

```txt
background: brand primary
text: #06111F or #070A0F
border-radius: 999px or 12px
padding: 10px 18px
font-weight: 700
```

Mobile:

- Keep logo left.
- Show primary CTA right if space allows.
- Collapse nav into a menu or hide secondary links.
- Language selector may be shown as compact icon.

---

### 9.2 Buttons

Create or standardize button variants.

Primary:

```txt
Label examples:
- Analyze a listing
- Check my first listing
- Start with one check

Style:
blue background
high contrast text
rounded 12px or pill
height 44–48px
font-weight 700
```

Secondary:

```txt
Label examples:
- See sample report
- Learn how it works

Style:
transparent or dark surface
border subtle
text primary
hover surface elevated
```

Ghost:

```txt
For header links, footer links, small actions.
```

Danger:

```txt
Use only inside authenticated app for destructive actions, not public landing.
```

---

### 9.3 Risk Badge

Reusable component.

Props:

```txt
riskLevel: low | medium | high | unknown
label: string
```

Visual examples:

```txt
Low risk
Medium risk
High risk
Unknown risk
Buy with caution
Request more information
Avoid for now
```

Style:

```txt
border-radius: 999px
padding: 6px 10px
font-size: 12px
font-weight: 700
border: semantic border
background: semantic background
color: semantic text
```

---

### 9.4 Report Preview Card

This is one of the most important components in the redesign.

It should visually communicate that AutoVerdict produces a structured report, not a chat response.

Desktop hero version:

```txt
Card title: Example risk report
Badge: Medium risk
Confidence: Medium confidence

Vehicle:
2021 Volvo XC60 · Diesel · 84,000 km

Main concerns:
- Service history is incomplete
- Import history requires clarification
- Seller claim “accident-free” is not supported by documents

Recommended next step:
Ask for VIN, service invoices, accident history, and ownership documents before arranging inspection.
```

Use realistic but generic example. Do not imply a real car or real seller.

Card sections:

1. Header with risk badge.
2. Vehicle summary line.
3. Main concerns list.
4. Recommended next step.
5. Small disclaimer line.

Suggested disclaimer inside card:

```txt
AI-assisted screening, not a professional inspection.
```

---

### 9.5 Input Preview Card

A small visual block showing what the user can paste/upload.

Content:

```txt
You can submit:
- Listing URL
- Copied listing text
- Seller replies
- VIN or registration details
- Vehicle history notes
- Photos or screenshots
```

Style:

- dark surface card;
- small input-like chips;
- optional paperclip/link/image icons.

---

### 9.6 Checklist Component

Used on landing and sample report pages.

Visual style:

```txt
[ ] Ask for VIN
[ ] Request service invoices
[ ] Confirm accident history
[ ] Check ownership documents
[ ] Verify mileage consistency
```

For public pages, checkboxes are visual only.

---

### 9.7 Source Label Component

For sample report.

Labels:

```txt
listing
user data
vehicle history
model knowledge
AI inference
```

Style:

```txt
small uppercase label
muted border
background surface secondary
```

Purpose: communicate that the report distinguishes facts from assumptions.

---

## 10. Page: `/` Main Landing

### 10.1 Page Goal

The landing page must explain the product quickly and convert users into first-check submissions.

Primary CTA:

```txt
Analyze a listing
```

Secondary CTA:

```txt
See sample report
```

### 10.2 SEO Metadata

Title:

```txt
AutoVerdict — AI Used-Car Listing Analysis Before You Buy
```

Description:

```txt
Paste a used-car listing, seller messages, VIN, notes, or photos. AutoVerdict gives you a structured AI risk report with red flags, missing information, seller questions, inspection points, and a clear next step.
```

Open Graph title:

```txt
Check a used-car listing before you contact the seller
```

Open Graph description:

```txt
AutoVerdict helps private buyers screen used-car listings, spot risks, and prepare seller questions before inspection.
```

### 10.3 Landing Page Section Order

```txt
1. Header
2. Hero
3. Trust strip / value proof
4. What you can submit
5. What AutoVerdict checks
6. Sample report preview
7. How it works
8. Why not just use a generic chatbot
9. Pricing preview
10. Safety / disclaimer block
11. SEO content block
12. FAQ
13. Final CTA
14. Footer
```

---

### 10.4 Hero Section

Layout:

- Desktop: two columns.
- Left: headline, subtitle, CTA, trust line.
- Right: report preview card.
- Mobile: text first, report preview below.

Hero H1:

```txt
Check a used-car listing before you contact the seller.
```

Hero subtitle:

```txt
Paste a marketplace listing, seller messages, VIN, vehicle notes, or photos. AutoVerdict gives you a structured AI risk report with red flags, missing information, seller questions, inspection points, and a clear next step.
```

Primary CTA:

```txt
Analyze a listing
```

Secondary CTA:

```txt
See sample report
```

Trust line under CTAs:

```txt
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

Optional microcopy:

```txt
Built for private buyers comparing used cars across Europe.
```

Hero right report preview content:

```txt
Example risk report

Medium risk
Medium confidence

2021 Volvo XC60 · 84,000 km · Diesel

Main concerns
• Service history is incomplete
• Import history needs clarification
• Accident-free claim is not supported by documents

Recommended next step
Ask for VIN, service invoices, accident history, and ownership documents before arranging inspection.
```

Do not use an actual seller name or real VIN.

---

### 10.5 Trust Strip / Value Proof

Immediately below hero, add a compact strip with 3–4 items.

Content:

```txt
Structured risk report
Seller questions included
Inspection checklist included
Built for private buyers
```

Alternative:

```txt
No car expertise required
Works with text, links, notes, and photos
Clear recommendation, not a chat transcript
Credits used only for completed checks
```

Visual:

- horizontal row on desktop;
- stacked grid on mobile;
- small icons optional;
- subtle border top/bottom.

---

### 10.6 Section: What You Can Submit

Purpose: show flexibility and avoid marketplace-specific positioning.

Section title:

```txt
Use whatever information you already have.
```

Section lead:

```txt
AutoVerdict can work with a full listing or with partial information. Add more details when you have them — the report will clearly show what is known and what still needs verification.
```

Cards:

Card 1:

```txt
Marketplace listing
Paste a used-car listing URL or copied ad text from the marketplace you are browsing.
```

Card 2:

```txt
Seller messages
Add replies from the seller, claims about service history, accident history, ownership, or import status.
```

Card 3:

```txt
Vehicle details
Add VIN, registration information, mileage, price, first registration date, or vehicle history notes.
```

Card 4:

```txt
Photos and screenshots
Upload listing screenshots, interior photos, exterior photos, dashboard photos, or document screenshots.
```

Implementation note:

- If the current backend supports only certain URL domains, do not overpromise automatic crawling for all marketplaces.
- Phrase carefully: “Paste a listing URL or copied listing text.”
- Do not say “we automatically crawl every marketplace in Europe” unless implemented.

---

### 10.7 Section: What AutoVerdict Checks

Section title:

```txt
What AutoVerdict checks before you spend time on a car.
```

Section lead:

```txt
The report focuses on practical buying risks: missing documents, unclear claims, suspicious wording, ownership signals, model-specific areas to verify, and questions to ask before inspection.
```

Use a 2-column or 3-column card grid.

Card content:

1. Missing information

```txt
Highlights missing VIN, service records, ownership details, accident history, import status, or unclear seller claims.
```

2. Listing and seller risks

```txt
Flags vague wording, unsupported claims, inconsistent details, short ownership signals, and deal risks that should be clarified.
```

3. Model-specific concerns

```txt
Lists known areas to verify for the specific model, engine, transmission, and year when enough data is available.
```

4. Cost and ownership signals

```txt
Helps you think about first-year costs, likely verification steps, and whether the listing is worth further attention.
```

5. Seller questions

```txt
Generates practical questions to ask before calling, travelling, or booking an inspection.
```

6. Inspection checklist

```txt
Turns the analysis into a focused checklist for the physical inspection or mechanic visit.
```

---

### 10.8 Section: Sample Report Preview

This section is critical.

Section title:

```txt
See the kind of report you get.
```

Section lead:

```txt
AutoVerdict does not give a vague chat answer. It creates a structured buyer report with risk level, confidence, missing details, seller questions, inspection points, and a recommendation.
```

Display a larger mock report component.

Report preview structure:

```txt
Risk level: Medium
Confidence: Medium
Recommendation: Request more information

Summary
The listing may be worth further review, but several important details should be clarified before arranging an inspection.

Positive signals
• Recent model year
• Mileage appears plausible for the age
• Seller provides several photos

Risk signals
[Medium] Service history incomplete
[Medium] Import status unclear
[Low] Listing does not mention tire/brake condition

Missing information
• VIN
• Service invoices
• Accident history confirmation
• Ownership duration

Questions for the seller
1. Can you send the VIN before viewing?
2. Do you have service invoices from the last 24 months?
3. Has the car had any paint or body repairs?
4. Why are you selling the car now?

Recommended next step
Request documents first. Arrange inspection only if the seller provides VIN, service history, and clear ownership information.
```

CTA below:

```txt
View full sample report
```

Link to:

```txt
/sample-report
```

---

### 10.9 Section: How It Works

Section title:

```txt
How it works
```

Three cards:

Step 1:

```txt
01
Paste what you know

Add a listing URL, copied ad text, seller messages, VIN, vehicle history notes, or photos.
```

Step 2:

```txt
02
AI reviews the risks

AutoVerdict checks missing facts, suspicious wording, deal risks, model-specific concerns, and unclear claims.
```

Step 3:

```txt
03
Get practical next steps

Receive seller questions, inspection points, estimated cost signals, and a clear recommendation.
```

Optional CTA:

```txt
Learn more about the process
```

Link to:

```txt
/how-it-works
```

---

### 10.10 Section: Why Not Just Use a Generic Chatbot

Section title:

```txt
Why not just paste the listing into a generic chatbot?
```

Section body:

```txt
Generic chatbots can summarize text, but AutoVerdict is built around the used-car buying workflow. It structures the input, separates known facts from assumptions, highlights missing information, prepares seller questions, creates an inspection checklist, and keeps your reports in one place so you can compare listings.
```

Comparison cards or table:

```txt
Generic chatbot
- Free-form answer
- No saved car history
- No credit/report workflow
- No consistent report structure
- Easy to miss seller questions

AutoVerdict
- Structured risk report
- Saved checks in your garage
- Risk level and confidence
- Seller questions and inspection checklist
- Buyer-focused recommendation
```

Keep tone respectful. Do not attack ChatGPT or other tools.

---

### 10.11 Section: Pricing Preview

Section title:

```txt
Simple pricing for careful buyers.
```

Section lead:

```txt
Use credits when you want to check a listing. One completed analysis uses one credit.
```

Pricing cards:

Card 1:

```txt
1 check
20 PLN

Best for checking one car before contacting the seller.

Includes:
• 1 structured AI risk report
• Seller questions
• Inspection checklist
• Recommendation

CTA: Start with 1 check
```

Card 2:

```txt
3 checks
40 PLN

Best for comparing several cars before choosing which ones are worth inspection.

Includes:
• 3 structured AI risk reports
• Compare multiple listings
• Better value per check
• Saved reports in your garage

CTA: Get 3 checks
Badge: Better value
```

Important note below pricing:

```txt
Credits are used only when a check is successfully processed. If a technical error prevents report generation, the credit is not consumed or can be restored according to the product rules.
```

If payment integration is not fully production-ready at the time of UI implementation, show pricing but route CTA to authentication/garage where the current flow handles payment readiness.

---

### 10.12 Section: Safety / Disclaimer

Section title:

```txt
A screening tool, not a guarantee.
```

Body:

```txt
AutoVerdict helps you identify questions, risks, and missing information before you invest time or money in a used car. It does not replace a professional inspection, official vehicle history report, legal verification, mechanic diagnostics, or independent expert evaluation.
```

Bullets:

```txt
Use AutoVerdict to prepare better questions.
Use inspection and official documents to verify the car.
Avoid making final decisions from AI output alone.
```

This section should feel honest and trustworthy, not scary.

---

### 10.13 SEO Content Block

This should be readable, not keyword stuffing.

Title:

```txt
AI used-car listing analysis for private buyers in Europe
```

Text:

```txt
Buying a used car often means comparing listings from different marketplaces, reading seller claims, checking service history, and deciding whether a car is worth a call, a trip, or an inspection. AutoVerdict helps private buyers screen used-car listings before they commit more time.

You can paste a listing description, seller replies, VIN, vehicle history notes, or photos. The system highlights possible red flags, missing details, unclear ownership or import information, model-specific areas to verify, and practical questions to ask the seller.

The result is a structured AI-assisted report designed for cautious decision-making. It helps you prepare for the next step, but it does not replace professional inspection or official verification.
```

Subsections:

```txt
Why analyze a listing before contacting the seller?
A quick screening can help you avoid wasting time on listings with missing documents, vague claims, or risks that should be clarified before a viewing.

What information should you check before buying a used car?
Important signals include VIN, service history, accident records, mileage consistency, ownership duration, import status, seller reputation, and model-specific known issues.

How AutoVerdict helps with marketplace listings
AutoVerdict turns listing text, seller messages, vehicle notes, and photos into a structured risk report with questions, checklist items, and a recommendation.
```

---

### 10.14 FAQ Section

Add FAQ accordion with schema.org FAQPage structured data if possible.

FAQ items:

1. Is AutoVerdict a professional car inspection?

```txt
No. AutoVerdict is an AI-assisted preliminary screening tool. It helps you identify risks, missing information, and questions before inspection. It does not replace a mechanic, diagnostic check, legal verification, or official vehicle history report.
```

2. What can I submit for analysis?

```txt
You can submit listing text, a marketplace URL, seller messages, VIN, registration details, vehicle history notes, your own notes, and photos or screenshots.
```

3. Does AutoVerdict work only with one marketplace?

```txt
No. AutoVerdict is designed around used-car listing analysis, not one specific marketplace. If automatic extraction is unavailable for a listing, you can paste the listing text and upload screenshots.
```

4. Can AutoVerdict tell me if a car is safe to buy?

```txt
No tool can guarantee that from a listing alone. AutoVerdict gives a risk-based recommendation and practical next steps, but the final decision should rely on inspection, documents, and independent verification.
```

5. What does one credit include?

```txt
One credit allows you to generate one completed car analysis report.
```

6. What happens if the analysis fails?

```txt
Credits should only be consumed for successfully processed checks. If a technical error prevents report generation, the credit is not consumed or can be restored according to the product rules.
```

7. Is my submitted data sent to an AI provider?

```txt
Yes. The information you submit may be sent to the configured AI provider to generate the report. Do not submit unnecessary personal or sensitive information.
```

---

### 10.15 Final CTA Section

Title:

```txt
Found a car you like? Check it before you call.
```

Body:

```txt
Paste the listing, seller messages, VIN, notes, or photos and get a structured risk report before deciding whether the car is worth your time.
```

CTA:

```txt
Analyze a listing
```

Secondary:

```txt
View sample report
```

---

### 10.16 Footer

Footer content:

```txt
AutoVerdict
AI-assisted used-car listing analysis for private buyers.

Links:
- How it works
- Sample report
- Pricing
- Privacy
- Terms
- Contact
```

Small disclaimer:

```txt
AutoVerdict does not replace professional inspection, official vehicle history reports, legal verification, or mechanic diagnostics.
```

---

## 11. Page: `/how-it-works`

### 11.1 Page Goal

Explain the product workflow in more detail for cautious buyers who need trust before trying the product.

### 11.2 SEO Metadata

Title:

```txt
How AutoVerdict Works — AI Used-Car Risk Screening
```

Description:

```txt
Learn how AutoVerdict turns used-car listings, seller messages, VIN, notes, and photos into a structured AI risk report with missing information, seller questions, inspection points, and a recommendation.
```

### 11.3 Page Structure

```txt
1. Header
2. Hero explanation
3. Step-by-step workflow
4. What the AI looks for
5. What the report contains
6. What AutoVerdict cannot verify
7. Best practices for better results
8. CTA
9. Footer
```

### 11.4 Hero

H1:

```txt
How AutoVerdict turns listing data into a buyer-ready risk report.
```

Lead:

```txt
AutoVerdict is designed for private buyers who want a structured second opinion before contacting a seller, travelling to view a car, or paying for inspection.
```

CTA:

```txt
Analyze a listing
```

Secondary CTA:

```txt
See sample report
```

---

### 11.5 Step-by-Step Workflow

Use vertical timeline or numbered cards.

Step 1:

```txt
Submit the information you have

Paste a listing URL, copied listing text, seller replies, VIN, registration details, vehicle history notes, or upload photos and screenshots.
```

Step 2:

```txt
AutoVerdict normalizes the input

The system organizes the data into facts, claims, notes, images, and missing fields so the analysis can be structured.
```

Step 3:

```txt
The AI reviews risks and gaps

The analysis looks for missing information, vague wording, unsupported seller claims, ownership or import uncertainty, accident-history ambiguity, and model-specific areas to verify.
```

Step 4:

```txt
You receive a structured report

The report includes a risk level, confidence level, extracted facts, positive signals, risk signals, missing information, seller questions, inspection checklist, and recommendation.
```

Step 5:

```txt
You decide the next step

Use the report to decide whether to request documents, ask more questions, arrange inspection, compare another car, or avoid the listing.
```

---

### 11.6 What the AI Looks For

Grid of cards:

```txt
Missing documents
VIN, service invoices, ownership proof, accident history, import documents.

Inconsistent claims
Claims that do not match the available listing information or provided history notes.

Suspicious wording
Vague phrases, unsupported “perfect condition” claims, missing details for expensive cars.

Model-specific checks
Known areas to verify for the model, engine, transmission, year, and mileage.

Deal risk
Unclear seller type, short ownership, pressure to buy quickly, missing payment or document clarity.

Inspection priorities
What to focus on if you decide to inspect the vehicle.
```

---

### 11.7 What the Report Contains

Use a report-like visual.

Sections:

```txt
Risk level and confidence
Vehicle facts
Positive signals
Risk signals with severity
Missing information
Questions for the seller
Inspection checklist
Model-specific risks
Final recommendation
Disclaimer
```

---

### 11.8 What AutoVerdict Cannot Verify

This is important for trust and legal clarity.

Title:

```txt
What AutoVerdict cannot guarantee
```

Content:

```txt
AutoVerdict cannot physically inspect the car, confirm hidden damage, verify legal ownership, guarantee mileage accuracy, or replace official vehicle history databases. It helps you prepare better questions and identify areas that need verification.
```

Bullets:

```txt
It cannot guarantee that a car is safe.
It cannot prove that a seller is honest or dishonest.
It cannot replace a mechanic or diagnostic station.
It cannot replace official vehicle history or legal checks.
It should not be the only basis for a purchase decision.
```

---

### 11.9 Best Practices for Better Results

Title:

```txt
How to get a better report
```

Bullets:

```txt
Paste the full listing text, not only the title.
Add seller replies if you already contacted the seller.
Include VIN or registration data when available.
Upload photos of the exterior, interior, dashboard, service book, or listing screenshots.
Add your own concerns, for example: “price seems low” or “seller says imported from Germany”.
```

---

## 12. Page: `/sample-report`

### 12.1 Page Goal

Show users exactly what they get. This page is critical for conversion, SEO, ads, and social sharing.

### 12.2 SEO Metadata

Title:

```txt
Sample AutoVerdict Report — AI Used-Car Listing Risk Analysis
```

Description:

```txt
View an example AutoVerdict used-car risk report with risk level, confidence, vehicle facts, risk signals, missing information, seller questions, inspection checklist, and recommendation.
```

### 12.3 Page Structure

```txt
1. Header
2. Sample report intro
3. Mock submitted input
4. Full sample report
5. Explanation of report sections
6. CTA
7. Footer
```

### 12.4 Intro

H1:

```txt
Sample used-car risk report
```

Lead:

```txt
This example shows the type of structured report AutoVerdict creates from listing text, seller information, and vehicle details. The example is fictional and for demonstration only.
```

CTA:

```txt
Analyze your own listing
```

---

### 12.5 Mock Submitted Input

Show a small input card before the report.

Title:

```txt
Example input
```

Content:

```txt
Marketplace listing for a 2021 Volvo XC60, 84,000 km, diesel automatic. Seller claims the car is accident-free and well maintained, but the listing does not include VIN, service invoices, ownership duration, or import details.
```

Add tags:

```txt
Listing text
Seller claims
Missing VIN
Photos available
```

---

### 12.6 Full Sample Report Content

Use a polished report UI, not a plain markdown wall.

Report header:

```txt
AutoVerdict Report
2021 Volvo XC60 · Diesel · Automatic · 84,000 km

Risk level: Medium
Confidence: Medium
Recommendation: Request more information before inspection
```

Section 1: Summary

```txt
The listing may be worth further review, but the available information is incomplete. The seller claims the car is accident-free and well maintained, but the listing does not provide enough supporting evidence. Before arranging an inspection, the buyer should request VIN, service invoices, ownership details, and clarification about import or accident history.
```

Section 2: Extracted Vehicle Facts

Table:

```txt
Make: Volvo
Model: XC60
Year: 2021
Mileage: 84,000 km
Fuel: Diesel
Transmission: Automatic
VIN provided: No
Seller type: Not clear from provided input
Service history: Claimed, not documented in input
Accident-free claim: Claimed, not supported by documents in input
```

Section 3: Positive Signals

```txt
• Recent model year
• Mileage appears plausible for the age
• Seller provided several photos
• No visible severe damage mentioned in the provided input
```

Section 4: Risk Signals

Use cards with severity badges and source labels.

Risk 1:

```txt
Severity: Medium
Title: Service history is claimed but not documented
Source: listing / seller claim
Explanation: The seller says the car is well maintained, but the provided input does not include service invoices, service book photos, or workshop records. This should be verified before inspection.
```

Risk 2:

```txt
Severity: Medium
Title: VIN is missing
Source: missing information
Explanation: For a relatively recent and expensive used car, the VIN should normally be available before viewing. Without VIN, the buyer cannot easily check history or prepare verification questions.
```

Risk 3:

```txt
Severity: Medium
Title: Accident-free claim needs evidence
Source: seller claim
Explanation: The accident-free claim is useful only if supported by documents, history data, paint measurements, or inspection. It should not be treated as confirmed from the listing alone.
```

Risk 4:

```txt
Severity: Low
Title: Ownership duration is unclear
Source: missing information
Explanation: The listing does not explain how long the seller has owned the vehicle. Short ownership is not automatically bad, but it should be clarified.
```

Section 5: Missing Information

```txt
• VIN
• Service invoices or service book records
• Ownership duration
• Accident or repair history confirmation
• Import status and country of previous registration
• Tire and brake condition
• Date of last technical inspection
```

Section 6: Questions for the Seller

```txt
1. Can you send the VIN before I arrange a viewing?
2. Do you have service invoices or digital service records?
3. Has the car had any paint, body, or accident repairs?
4. How long have you owned the vehicle?
5. Was the car imported? If yes, from which country and when?
6. Are there any current warning lights, leaks, or known mechanical issues?
7. Can the car be inspected at an independent workshop before purchase?
```

Section 7: Inspection Checklist

```txt
[ ] Verify VIN on the car and documents
[ ] Check service invoices and mileage consistency
[ ] Measure paint thickness on all panels
[ ] Check suspension, brakes, tires, and underbody condition
[ ] Scan for diagnostic error codes
[ ] Test automatic transmission behavior during cold and warm driving
[ ] Check for water leaks, unusual engine noise, and dashboard warnings
[ ] Compare seller claims with documents and inspection findings
```

Section 8: Model-Specific Areas to Verify

Use cautious wording.

```txt
Component: Diesel engine and emissions system
Risk: Diesel vehicles can have expensive emissions-system components depending on mileage, usage pattern, and maintenance.
How to check: Ask about DPF/EGR/AdBlue-related repairs, warning lights, and diagnostic history.

Component: Automatic transmission
Risk: Smooth shifting and service history should be verified, especially on higher-mileage vehicles.
How to check: Test cold start, low-speed shifting, kickdown, and service records.

Component: Suspension and brakes
Risk: Heavier SUVs may have higher wear on suspension, tires, and brakes.
How to check: Inspect on a lift and check invoices for recent replacement.
```

Section 9: Recommendation

```txt
Decision: Request more information

Do not arrange a long trip or paid inspection until the seller provides VIN, service history, accident-history clarification, and ownership details. If the seller provides documents and agrees to independent inspection, the listing may still be worth checking. If the seller avoids basic questions or refuses VIN, consider another car.
```

Section 10: Disclaimer

```txt
This report is an AI-assisted preliminary screening based only on the information provided. It does not replace professional inspection, official vehicle history reports, legal verification, mechanic diagnostics, or independent expert evaluation.
```

---

### 12.7 Explanation of Report Sections

After the sample report, add concise explanations:

```txt
Risk level
A quick overall estimate based on the available information.

Confidence
How much the report can rely on the submitted data.

Risk signals
Potential problems or unclear areas, each with severity and explanation.

Missing information
Data you should request before investing more time.

Seller questions
Practical questions to send before viewing the car.

Inspection checklist
Items to verify if the car reaches the inspection stage.
```

---

## 13. Page: `/pricing`

### 13.1 Page Goal

Make pricing clear before registration and reduce payment anxiety.

### 13.2 SEO Metadata

Title:

```txt
AutoVerdict Pricing — Used-Car AI Risk Reports
```

Description:

```txt
Simple credit-based pricing for AutoVerdict used-car listing analysis. Buy one check or a discounted package to compare multiple cars before inspection.
```

### 13.3 Page Structure

```txt
1. Header
2. Pricing hero
3. Pricing cards
4. What is included
5. How credits work
6. Refund / technical failure note
7. FAQ
8. CTA
9. Footer
```

### 13.4 Pricing Hero

H1:

```txt
Simple pricing for used-car checks.
```

Lead:

```txt
Use credits to generate structured AI risk reports before contacting sellers, booking inspections, or travelling to view a car.
```

---

### 13.5 Pricing Cards

Card 1:

```txt
1 check
20 PLN

For one car you want to verify before taking the next step.

Includes:
• 1 structured AI risk report
• Risk level and confidence
• Missing information
• Seller questions
• Inspection checklist
• Final recommendation

CTA: Start with 1 check
```

Card 2:

```txt
3 checks
40 PLN
Better value

For comparing several cars and choosing which ones are worth inspection.

Includes:
• 3 structured AI risk reports
• Saved reports in your garage
• Better value per check
• Useful for shortlisting cars

CTA: Get 3 checks
```

Visual:

- Make 3-check package slightly highlighted.
- Do not make the 1-check card look bad.
- Use transparent pricing, no hidden subscription.

---

### 13.6 What Is Included

Title:

```txt
Every check includes
```

Grid:

```txt
Risk level and confidence
Structured vehicle facts
Positive and risk signals
Missing information
Questions for the seller
Inspection checklist
Model-specific areas to verify
Final recommendation
```

---

### 13.7 How Credits Work

Content:

```txt
One completed report uses one credit. Credits are stored in your account and can be used when you submit a car for analysis. If you buy a package, the credits are added to your balance after successful payment confirmation.
```

Add note:

```txt
Credits are not a subscription. There is no monthly plan in the MVP.
```

---

### 13.8 Failure and Credit Safety

Content:

```txt
Credits should only be consumed when a check is successfully processed and a report is provided. If a technical error prevents report generation, the credit is not consumed or can be restored according to the product rules.
```

---

## 14. Page: `/contact`

### 14.1 Page Goal

Provide a simple way to contact the product owner and support basic trust.

### 14.2 SEO Metadata

Title:

```txt
Contact AutoVerdict
```

Description:

```txt
Contact AutoVerdict for questions about used-car listing analysis, reports, payments, or product support.
```

### 14.3 Page Content

H1:

```txt
Contact AutoVerdict
```

Lead:

```txt
Have a question about a report, payment, or the product? Send us a message.
```

Contact options:

```txt
Email: [support email]
Response time: usually within 1–2 business days
```

If no support email exists yet, add a TODO placeholder in code and use current configured contact value if available.

Suggested form fields if a form already exists or can be implemented quickly:

```txt
Name
Email
Message
Submit
```

If no backend contact form exists, do not invent complex infrastructure. Use a mailto link or static email display.

Add support categories:

```txt
Report question
Payment or credits
Technical issue
General feedback
```

---

## 15. Page: `/privacy`

### 15.1 Goal

Provide a clear privacy page before launch.

The content should be legally reviewed later. For now, implement a practical MVP privacy page using cautious wording.

### 15.2 SEO Metadata

Title:

```txt
Privacy Policy — AutoVerdict
```

Description:

```txt
Learn how AutoVerdict handles account data, submitted vehicle information, uploaded images, AI processing, payments, and user reports.
```

### 15.3 Required Content Sections

```txt
Privacy Policy
Last updated: [date]

1. Who we are
2. What data we collect
3. How we use your data
4. AI processing notice
5. Uploaded files and vehicle information
6. Payments
7. Authentication
8. Data retention
9. Data deletion requests
10. Security
11. Contact
```

Important content:

```txt
Information submitted for analysis may be sent to the configured AI provider to generate the report.
```

```txt
Users should avoid submitting unnecessary personal, sensitive, or unrelated information.
```

```txt
Uploaded images and reports are not public by default.
```

Do not claim compliance certifications unless implemented.

---

## 16. Page: `/terms`

### 16.1 Goal

Explain product limitations, payment/credit rules, and user responsibility.

The content should be legally reviewed later.

### 16.2 SEO Metadata

Title:

```txt
Terms of Use — AutoVerdict
```

Description:

```txt
Read the AutoVerdict terms of use, including product limitations, AI report disclaimer, credit rules, and user responsibilities.
```

### 16.3 Required Content Sections

```txt
Terms of Use
Last updated: [date]

1. Product description
2. AI-assisted screening disclaimer
3. No guarantee of vehicle condition
4. User responsibility
5. Credits and payments
6. Refund and technical failure handling
7. Acceptable use
8. Account access
9. Limitation of liability
10. Contact
```

Critical disclaimer:

```txt
AutoVerdict provides AI-assisted preliminary screening only. It does not replace professional vehicle inspection, official vehicle history reports, mechanic diagnostics, legal verification, or independent expert evaluation.
```

Credit terms should match product rules:

```txt
One completed analysis uses one credit. Credits are used only for report generation and cannot be exchanged for cash unless required by applicable law or payment-provider rules.
```

Use cautious wording and mark as MVP legal text if needed.

---

## 17. Authenticated App / Garage Improvements

This document mainly focuses on public pages, but the public CTA leads into the authenticated app. The first screen after sign-in must feel consistent with the landing page.

### 17.1 Header in Authenticated App

Authenticated header should show:

```txt
AutoVerdict
Credits: [number]
Top up balance
User email/avatar
Sign out
```

The payment document says users can see their balance and should have a “Top up balance” button next to it. Implement or visually prepare this behavior if not already present.

### 17.2 Main CTA in App

The main form title should be buyer-focused:

```txt
Analyze a used-car listing
```

Subtitle:

```txt
Paste listing text, seller messages, VIN, vehicle history notes, or upload photos. The more useful details you add, the more specific the report can be.
```

### 17.3 Input Form Microcopy

Description placeholder:

```txt
Paste the listing text here — ad copy, seller messages, specs, VIN, inspection notes, vehicle history data, or anything relevant.
```

Listing URL helper:

```txt
Add a marketplace listing URL if available. If automatic extraction is not supported, paste the listing text as well.
```

Images helper:

```txt
Upload up to 5 listing screenshots or vehicle photos. Accepted formats: JPEG, PNG, WEBP.
```

Submit button:

```txt
Analyze listing
```

Submitting state:

```txt
Submitting…
```

Processing state after submit:

```txt
Analysis queued. Your report will appear in your garage when processing is complete.
```

### 17.4 Empty State

If user has no previous reports:

```txt
No analyses yet

Start by pasting a used-car listing, seller message, or vehicle notes. Your completed reports will appear here.

CTA: Analyze your first listing
```

### 17.5 Report History

History card should show:

```txt
Title
Created date
Status badge
Risk level if available
Recommendation if available
```

For completed reports, show quick summary:

```txt
Medium risk · Request more information
```

For processing:

```txt
Processing · usually takes a few minutes
```

For failed:

```txt
Failed · credit was not consumed/restored if technical failure applies
```

Do not overpromise exact processing time if not measured.

---

## 18. SEO Implementation Requirements

### 18.1 Global SEO

Each public page must have:

- unique title;
- unique meta description;
- canonical URL;
- Open Graph title;
- Open Graph description;
- Open Graph image if available;
- Twitter/X card metadata;
- proper H1 exactly once;
- semantic H2/H3 structure.

### 18.2 Recommended OG Image

Create a reusable OG image design if the project supports it.

Text:

```txt
AutoVerdict
Check a used-car listing before you contact the seller
AI-assisted risk report · Seller questions · Inspection checklist
```

Visual:

- dark background;
- report preview card;
- medium risk badge;
- brand accent.

### 18.3 Structured Data

Add JSON-LD where practical:

Homepage:

```txt
SoftwareApplication or WebApplication
```

Pricing page:

```txt
Product / Offer if appropriate
```

FAQ sections:

```txt
FAQPage
```

Do not add fake ratings, reviews, or aggregateRating unless real.

### 18.4 Internal Linking

Header links:

```txt
/ -> /how-it-works
/ -> /sample-report
/ -> /pricing
```

Landing page contextual links:

```txt
Sample report preview -> /sample-report
How it works section -> /how-it-works
Pricing preview -> /pricing
FAQ payment question -> /pricing
```

Footer links to all public pages.

### 18.5 Content Rules

Avoid keyword stuffing.

Use natural recurring phrases:

- used-car listing analysis;
- AI risk report;
- seller questions;
- inspection checklist;
- missing information;
- vehicle history;
- private used-car buyer;
- before contacting the seller;
- before booking inspection;
- used-car red flags.

---

## 19. SMM Readiness Requirements

### 19.1 Shareable Sections

The following sections should look good in screenshots:

- Hero with report preview;
- Sample report risk card;
- Pricing cards;
- Seller questions card;
- Inspection checklist card;
- “Why not generic chatbot” comparison.

### 19.2 Social Copy Support

The UI should visually support content hooks like:

```txt
Before you drive 200 km to view a car, ask these questions first.
```

```txt
A seller says “accident-free”. What evidence should you request?
```

```txt
This is what a structured AI used-car report looks like.
```

```txt
Don’t buy the story. Verify the listing.
```

Do not put all these slogans on the page, but design components so they can be used in SMM screenshots.

---

## 20. Accessibility Requirements

- Text contrast must meet WCAG AA where possible.
- Buttons must have visible focus states.
- Interactive elements must be keyboard accessible.
- Accordions must be accessible.
- Images must have meaningful alt text.
- Do not rely only on color for risk meaning; include text labels such as “Medium risk”.
- Use semantic HTML: header, nav, main, section, footer.
- Use one H1 per page.

---

## 21. Mobile Requirements

The product must look good on mobile because many buyers browse car listings from phones.

Mobile rules:

- Hero CTA must be visible without excessive scrolling.
- Report preview must not overflow horizontally.
- Pricing cards stack vertically.
- Header should not take too much height.
- FAQ accordions should be easy to tap.
- Long report sample tables should become stacked key-value blocks.
- Avoid tiny text in cards.

Critical mobile first viewport:

```txt
User should see H1, short value proposition, and primary CTA in the first screen.
```

---

## 22. Implementation Notes

### 22.1 Tech Stack Assumption

Use the existing frontend stack:

```txt
Next.js
TypeScript
Tailwind CSS
shadcn/ui if already installed
```

Do not introduce a heavy UI framework.

### 22.2 Component Organization Suggestion

Suggested structure:

```txt
components/public/Header.tsx
components/public/Footer.tsx
components/public/HeroSection.tsx
components/public/ReportPreviewCard.tsx
components/public/InputSourcesSection.tsx
components/public/WhatWeCheckSection.tsx
components/public/HowItWorksSection.tsx
components/public/PricingCards.tsx
components/public/FaqSection.tsx
components/public/FinalCtaSection.tsx
components/public/RiskBadge.tsx
components/public/SourceLabel.tsx
components/public/SampleReport.tsx
```

Page routes:

```txt
app/page.tsx
app/how-it-works/page.tsx
app/sample-report/page.tsx
app/pricing/page.tsx
app/privacy/page.tsx
app/terms/page.tsx
app/contact/page.tsx
```

Adjust paths to the actual project structure.

### 22.3 Content Constants

Consider placing reusable copy into constants:

```txt
lib/public-copy.ts
```

This makes it easier to adjust marketing copy later.

### 22.4 Avoid Breaking Authenticated Flow

Do not break existing authentication, credit, or report history flows.

Public CTA behavior should integrate with current auth flow:

- If unauthenticated: route to sign-in or start auth flow.
- If authenticated: route to Garage / app screen.

Use existing route conventions where possible.

### 22.5 Language Selector

Current UI shows a language selector. If multi-language content is not implemented, do not fake full translated pages.

Acceptable MVP behavior:

- keep language selector if it currently works;
- otherwise keep it visually but disabled only if product owner approves;
- preferably avoid showing unavailable languages.

---

## 23. Acceptance Criteria

The redesign is complete when:

1. The landing page clearly explains the product within 10 seconds.
2. The hero has a strong CTA and visible report preview.
3. The page no longer looks like an internal developer MVP screen.
4. The user can understand what input is accepted.
5. The user can understand what the report contains.
6. The user can see pricing before registration.
7. The product limitations are clearly stated.
8. There is a full sample report page.
9. There is a detailed how-it-works page.
10. There is a pricing page.
11. Privacy, terms, and contact pages exist.
12. The product is not positioned as Poland-only or Otomoto-only.
13. All public pages have proper SEO metadata.
14. FAQ schema is implemented where practical.
15. The page works well on mobile.
16. The design uses consistent colors, typography, spacing, cards, and badges.
17. Existing authenticated functionality remains intact.

---

## 24. Copy Summary for Quick Implementation

### Main headline

```txt
Check a used-car listing before you contact the seller.
```

### Main subtitle

```txt
Paste a marketplace listing, seller messages, VIN, vehicle notes, or photos. AutoVerdict gives you a structured AI risk report with red flags, missing information, seller questions, inspection points, and a clear next step.
```

### Primary CTA

```txt
Analyze a listing
```

### Secondary CTA

```txt
See sample report
```

### Trust line

```txt
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

### Pricing

```txt
1 check — 20 PLN
3 checks — 40 PLN
```

### Final CTA

```txt
Found a car you like? Check it before you call.
```

---

## 25. Important Do / Do Not Rules

### Do

- Position AutoVerdict as a Europe-oriented used-car listing risk screening tool.
- Use practical buyer language.
- Show concrete report examples.
- Highlight seller questions and inspection checklist.
- Show pricing clearly.
- Include disclaimers visibly.
- Use structured cards, badges, and checklists.
- Keep the design calm and trustworthy.

### Do Not

- Do not position the product as Otomoto-only.
- Do not position the product as Poland-only.
- Do not claim the AI can guarantee vehicle condition.
- Do not claim the seller is dishonest or fraudulent.
- Do not say the car is definitely safe.
- Do not replace legal/inspection language with absolute claims.
- Do not create fake reviews, ratings, or testimonials.
- Do not overpromise automatic crawling for unsupported marketplaces.
- Do not hide pricing until after registration.
- Do not make the page look like a generic AI chatbot wrapper.

---

## 26. Suggested First Implementation Order

Implement in this order:

```txt
1. Create shared public layout, header, footer, button, badge, card components.
2. Redesign `/` landing page hero and report preview.
3. Add landing sections: input sources, checks, sample preview, how it works, pricing, FAQ, final CTA.
4. Create `/sample-report` page.
5. Create `/pricing` page.
6. Create `/how-it-works` page.
7. Create `/privacy`, `/terms`, `/contact` pages.
8. Add SEO metadata and structured data.
9. Test responsive behavior.
10. Verify authenticated app routes and CTA behavior.
```

---

## 27. Final Product Direction

AutoVerdict should feel like a practical decision-support tool for real buyers, not an experimental AI demo.

The user should leave the landing page thinking:

```txt
I can paste a listing here and quickly understand what to ask, what to verify, and whether the car is worth my time.
```

That is the core design goal.
