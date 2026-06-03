import { useEffect } from "react";
import { useTranslation } from "react-i18next";

type JsonLd = Record<string, unknown>;

interface PublicSeoProps {
  title: string;
  description: string;
  path: string;
  ogTitle?: string;
  ogDescription?: string;
  jsonLd?: JsonLd | JsonLd[];
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

export function PublicSeo({
  title,
  description,
  path,
  ogTitle = title,
  ogDescription = description,
  jsonLd,
}: PublicSeoProps) {
  const { i18n } = useTranslation();

  useEffect(() => {
    const origin = window.location.origin || DEFAULT_ORIGIN;
    const canonical = new URL(path, origin).toString();
    const image = new URL("/og-image.svg", origin).toString();

    document.title = title;
    setMeta('meta[name="description"]', "name", "description", description);
    setCanonical(canonical);
    setMeta('meta[property="og:type"]', "property", "og:type", "website");
    setMeta('meta[property="og:url"]', "property", "og:url", canonical);
    setMeta('meta[property="og:title"]', "property", "og:title", ogTitle);
    setMeta('meta[property="og:description"]', "property", "og:description", ogDescription);
    setMeta('meta[property="og:image"]', "property", "og:image", image);
    setMeta('meta[property="og:locale"]', "property", "og:locale", i18n.language);
    setMeta('meta[name="twitter:card"]', "name", "twitter:card", "summary_large_image");
    setMeta('meta[name="twitter:title"]', "name", "twitter:title", ogTitle);
    setMeta('meta[name="twitter:description"]', "name", "twitter:description", ogDescription);
    setMeta('meta[name="twitter:image"]', "name", "twitter:image", image);

    document.querySelectorAll("script[data-public-json-ld]").forEach((node) => node.remove());
    const items = Array.isArray(jsonLd) ? jsonLd : jsonLd ? [jsonLd] : [];
    for (const item of items) {
      const script = document.createElement("script");
      script.type = "application/ld+json";
      script.dataset.publicJsonLd = "true";
      script.text = JSON.stringify(item);
      document.head.appendChild(script);
    }
  }, [description, i18n.language, jsonLd, ogDescription, ogTitle, path, title]);

  return null;
}

// TODO(seo): This metadata is client-side because the frontend is a Vite SPA.
// Before serious SEO work, add prerendering, split public marketing pages into
// an SSR/SSG site, or move public pages to an SSR/SSG framework.
