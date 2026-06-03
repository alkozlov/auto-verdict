# Model buying guides (programmatic SEO content engine)

Each guide is an SEO landing page at `/guides/<slug>` targeting buyer-intent searches
("what to check buying a used <model>", "is <model> reliable"). Guides are the content
arm of the growth plan — see `docs/marketing/`.

## How a guide is assembled

A guide is two co-located files that share a `<slug>`:

- `data/<slug>.json` — typed metadata (`GuideMeta` in `types.ts`): make, model, years,
  `<title>`, meta description, H1, intro, FAQ list, and `updated` (YYYY-MM-DD).
- `bodies/<slug>.md` — the markdown body (rendered by `ReportMarkdownViewer`).

`registry.ts` auto-discovers both via `import.meta.glob`, so there is **nothing to register**.

## Add a new guide

1. Create `data/<slug>.json` and `bodies/<slug>.md` (copy an existing pair as a template).
2. Use a keyword-rich, stable slug, e.g. `toyota-rav4-used-buying-guide`.
3. Run `npm run sitemap` (or just `npm run build`, which runs it via `prebuild`) to add it
   to `public/sitemap.xml`.

The route, the `/guides` index card, the JSON-LD (Article + FAQPage + BreadcrumbList), and
the sitemap entry all update automatically.

## Content rules (important)

- **Process-oriented, cautious framing** per PRD §13: "areas to verify", "questions to ask",
  "commonly reported" — never assert a specific car is faulty or safe.
- Genuinely useful and non-thin (Google penalizes scaled thin AI content). Quality over volume,
  especially while the domain has little authority.
- No guarantees, no "detects fraud", no "verifies accident-free".

## Translation (next phase)

Guides are English-first. To go pan-European, add per-locale variants and `hreflang`
(e.g. `/de/guides/<slug>`), translating each `data`/`bodies` pair via the existing pipeline.
This is the deferred multi-language step from Workstream A.
