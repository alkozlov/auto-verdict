# AutoVerdict Landing Page Polishing Instructions

## Purpose

This document describes the next polishing iteration for the AutoVerdict public landing page.

The current redesign is a strong improvement over the previous MVP page. The goal of this iteration is not to rebuild the page from scratch, but to improve conversion clarity, perceived product quality, spacing, SEO usefulness, and the user's understanding of the free first analysis and credit model.

Apply these changes to the public landing page only. Do not redesign the authenticated dashboard / analyses area in this task.

---

## 1. Make “First Analysis Is Free” a Visible Conversion Hook

### Problem

New users receive one free analysis, but the current landing page does not communicate this clearly. As a result, a new visitor may think they must pay before seeing the product value.

For this product, the free first analysis is a major conversion hook. It should reduce friction and make the first user action feel safe.

### Required Behavior

Show the free first analysis offer in three places:

1. Hero section, directly under the primary CTA area.
2. Pricing section, before or above the paid plans.
3. Near the final CTA section, to reinforce the action before the user leaves the page.

### Hero Section Update

Current hero CTA area has buttons similar to:

- Analyze a listing
- See sample report

Add a short trust/conversion line directly below the buttons.

Use this exact copy:

```text
First analysis is free. No card required.
```

Below it, keep or add the existing disclaimer line:

```text
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

Recommended visual treatment:

- The “First analysis is free” line should be more visible than the disclaimer.
- Use a small check icon or subtle accent dot.
- Text color should be brighter than normal muted text, but not as strong as the main heading.
- Example styling:
  - `font-size: 14px`
  - `font-weight: 600`
  - color: light blue / accent text
  - margin-top: 18–20px after CTA buttons
- Disclaimer should be below it:
  - `font-size: 13px`
  - muted color
  - margin-top: 6–8px

Example layout:

```text
[Analyze a listing] [See sample report]

✓ First analysis is free. No card required.
AI-assisted preliminary screening. Not a replacement for professional inspection.
```

### Pricing Section Update

Add a small banner above the pricing cards.

Section copy:

```text
New users get 1 free analysis after sign-in.
After that, buy credits only when you need more checks.
```

Visual treatment:

- Place it between the pricing intro text and the pricing cards.
- Use a horizontal banner/card with a subtle accent border.
- It should be noticeable but not more dominant than the pricing cards.
- Suggested content structure:

```text
First analysis included
New users get 1 free completed analysis. No card required.
```

Optional small label:

```text
Free starter credit
```

Recommended styling:

- Background: slightly lighter than page background, similar to other cards.
- Border: 1px solid accent blue with low opacity.
- Border radius: same as pricing cards.
- Padding: 18–24px.
- Layout desktop: horizontal, label/title on left, short text on right or below.
- Layout mobile: stacked.

### Pricing Cards Copy Update

Inside each paid pricing card, add or adjust text so it is clear paid credits are for additional checks after the free one.

For the 1-check card:

```text
For one extra car you want to verify before taking the next step.
```

For the 3-check card:

```text
For comparing several cars after your free first analysis.
```

### Final CTA Section Update

Current CTA resembles:

```text
Found a car you like? Check it before you call.
```

Add a short line below the CTA description or above the CTA buttons:

```text
Start with 1 free analysis. No card required.
```

The primary button in this section should also reflect this:

```text
Start free analysis
```

Secondary button:

```text
See sample report
```

---

## 2. Rename “Go to Garage” to a Clearer Product Action

### Problem

“Garage” is a nice internal metaphor, but for a new visitor it is not clear enough. The landing page should use action-oriented language.

### Required Change

Replace the public header CTA text:

```text
Go to Garage
```

with:

```text
Go to analyses
```

However, use different text depending on auth state if this is already easy in the current codebase.

### Preferred Behavior

For unauthenticated users:

```text
Start free analysis
```

For authenticated users:

```text
Go to analyses
```

### If Auth State Is Not Easy to Determine

Use this as a single universal label:

```text
Go to analyses
```

This is clearer than “Go to Garage” and better aligned with the actual product workflow.

### Navigation Naming

Avoid using “Garage” on the public landing page for now. It can still exist internally later if needed, but the first-release public language should be literal and clear.

Preferred public terms:

- analysis
- analyses
- report
- risk report
- check
- listing check

Avoid for public-first experience:

- garage
- workspace
- cockpit
- lab
- studio

---

## 3. Make the Design Less Flat and More Premium

### Problem

The redesign is much better, but the page still feels a little flat because many sections use similar dark cards with similar borders and spacing. The page needs more visual hierarchy and a more polished SaaS feel without becoming flashy.

### Goal

Keep the dark, serious, trustworthy style, but introduce more depth, stronger hierarchy, and 2–3 intentional visual moments.

Do not radically change the design system. Improve it.

---

### 3.1 Add a Subtle Hero Background Accent

Add a very subtle background glow or gradient behind the hero area.

Recommended approach:

- Add a soft radial gradient behind the hero content, aligned around the report preview card or between the hero text and preview.
- Keep it subtle. It should not look like a gaming/productivity app glow.
- Suggested colors:
  - accent blue with very low opacity;
  - maybe a tiny amount of amber near the risk badge, but blue is safer.
- The page should still feel serious and trustworthy.

Example CSS direction:

```css
.hero-section {
  position: relative;
  overflow: hidden;
}

