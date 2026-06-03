import type { Guide, GuideMeta } from "./types";

// Auto-discover all guides. Drop a new ./data/<slug>.json + ./bodies/<slug>.md
// pair and it is picked up here, in routing, the index page, and the sitemap.
const metaModules = import.meta.glob<{ default: GuideMeta }>("./data/*.json", {
  eager: true,
});
const bodyModules = import.meta.glob<string>("./bodies/*.md", {
  eager: true,
  query: "?raw",
  import: "default",
});

function slugFromPath(path: string): string {
  return path.replace(/^.*\/([^/]+)\.(json|md)$/, "$1");
}

const bodyBySlug = new Map<string, string>();
for (const [path, body] of Object.entries(bodyModules)) {
  bodyBySlug.set(slugFromPath(path), body);
}

export const GUIDES: Guide[] = Object.entries(metaModules)
  .map(([path, mod]) => {
    const meta = mod.default;
    const slug = slugFromPath(path);
    return {
      ...meta,
      slug,
      bodyMarkdown: bodyBySlug.get(slug) ?? "",
    };
  })
  .sort((a, b) => `${a.make} ${a.model}`.localeCompare(`${b.make} ${b.model}`));

export function getGuide(slug: string): Guide | undefined {
  return GUIDES.find((guide) => guide.slug === slug);
}
