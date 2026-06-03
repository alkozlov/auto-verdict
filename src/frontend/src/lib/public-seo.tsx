import { useEffect } from "react";
import { useTranslation } from "react-i18next";

type JsonLd = Record<string, unknown>;

interface PublicSeoAlternate {
  /** hreflang code, e.g. "en", "pl", "de". */
  locale: string;
  /** Absolute-from-root path, e.g. "/pl/guides/x". */
  path: string;
}

interface PublicSeoProps {
  title: string;
  description: string;
  path: string;
  ogTitle?: string;
  ogDescription?: string;
  jsonLd?: JsonLd | JsonLd[];
  /** Document language for this page; defaults to the active UI language. */
  locale?: string;
  /**
   * hreflang alternates for this page (include this page itself). When an "en"
   * alternate is present it is also emitted as x-default.
   */
  alternates?: PublicSeoAlternate[];
}

const DEFAULT_ORIGIN = "https://autoverdict.app";

function setMeta(selector: string, attr: "name" | "property", key: string, content: string) {
  let tag = document.head.querySelector<HTMLMetaElement>(selector);
  if (!tag) {
    tag = document.createElement("meta");
    tag.setAttribute(attr, key);
    document.head.appendChild(tag);
  }
  tag.content = content;
}

function setCanonical(url: string) {
  let link = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!link) {
    link = document.createElement("link");
    link.rel = "canonical";
    document.head.appendChild(link);
  }
  link.href = url;
}

function setAlternates(origin: string, alternates: PublicSeoAlternate[]) {
  document.head.querySelectorAll("link[data-hreflang]").forEach((node) => node.remove());
  const entries = [...alternates];
  const enAlt = alternates.find((alt) => alt.locale === "en");
  if (enAlt) entries.push({ locale: "x-default", path: enAlt.path });
  for (const alt of entries) {
    const link = document.createElement("link");
    link.rel = "alternate";
    link.hreflang = alt.locale;
    link.href = new URL(alt.path, origin).toString();
    link.dataset.hreflang = "true";
    document.head.appendChild(link);
  }
}

export function PublicSeo({
  title,
  description,
  path,
  ogTitle = title,
  ogDescription = description,
  jsonLd,
  locale,
  alternates,
}: PublicSeoProps) {
  const { i18n } = useTranslation();
  const lang = locale ?? i18n.language;

  useEffect(() => {
    const origin = window.location.origin || DEFAULT_ORIGIN;
    const canonical = new URL(path, origin).toString();
    const image = new URL("/og-image.svg", origin).toString();

    document.title = title;
    document.documentElement.lang = lang;
    setMeta('meta[name="description"]', "name", "description", description);
    setCanonical(canonical);
    setMeta('meta[property="og:type"]', "property", "og:type", "website");
    setMeta('meta[property="og:url"]', "property", "og:url", canonical);
    setMeta('meta[property="og:title"]', "property", "og:title", ogTitle);
    setMeta('meta[property="og:description"]', "property", "og:description", ogDescription);
    setMeta('meta[property="og:image"]', "property", "og:image", image);
    setMeta('meta[property="og:locale"]', "property", "og:locale", lang);
    setMeta('meta[name="twitter:card"]', "name", "twitter:card", "summary_large_image");
    setMeta('meta[name="twitter:title"]', "name", "twitter:title", ogTitle);
    setMeta('meta[name="twitter:description"]', "name", "twitter:description", ogDescription);
    setMeta('meta[name="twitter:image"]', "name", "twitter:image", image);

    setAlternates(origin, alternates ?? []);

    document.querySelectorAll("script[data-public-json-ld]").forEach((node) => node.remove());
    const items = Array.isArray(jsonLd) ? jsonLd : jsonLd ? [jsonLd] : [];
    for (const item of items) {
      const script = document.createElement("script");
      script.type = "application/ld+json";
      script.dataset.publicJsonLd = "true";
      script.text = JSON.stringify(item);
      document.head.appendChild(script);
    }
  }, [alternates, description, lang, jsonLd, ogDescription, ogTitle, path, title]);

  return null;
}

// TODO(seo): This metadata is client-side because the frontend is a Vite SPA.
// Before serious SEO work, add prerendering, split public marketing pages into
// an SSR/SSG site, or move public pages to an SSR/SSG framework.
