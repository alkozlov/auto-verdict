"use client";

import { Link, Navigate, useParams } from "react-router-dom";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta } from "@/components/public/PublicComponents";
import { ReportMarkdownViewer } from "@/components/ReportMarkdownViewer";
import { PublicSeo } from "@/lib/public-seo";
import { getGuide } from "@/content/guides/registry";

const ORIGIN = "https://autoverdict.app";

export default function GuidePage() {
  const { slug } = useParams<{ slug: string }>();
  const guide = slug ? getGuide(slug) : undefined;

  if (!guide) {
    return <Navigate to="/guides" replace />;
  }

  const path = `/guides/${guide.slug}`;
  const url = `${ORIGIN}${path}`;

  const jsonLd = [
    {
      "@context": "https://schema.org",
      "@type": "Article",
      headline: guide.h1,
      description: guide.description,
      datePublished: guide.updated,
      dateModified: guide.updated,
      inLanguage: "en",
      mainEntityOfPage: { "@type": "WebPage", "@id": url },
      author: { "@type": "Organization", name: "AutoVerdict" },
      publisher: { "@type": "Organization", name: "AutoVerdict" },
      about: {
        "@type": "Car",
        manufacturer: guide.make,
        model: guide.model,
      },
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
        { "@type": "ListItem", position: 1, name: "Home", item: ORIGIN },
        { "@type": "ListItem", position: 2, name: "Buying guides", item: `${ORIGIN}/guides` },
        { "@type": "ListItem", position: 3, name: `${guide.make} ${guide.model}`, item: url },
      ],
    },
  ];

  return (
    <PublicLayout>
      <PublicSeo path={path} title={guide.title} description={guide.description} ogTitle={guide.h1} jsonLd={jsonLd} />
      <main>
        <Section className="border-t-0 bg-[#070A0F]">
          <nav aria-label="Breadcrumb" className="mb-6 text-sm text-slate-500">
            <Link to="/" className="hover:text-slate-300">Home</Link>
            <span className="mx-2">/</span>
            <Link to="/guides" className="hover:text-slate-300">Buying guides</Link>
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
            <h2 className="text-3xl font-extrabold text-white">Frequently asked questions</h2>
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