.hero-section::before {
  content: "";
  position: absolute;
  width: 680px;
  height: 680px;
  right: 8%;
  top: 8%;
  background: radial-gradient(circle, rgba(117, 146, 255, 0.13), transparent 62%);
  pointer-events: none;
  filter: blur(4px);
}
```

Adjust values to fit the existing codebase.

Important:

- The glow must not reduce text readability.
- It must not make the page feel colorful or playful.
- On mobile, reduce or disable the glow if it causes visual noise.

---

### 3.2 Improve Card Hierarchy

Currently many cards look equal. Introduce card hierarchy:

#### Primary Cards

Used for:

- hero report preview;
- pricing cards;
- final CTA card;
- maybe the “What your report includes” section.

Style:

- Slightly stronger border.
- Slightly lighter background than normal cards.
- More padding.
- Optional very subtle shadow/glow.
- More noticeable heading.

#### Secondary Cards

Used for:

- input types;
- FAQ items;
- smaller benefit/check cards.

Style:

- Existing card style mostly okay.
- Lower visual weight.

### Suggested Design Tokens

Use or approximate these values:

```css
--bg-page: #05080d;
--bg-section: #070b12;
--card-bg: #0d1420;
--card-bg-strong: #101827;
--card-border: rgba(148, 163, 184, 0.14);
--card-border-strong: rgba(129, 156, 255, 0.32);
--text-primary: #f8fafc;
--text-secondary: #b7c6d9;
--text-muted: #7f8fa3;
--accent-blue: #7c9cff;
--accent-blue-strong: #86a3ff;
--accent-green: #57d9a3;
--accent-amber: #f5c84c;
--accent-red: #ff6b6b;
```

Do not overuse bright colors. Use accent colors only for calls to action, status/risk indicators, small icons, and important conversion hooks.

---

### 3.3 Vary Section Layouts

Avoid making every section a simple grid of identical cards.

Use these layout variations:

1. Hero: two-column text + report preview.
2. Input section: four compact cards.
3. “What your report includes”: feature list + report workflow card or grouped cards.
4. “Why not generic chatbot?”: comparison layout.
5. Pricing: two strong pricing cards plus free-analysis banner.
6. FAQ: two-column cards.
7. Final CTA: centered highlighted card.

This makes the page feel more intentionally designed.

---

### 3.4 Add Small Visual Details to Key Product Concepts

Add small icons or visual markers consistently:

- Link icon for marketplace listing.
- Message icon for seller messages.
- Document/VIN icon for vehicle details.
- Image icon for photos.
- Shield/check icon for risk checks.
- Question icon for seller questions.
- Clipboard/checklist icon for inspection checklist.
- Credit/card icon for pricing.

Icons should be thin-line, simple, and consistent.

Do not use large colorful illustrations.

---

### 3.5 Improve the Report Preview Card

The report preview card is one of the most important elements. Make it feel like a real product output.

Keep the current compact preview, but polish it:

- Add a small header line:
  ```text
  Example risk report
  ```
- Keep the risk badge:
  ```text
  Medium risk
  ```
- Add confidence:
  ```text
  Confidence: Medium
  ```
- Add a small “Recommendation” row at the bottom:
  ```text
  Recommendation: Request more information before inspection
  ```
- Use subtle dividers between sections.
- Make the list bullets and severity/risk indicators visually aligned.

Suggested card structure:

```text
Example risk report                         Medium risk

