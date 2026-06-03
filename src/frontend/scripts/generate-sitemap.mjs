// Generates public/sitemap.xml from the static public routes plus every guide
// (and its translations) in src/content/guides/data, including hreflang
// alternates. Runs automatically before `vite build` (prebuild).
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ORIGIN = "https://autoverdict.app";
const DEFAULT_LOCALE = "en";
const LOCALES = new Set(["en", "pl", "de", "uk", "fr"]);

const here = dirname(fileURLToPath(import.meta.url));
const guidesDir = join(here, "..", "src", "content", "guides", "data");
const outFile = join(here, "..", "public", "sitemap.xml");
const today = new Date().toISOString().slice(0, 10);

const guidePath = (slug, locale) =>
  locale === DEFAULT_LOCALE ? `/guides/${slug}` : `/${locale}/guides/${slug}`;
const indexPath = (locale) =>
  locale === DEFAULT_LOCALE ? "/guides" : `/${locale}/guides`;

function parseName(file) {
  const name = file.replace(/\.json$/, "");
  const segments = name.split(".");
  const last = segments[segments.length - 1];
  if (segments.length > 1 && LOCALES.has(last) && last !== DEFAULT_LOCALE) {
    return { slug: segments.slice(0, -1).join("."), locale: last };
  }
  return { slug: name, locale: DEFAULT_LOCALE };
}

// Group guide files by slug: { [slug]: { [locale]: { updated } } }
const guides = {};
for (const file of readdirSync(guidesDir).filter((f) => f.endsWith(".json"))) {
  const { slug, locale } = parseName(file);
  const meta = JSON.parse(readFileSync(join(guidesDir, file), "utf8"));
  (guides[slug] ??= {})[locale] = { updated: meta.updated || today };
}

const sortLocales = (locales) =>
  [...locales].sort((a, b) =>
    a === DEFAULT_LOCALE ? -1 : b === DEFAULT_LOCALE ? 1 : a.localeCompare(b)
  );

const guideLocaleSet = sortLocales(
  new Set([DEFAULT_LOCALE, ...Object.values(guides).flatMap((v) => Object.keys(v))])
);

function alternatesXml(pathFor, locales) {
  const links = locales.map(
    (loc) => `    <xhtml:link rel="alternate" hreflang="${loc}" href="${ORIGIN}${pathFor(loc)}"/>`
  );
  if (locales.includes(DEFAULT_LOCALE)) {
    links.push(
      `    <xhtml:link rel="alternate" hreflang="x-default" href="${ORIGIN}${pathFor(DEFAULT_LOCALE)}"/>`
    );
  }
  return links.join("\n");
}

function urlBlock({ loc, changefreq, priority, lastmod, alternates }) {
  const parts = [`    <loc>${ORIGIN}${loc}</loc>`];
  if (lastmod) parts.push(`    <lastmod>${lastmod}</lastmod>`);
  parts.push(`    <changefreq>${changefreq}</changefreq>`);
  parts.push(`    <priority>${priority}</priority>`);
  if (alternates) parts.push(alternates);
  return `  <url>\n${parts.join("\n")}\n  </url>`;
}

const blocks = [];

// Static pages (single language).
for (const [loc, changefreq, priority] of [
  ["/", "weekly", "1.0"],
  ["/how-it-works", "monthly", "0.8"],
  ["/sample-report", "monthly", "0.8"],
  ["/pricing", "monthly", "0.8"],
  ["/privacy", "yearly", "0.4"],
  ["/terms", "yearly", "0.4"],
  ["/contact", "yearly", "0.5"],
]) {
  blocks.push(urlBlock({ loc, changefreq, priority }));
}

// Guides index, per supported locale, cross-linked with hreflang.
for (const locale of guideLocaleSet) {
  blocks.push(
    urlBlock({
      loc: indexPath(locale),
      changefreq: "weekly",
      priority: "0.9",
      alternates: alternatesXml((loc) => indexPath(loc), guideLocaleSet),
    })
  );
}

// Each guide, one URL per available locale, cross-linked with hreflang.
let guideUrlCount = 0;
for (const slug of Object.keys(guides).sort()) {
  const locales = sortLocales(Object.keys(guides[slug]));
  for (const locale of locales) {
    guideUrlCount += 1;
    blocks.push(
      urlBlock({
        loc: guidePath(slug, locale),
        changefreq: "monthly",
        priority: "0.7",
        lastmod: guides[slug][locale].updated,
        alternates: alternatesXml((loc) => guidePath(slug, loc), locales),
      })
    );
  }
}

const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">
${blocks.join("\n")}
</urlset>
`;

writeFileSync(outFile, xml, "utf8");
console.log(
  `sitemap.xml written: ${blocks.length} URLs (${Object.keys(guides).length} guides, ${guideUrlCount} guide URLs, locales: ${guideLocaleSet.join(", ")})`
);
