"use client";

import { Link } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";
import { GUIDES } from "@/content/guides/registry";

const ORIGIN = "https://autoverdict.app";

const TITLE = "Used Car Buying Guides: Model Risks & What to Check | AutoVerdict";
const DESCRIPTION =
  "Model-by-model used car buying guides — what to check, what to ask the seller, and the risks to verify before you view or pay for a car.";
const H1 = "Used car buying guides";
const LEAD =
  "Practical, model-specific checklists for buying used. Each guide covers what to verify, the questions to ask the seller, and an inspection checklist — then run your own listing for a free, personalised risk report.";

export default function GuidesIndexPage() {
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "CollectionPage",
    name: H1,
    description: DESCRIPTION,
    url: `${ORIGIN}/guides`,
    mainEntity: {
      "@type": "ItemList",
      itemListElement: GUIDES.map((guide, index) => ({
        "@type": "ListItem",
        position: index + 1,
        name: `${guide.make} ${guide.model}`,
        url: `${ORIGIN}/guides/${guide.slug}`,
      })),
    },
  };

  return (
    <PublicLayout>
      <PublicSeo path="/guides" title={TITLE} description={DESCRIPTION} ogTitle={H1} jsonLd={jsonLd} />
      <main>
        <Section className="border-t-0 bg-[#070A0F]">
          <h1 className="max-w-3xl text-4xl font-extrabold text-white md:text-6xl">{H1}</h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">{LEAD}</p>
        </Section>

        <Section>
          <div className="grid gap-4 md:grid-cols-2">
            {GUIDES.map((guide) => (
              <Link
                key={guide.slug}
                to={`/guides/${guide.slug}`}
                className="group rounded-2xl border border-slate-400/10 bg-[#101722] p-6 transition-colors hover:border-[#7C9CFF]/40"
              >
                <p className="text-xs font-bold uppercase tracking-wide text-[#AFC0FF]">{guide.years}</p>
                <h2 className="mt-2 text-xl font-bold text-white">{guide.make} {guide.model}</h2>
                <p className="mt-3 text-sm leading-6 text-slate-400">{guide.intro}</p>
                <span className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-[#7C9CFF]">
                  Read the guide
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
                </span>
              </Link>
            ))}
          </div>
        </Section>

        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