2021 Volvo XC60 · 84,000 km · Diesel
Confidence: Medium

Main concerns
• Service history is incomplete
• Import history needs clarification
• Accident-free claim is not supported by documents

Recommended next step
Ask for VIN, service invoices, accident history, and ownership documents before arranging inspection.

AI-assisted screening, not a professional inspection.
```

Do not make it too large. This is a preview, not the full report.

---

## 4. Reduce Excessive Vertical Spacing

### Problem

Some sections have too much vertical whitespace, especially on desktop. The page feels long and a bit empty. Dark backgrounds make this more noticeable.

### Goal

Make the landing page feel tighter and more deliberate while preserving a premium SaaS feel.

### Required Changes

Review all public landing sections and reduce excessive vertical padding by approximately 15–25%.

Suggested desktop spacing:

```css
.hero {
  padding-top: 96px;
  padding-bottom: 88px;
}

.section {
  padding-top: 80px;
  padding-bottom: 80px;
}

.section-compact {
  padding-top: 56px;
  padding-bottom: 56px;
}

.section-large {
  padding-top: 96px;
  padding-bottom: 96px;
}
```

Suggested mobile spacing:

```css
.hero {
  padding-top: 56px;
  padding-bottom: 56px;
}

.section {
  padding-top: 48px;
  padding-bottom: 48px;
}

.section-compact {
  padding-top: 36px;
  padding-bottom: 36px;
}
```

### Specific Spacing Guidelines

1. After hero:
   - The next section should appear sooner.
   - The user should not feel like the first screen ends with a large empty gap.

2. Between card grids and next section:
   - Reduce large empty bands.
   - Keep enough space for visual breathing room.

3. Pricing section:
   - Keep enough space because pricing is important.
   - But avoid huge blank area before the disclaimer section.

4. Final CTA:
   - Keep it visually separated, but not isolated by excessive empty space.

### Container Width

Keep the current max-width approximately:

```css
max-width: 1120px or 1180px;
```

Do not stretch text lines too wide. Main paragraphs should usually be max 700–760px.

### Heading-to-Body Spacing

Use consistent spacing:

```css
section eyebrow → heading: 10–12px
heading → paragraph: 14–18px
paragraph → cards: 32–40px
cards grid gap: 16–24px
```

---

## 5. Add a Stronger “What You Get in the Report” Section

### Problem

The page explains what AutoVerdict checks, but it should more clearly sell the output. Users need to know exactly what they receive after spending a credit or using the free first analysis.

### Required New Section

Add a new section titled:

```text
What you get in the report
```

Recommended placement:

Place this section after “What AutoVerdict checks before you spend time on a car” and before “See what the report looks like” / “How it works”.

If the page becomes too long, it can replace or partially merge with the current “See what the report looks like” section, but do not remove the report preview entirely.

### Section Intro Copy

Use this exact or very close copy:

```text
AutoVerdict turns messy listing information into a structured buyer report you can use before calling the seller, travelling to view the car, or booking an inspection.
```

### Content Items

Include these eight report components:

1. Risk level and confidence
2. Extracted vehicle facts
3. Missing information
4. Risk signals
5. Seller questions
6. Inspection checklist
7. Model-specific things to verify
8. Clear recommendation

### Recommended Layout

Use a two-column layout on desktop:

Left side:

- Section heading.
- Intro paragraph.
- Small CTA link or button:
  ```text
  See sample report
  ```

Right side:

- A structured feature list grouped into two cards or one large card.

Option A — one strong card:

```text
Your report includes

