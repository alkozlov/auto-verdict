// Generates public/sitemap.xml from the static public routes plus every guide
// in src/content/guides/data. Runs automatically before `vite build` (prebuild).
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ORIGIN = "https://autoverdict.app";
const here = dirname(fileURLToPath(import.meta.url));
const guidesDir = join(here, "..", "src", "content", "guides", "data");
const outFile = join(here, "..", "public", "sitemap.xml");

const today = new Date().toISOString().slice(0, 10);

/** @type {{loc: string, changefreq: string, priority: string, lastmod?: string}[]} */
const urls = [
  { loc: "/", changefreq: "weekly", priority: "1.0" },
  { loc: "/how-it-works", changefreq: "monthly", priority: "0.8" },
  { loc: "/sample-report", changefreq: "monthly", priority: "0.8" },
  { loc: "/pricing", changefreq: "monthly", priority: "0.8" },
  { loc: "/guides", changefreq: "weekly", priority: "0.9" },
  { loc: "/privacy", changefreq: "yearly", priority: "0.4" },
  { loc: "/terms", changefreq: "yearly", priority: "0.4" },
  { loc: "/contact", changefreq: "yearly", priority: "0.5" },
];

const guideFiles = readdirSync(guidesDir).filter((f) => f.endsWith(".json"));
for (const file of guideFiles) {
  const meta = JSON.parse(readFileSync(join(guidesDir, file), "utf8"));
  const slug = file.replace(/\.json$/, "");
  urls.push({
    loc: `/guides/${slug}`,
    changefreq: "monthly",
    priority: "0.7",
    lastmod: typeof meta.updated === "string" ? meta.updated : today,
  });
}

const body = urls
  .map((u) => {
    const lastmod = u.lastmod ? `<lastmod>${u.lastmod}</lastmod>` : "";
    return `  <url><loc>${ORIGIN}${u.loc}</loc>${lastmod}<changefreq>${u.changefreq}</changefreq><priority>${u.priority}</priority></url>`;
  })
  .join("\n");

const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${body}
</urlset>
`;

writeFileSync(outFile, xml, "utf8");
console.log(`sitemap.xml written: ${urls.length} URLs (${guideFiles.length} guides)`);
