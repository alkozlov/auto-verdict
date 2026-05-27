# AutoVerdict — UI/UX Refinement Instructions (Phase 1)

## Objective

Improve the existing UI without changing product behavior.

**Do NOT redesign flows.**

**Do NOT modify backend interactions.**

**Do NOT change report structure.**

Focus only on:

- visual hierarchy
- spacing
- component polish
- readability
- perceived quality
- micro-interactions

The app should feel:

- ultra minimal SaaS
- calm
- trustworthy
- premium
- dark-mode first
- automotive-neutral
- not flashy

---

# 1. Global Layout Improvements

## Main container

Current layout feels slightly too stretched.

Apply:

```css
max-width: 960px;
margin: 0 auto;
padding-left: 32px;
padding-right: 32px;
```

Tablet:

```css
padding:24px;
```

Mobile:

```css
padding:16px;
```

---

## Vertical spacing

Increase spacing between:

- intro text
- composer card
- history section
- report section

Target:

```css
gap:32px;
```

Avoid crowded sections.

---

# 2. Header Improvements

Current header is acceptable but visually weak.

## Height

```css
height:64px;
```

## Border

```css
border-bottom:
1px solid rgba(255,255,255,.06)
```

## Reduce visual noise

### Email

```css
opacity:.7;
font-size:14px;
```

### Sign out

```css
opacity:.7;
```

### Credits pill

Make it smaller and more premium.

```css
padding:8px 14px;
border-radius:999px;
font-size:13px;
font-weight:600;
```

---

# 3. Composer Card Improvements

This is the most important area.

---

## Increase card quality

```css
background:#111419;

border:
1px solid rgba(255,255,255,.06);

border-radius:24px;

padding:28px;
```

---

## Title spacing

Increase spacing.

Current:

```text
title
helper
editor
```

Target:

```text
title

helper


editor
```

---

## Reduce editor dominance

Current editor feels too massive.

Change:

```css
min-height:220px;
```

---

## Improve placeholder

Use:

```text
Paste listing text, seller messages,
VIN, concerns, inspection notes,
or ask AutoVerdict specific questions.

Example:

"I'm considering this Toyota Corolla from Otomoto.
What should I verify before contacting the seller?"
```

Large placeholders improve empty states dramatically.

---

# 4. Toolbar Improvements

Current toolbar is visually noisy.

Reduce toolbar prominence.

```css
opacity:.72;
```

Icons:

```css
font-size:14px;
```

Toolbar background:

```css
transparent;
```

Toolbar border:

```css
border:
1px solid rgba(255,255,255,.04);
```

Goal:

**Toolbar should visually disappear.**

---

# 5. Buttons Improvements

## Secondary buttons

Buttons:

- Add photos
- Add Otomoto link

Use:

```css
border:
1px dashed rgba(255,255,255,.12);

background:
rgba(255,255,255,.01);
```

Hover:

```css
background:
rgba(255,255,255,.03);
```

---

## Primary Button

Current button is too bright.

Use:

```css
background:#6E8FEA;

color:#111;

font-weight:600;
```

Hover:

```css
background:#7C9AF0;
```

Height:

```css
height:56px;
```

Radius:

```css
border-radius:16px;
```

Goal:

**Feel expensive. Not aggressive.**

---

# 6. Recent Analyses Section

Current cards need stronger hierarchy.

---

## Card Improvements

```css
padding:24px;

margin-bottom:16px;

background:#111419;

border:
1px solid rgba(255,255,255,.06);
```

Hover:

```css
transform:
translateY(-1px);

background:
#171B22;
```

Transition:

```css
transition:150ms ease;
```

---

## Improve Metadata Hierarchy

Current:

```text
title
date
2
```

Replace with:

```text
TITLE

Completed • May 27
```

Status should be semantic.

Remove unexplained numbers.

---

# 7. Typography Refinement

Use:

```css
font-family:
Inter,
system-ui,
sans-serif;
```

Page intro:

```css
font-size:20px;
font-weight:500;
```

Card titles:

```css
font-size:15px;
font-weight:650;
```

Body:

```css
font-size:15px;
line-height:1.6;
```

Reduce oversized typography.

---

# 8. Animation Rules

Keep animation subtle.

---

## Hover

```css
transition:150ms;
```

---

## Report Open

```css
opacity
translateY(4px)
```

---

## Button Hover

```css
filter:brightness(1.03);
```

---

Avoid:

- bounce
- scale effects
- dramatic motion
- spring animations everywhere

---

# 9. Color Tokens

Use globally.

```css
--bg-page:#0B0D10;
--bg-surface:#111419;
--bg-raised:#171B22;

--border:#252B35;

--text:#F4F6F8;
--text-secondary:#B6C0CC;

--brand:#6EA8FF;

--success:#4ADE80;
--warning:#FBBF24;
--danger:#F87171;
```

Do NOT introduce additional random colors.

---

# 10. Things NOT To Change

Do NOT:

- redesign flow
- add more pages
- replace multiline input
- move history elsewhere
- redesign reports
- create dashboard layouts
- introduce sidebar navigation
- add AI chat interface

The product must remain:

```text
single page

input

analysis

history
```

---

# Final Goal

The product should feel like:

> A calm, premium AI decision assistant for used car buyers.

Not:

> A developer tool with AI attached.

Success means users immediately understand:

1. What to do
2. Why the tool is valuable
3. What happens next
4. Why they should trust the result

The UI should communicate:

**quiet confidence**