✓ Risk level and confidence
Understand whether the listing looks low, medium, high, or unknown risk.

✓ Extracted vehicle facts
See the key facts AutoVerdict found in the listing or user-provided data.

✓ Missing information
Know what is not provided but should be clarified before inspection.

✓ Risk signals
Review suspicious wording, unsupported claims, unclear ownership, import or accident-history ambiguity.

✓ Seller questions
Get practical questions you can send before calling or travelling.

✓ Inspection checklist
Use concrete inspection points if the car reaches viewing or mechanic stage.

✓ Model-specific things to verify
See common areas to check for the specific model, engine, year, or configuration when available.

✓ Clear recommendation
Proceed, proceed with caution, request more information, or avoid.
```

Option B — 2x4 cards:

- Better if the page needs visual rhythm.
- Each card should be compact.
- Avoid overly large cards that create more vertical height.

Preferred: Option A if the page already has many cards. It will reduce repetitive grid feeling.

### Suggested Visual Treatment

Use a stronger card than normal feature cards:

- Background: `card-bg-strong`
- Border: accent blue with low opacity
- Small check icons in accent green or blue
- Section title inside card:
  ```text
  Buyer-ready report
  ```
- Optional small badge:
  ```text
  Structured output
  ```

### Important Content Rule

Do not claim that AutoVerdict proves the car is safe.

Use cautious wording:

- “risk signals”
- “things to verify”
- “questions to ask”
- “recommendation”
- “before inspection”

Avoid:

- “detects fraud”
- “guarantees safety”
- “verifies accident-free status”
- “proves mileage is real”

---

## 6. Clarify Pricing: Credits Are Not a Subscription

### Problem

Users may worry this is a subscription. For a used-car buyer, subscription pricing is unattractive because they may only need the product for a short buying period.

### Required Changes

Update the pricing section to make this very clear.

### Pricing Section Heading

Current:

```text
Simple pricing before registration.
```

This is acceptable, but improve it to:

```text
Simple pricing. No subscription.
```

### Pricing Section Description

Replace or adjust the paragraph under the heading with:

```text
Buy checks only when you need them. One completed report uses one credit. Credits are not a subscription and there is no monthly fee.
```

### Free Analysis Banner

Add this above pricing cards:

```text
New users get 1 free analysis after sign-in. No card required.
```

### Pricing Card Details

Each paid card should mention:

```text
One completed report uses one credit.
```

For the 3-check package, mention:

```text
Better value for comparing multiple cars.
```

### Payment/Credit Safety Note

Add a small muted note below the pricing cards:

```text
Credits are used for completed reports. If a technical error prevents report generation, the credit should not be consumed or should be restored.
```

If this exact behavior is not fully implemented yet, use softer wording:

```text
Credits are intended for completed reports. Technical failures should not cost you an additional check.
```

Use the softer version if the implementation is still being finalized.

### Avoid

Do not use subscription-like language:

- plan
- monthly
- recurring
- membership
- upgrade

Prefer:

- credits
- checks
- package
- one completed report
- buy when needed

---

## 7. Expand FAQ for SEO and Trust

### Problem

The FAQ is useful but too short for SEO and for handling buyer objections. Add more questions that match real buyer concerns and search intent.

### Required FAQ Items

Keep the existing FAQ items, but add the following new ones.

---

### FAQ 1

Question:

```text
Can I use AutoVerdict if I only have photos or seller messages?
```

Answer:

```text
Yes. You can submit partial information such as screenshots, seller replies, VIN details, vehicle notes, or copied listing text. The report will show what is known and what still needs verification.
```

---

### FAQ 2

Question:

```text
What should I check before travelling to view a used car?
```

Answer:

```text
Before travelling, you should clarify the VIN, ownership history, service records, accident history, import status, mileage consistency, and whether the seller can provide documents. AutoVerdict helps turn these points into practical seller questions and inspection items.
```

---

### FAQ 3

Question:

```text
Can AutoVerdict tell me if a car is safe to buy?
```

Answer:

```text
No. AutoVerdict is a preliminary screening tool. It can highlight risks, missing information, and questions to ask, but it cannot guarantee that a car is safe. Always verify documents and use professional inspection before purchase.
```

---

### FAQ 4

Question:

```text
Does AutoVerdict work only with one marketplace?
```

Answer:

```text
No. You can paste listing text, seller messages, VIN details, notes, or screenshots from different marketplaces. Automatic URL extraction may depend on marketplace support, but manual input can still be analyzed.
```

This can replace or refine the existing marketplace FAQ.

---

### FAQ 5

Question:

```text
Is AutoVerdict useful if I am not a car expert?
```

Answer:

```text
Yes. AutoVerdict is designed for private buyers who want a structured second opinion before contacting the seller, travelling to view the car, or booking an inspection.
```

---

### FAQ 6

Question:

```text
Is this better than asking a generic AI chatbot?
```

Answer:

```text
AutoVerdict is built around the used-car buying workflow. It organizes the result into risk level, missing information, seller questions, inspection checklist, model-specific things to verify, and a clear recommendation.
```

This can replace or refine the existing chatbot FAQ.

---

### FAQ Layout

Use 2-column FAQ cards on desktop and 1-column on mobile.

FAQ cards should be readable and not too tall.

Recommended styling:

- Question: strong white text.
- Answer: muted text with comfortable line height.
- Optional question icon in accent blue.
- Keep card height flexible.

### SEO Notes

FAQ content should be present in the initial rendered DOM for the SPA route.

If JSON-LD FAQ schema is already implemented or easy to add, add/update it to match the visible FAQ items. Do not add FAQ schema for questions that are not visible on the page.

---

## 8. Implementation Scope and Non-Goals

### In Scope

- Public landing page copy and layout changes.
- Hero conversion hook.
- Header CTA rename.
- Visual polish and hierarchy improvements.
- Reduced vertical spacing.
- New “What you get in the report” section.
- Pricing clarity.
- FAQ expansion.
- Responsive checks for these changes.

### Out of Scope

- Footer redesign.
- Full sample report page implementation.
- Embedding the full real report into the landing page.
- Auth/dashboard redesign.
- Backend changes.
- Payment integration changes.
- Full SSR/SSG migration.

---

## 9. Acceptance Checklist

The task is complete when all items below are true:

- [ ] Hero clearly says the first analysis is free and no card is required.
- [ ] Public header CTA no longer says “Go to Garage”.
- [ ] Unauthenticated header CTA says “Start free analysis” if auth state is available.
- [ ] Authenticated header CTA says “Go to analyses” if auth state is available.
- [ ] If auth state is not available, the CTA says “Go to analyses”.
- [ ] Page has a new or clearly visible “What you get in the report” section.
- [ ] Pricing section clearly says there is no subscription and no monthly fee.
- [ ] Pricing section explains that one completed report uses one credit.
- [ ] Pricing section mentions the free first analysis.
- [ ] FAQ includes the additional buyer-focused SEO questions.
- [ ] Vertical spacing is reduced and the page feels less empty on desktop.
- [ ] Cards have better hierarchy and the design feels less flat.
- [ ] Hero report preview looks like a polished product output.
- [ ] Mobile layout remains readable with no horizontal overflow.
- [ ] No copy claims that AutoVerdict guarantees safety or replaces inspection.
