import type { Guide, GuideMeta } from "./types";
import { DEFAULT_LOCALE, isLocale, type Locale } from "@/i18n/languages";

// Auto-discover all guides and their translations. File naming convention:
//   data/<slug>.json          + bodies/<slug>.md          -> English (default)
//   data/<slug>.<locale>.json + bodies/<slug>.<locale>.md -> translated variant
// Drop a new pair and it is picked up here, in routing, the index, and the sitemap.
const metaModules = import.meta.glob<{ default: GuideMeta }>("./data/*.json", {
  eager: true,
});
const bodyModules = import.meta.glob<string>("./bodies/*.md", {
  eager: true,
  query: "?raw",
  import: "default",
});

function parseName(path: string): { slug: string; locale: Locale } {
  const file = path.replace(/^.*\/([^/]+)\.(?:json|md)$/, "$1");
  const segments = file.split(".");
  if (segments.length > 1) {
    const maybeLocale = segments[segments.length - 1];
    if (isLocale(maybeLocale) && maybeLocale !== DEFAULT_LOCALE) {
      return { slug: segments.slice(0, -1).join("."), locale: maybeLocale };
    }
  }
  return { slug: file, locale: DEFAULT_LOCALE };
}

const bodyByKey = new Map<string, string>();
for (const [path, body] of Object.entries(bodyModules)) {
  const { slug, locale } = parseName(path);
  bodyByKey.set(`${locale}:${slug}`, body);
}

const ALL_GUIDES: Guide[] = Object.entries(metaModules).map(([path, mod]) => {
  const { slug, locale } = parseName(path);
  return {
    ...mod.default,
    slug,
    locale,
    bodyMarkdown:
      bodyByKey.get(`${locale}:${slug}`) ??
      bodyByKey.get(`${DEFAULT_LOCALE}:${slug}`) ??
      "",
  };
});

/** Distinct guide slugs, sorted by make/model (uses the English variant for ordering). */
export const GUIDE_SLUGS: string[] = [
  ...new Set(ALL_GUIDES.map((g) => g.slug)),
].sort((a, b) => {
  const ga = ALL_GUIDES.find((g) => g.slug === a && g.locale === DEFAULT_LOCALE) ?? ALL_GUIDES.find((g) => g.slug === a);
  const gb = ALL_GUIDES.find((g) => g.slug === b && g.locale === DEFAULT_LOCALE) ?? ALL_GUIDES.find((g) => g.slug === b);
  return `${ga?.make} ${ga?.model}`.localeCompare(`${gb?.make} ${gb?.model}`);
});

/** Resolve a guide for a locale, falling back to the English variant. */
export function getGuide(slug: string, locale: Locale = DEFAULT_LOCALE): Guide | undefined {
  return (
    ALL_GUIDES.find((g) => g.slug === slug && g.locale === locale) ??
    ALL_GUIDES.find((g) => g.slug === slug && g.locale === DEFAULT_LOCALE)
  );
}

/** Locales that have a real (authored) variant for this slug, English first. */
export function localesForSlug(slug: string): Locale[] {
  const locales = ALL_GUIDES.filter((g) => g.slug === slug).map((g) => g.locale);
  return [...new Set(locales)].sort((a, b) =>
    a === DEFAULT_LOCALE ? -1 : b === DEFAULT_LOCALE ? 1 : a.localeCompare(b)
  );
}

/** All guides for an index page in the given locale (localized variant or English fallback). */
export function guidesForLocale(locale: Locale = DEFAULT_LOCALE): Guide[] {
  return GUIDE_SLUGS.map((slug) => getGuide(slug, locale)!).filter(Boolean);
}

/** Every locale that has at least one authored guide variant (English always included). */
export function supportedGuideLocales(): Locale[] {
  const set = new Set<Locale>([DEFAULT_LOCALE, ...ALL_GUIDES.map((g) => g.locale)]);
  return [...set].sort((a, b) =>
    a === DEFAULT_LOCALE ? -1 : b === DEFAULT_LOCALE ? 1 : a.localeCompare(b)
  );
}
