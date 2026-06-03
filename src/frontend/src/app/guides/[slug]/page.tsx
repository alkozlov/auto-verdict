"use client";

import { useEffect } from "react";
import { Link, Navigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta } from "@/components/public/PublicComponents";
import { ReportMarkdownViewer } from "@/components/ReportMarkdownViewer";
import { PublicSeo } from "@/lib/public-seo";
import { getGuide, localesForSlug } from "@/content/guides/registry";
import { guideAlternates, guidePath, guidesIndexPath, localeFromParam, ORIGIN } from "@/content/guides/routing";
import { DEFAULT_LOCALE } from "@/i18n/languages";

const FAQ_HEADING: Record<string, string> = {
  en: "Frequently asked questions",
  pl: "Najczęściej zadawane pytania",
  de: "Häufig gestellte Fragen",
  uk: "Поширені запитання",
  fr: "Questions fréquentes",
};

const BREADCRUMB_HOME: Record<string, string> = {
  en: "Home", pl: "Strona główna", de: "Startseite", uk: "Головна", fr: "Accueil",
};
const BREADCRUMB_GUIDES: Record<string, string> = {
  en: "Buying guides", pl: "Poradniki zakupowe", de: "Kaufratgeber", uk: "Посібники з купівлі", fr: "Guides d'achat",
};

export default function GuidePage() {
  const params = useParams<{ locale?: string; slug: string }>();
  const slug = params.slug ?? "";
  const locale = localeFromParam(params.locale);
  const { i18n } = useTranslation();

  // Keep the surrounding UI chrome in the page's language.
  useEffect(() => {
    if (locale && i18n.language !== locale) i18n.changeLanguage(locale);
  }, [locale, i18n]);

  // Unsupported / redundant "en" prefix -> English canonical URL.
  if (locale === null) return <Navigate to={`/guides/${slug}`} replace />;

  const guide = getGuide(slug, locale);
  if (!guide) return <Navigate to={guidesIndexPath(locale)} replace />;

  // No real translation for this locale -> don't serve duplicate content under a localized URL.
  if (locale !== DEFAULT_LOCALE && !localesForSlug(slug).includes(locale)) {
    return <Navigate to={`/guides/${slug}`} replace />;
  }

  const path = guidePath(slug, locale);
  const url = `${ORIGIN}${path}`;

  const jsonLd = [
    {
      "@context": "https://schema.org",
      "@type": "Article",
      headline: guide.h1,
      description: guide.description,
      datePublished: guide.updated,
      dateModified: guide.updated,
      inLanguage: locale,
      mainEntityOfPage: { "@type": "WebPage", "@id": url },
      author: { "@type": "Organization", name: "AutoVerdict" },
      publisher: { "@type": "Organization", name: "AutoVerdict" },
      about: { "@type": "Car", manufacturer: guide.make, model: guide.model },
    },
    {
      "@context": "https://schema.org",
      "@type": "FAQPage",
      mainEntity: guide.faq.map((item) => ({
        "@type": "Question",
        name: item.q,
        acceptedAnswer: { "@type": "Answer", text: item.a },
      })),
    },
    {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      itemListElement: [
        { "@type": "ListItem", position: 1, name: BREADCRUMB_HOME[locale], item: `${ORIGIN}/` },
        { "@type": "ListItem", position: 2, name: BREADCRUMB_GUIDES[locale], item: `${ORIGIN}${guidesIndexPath(locale)}` },
        { "@type": "ListItem", position: 3, name: `${guide.make} ${guide.model}`, item: url },
      ],
    },
  ];

  return (
    <PublicLayout>
      <PublicSeo
        path={path}
        title={guide.title}
        description={guide.description}
        ogTitle={guide.h1}
        locale={locale}
        alternates={guideAlternates(slug)}
        jsonLd={jsonLd}
      />
      <main>
        <Section className="border-t-0 bg-[#070A0F]">
          <nav aria-label="Breadcrumb" className="mb-6 text-sm text-slate-500">
            <Link to="/" className="hover:text-slate-300">{BREADCRUMB_HOME[locale]}</Link>
            <span className="mx-2">/</span>
            <Link to={guidesIndexPath(locale)} className="hover:text-slate-300">{BREADCRUMB_GUIDES[locale]}</Link>
            <span className="mx-2">/</span>
            <span className="text-slate-300">{guide.make} {guide.model}</span>
          </nav>
          <p className="text-xs font-bold uppercase tracking-wide text-[#AFC0FF]">{guide.years}</p>
          <h1 className="mt-2 max-w-3xl text-4xl font-extrabold text-white md:text-5xl">{guide.h1}</h1>
          <p className="mt-5 max-w-3xl text-lg leading-8 text-slate-300">{guide.intro}</p>
        </Section>

        <Section className="border-t-0">
          <div className="max-w-3xl">
            <ReportMarkdownViewer markdown={guide.bodyMarkdown} />
          </div>
        </Section>

        <Section>
          <div className="max-w-3xl">
            <h2 className="text-3xl font-extrabold text-white">{FAQ_HEADING[locale]}</h2>
            <div className="mt-6 space-y-4">
              {guide.faq.map((item) => (
                <div key={item.q} className="rounded-2xl border border-slate-400/10 bg-[#101722] p-5">
                  <h3 className="text-base font-bold text-white">{item.q}</h3>
                  <p className="mt-2 text-sm leading-7 text-slate-400">{item.a}</p>
                </div>
              ))}
            </div>
          </div>
        </Section>

        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
