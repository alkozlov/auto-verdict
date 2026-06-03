export interface GuideFaq {
  q: string;
  a: string;
}

/** Metadata for a model buying guide. Authored as JSON in ./data/<slug>.json. */
export interface GuideMeta {
  /** URL slug, e.g. "volkswagen-golf-used-buying-guide". Must match the file name. */
  slug: string;
  make: string;
  model: string;
  /** Human-readable generation/year span, e.g. "2012–2020 (Mk7)". */
  years: string;
  /** <title> tag. */
  title: string;
  /** Meta description (≤ ~160 chars). */
  description: string;
  /** On-page H1. */
  h1: string;
  /** Short lead paragraph shown under the H1 and in the index card. */
  intro: string;
  faq: GuideFaq[];
  /** ISO date (YYYY-MM-DD) of last content review — used for sitemap lastmod. */
  updated: string;
}

/** A full guide: metadata + the markdown body authored in ./bodies/<slug>.md. */
export interface Guide extends GuideMeta {
  bodyMarkdown: string;
}
