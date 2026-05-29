"use client";

import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { ChecksGrid, FaqList, FinalCta, HowSteps, InputSources, PricingCards, ReportIncludes, ReportPreviewCard } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";

export default function HomePage() {
  const { t } = useTranslation();
  const jsonLd = [
    {
      "@context": "https://schema.org",
      "@type": "WebApplication",
      name: "AutoVerdict",
      applicationCategory: "BusinessApplication",
      operatingSystem: "Web",
      description: t("public.meta.home.description"),
      offers: { "@type": "Offer", price: "20", priceCurrency: "PLN" },
    },
    faqJsonLd(t),
  ];

  return (
    <PublicLayout>
      <PublicSeo
        path="/"
        title={t("public.meta.home.title")}
        description={t("public.meta.home.description")}
        ogTitle={t("public.meta.home.ogTitle")}
        ogDescription={t("public.meta.home.ogDescription")}
        jsonLd={jsonLd}
      />
      <main>
        <section className="relative overflow-hidden bg-[radial-gradient(circle_at_20%_20%,rgba(124,156,255,0.16),transparent_32%),radial-gradient(circle_at_80%_10%,rgba(245,158,11,0.08),transparent_26%),#05080D] before:pointer-events-none before:absolute before:right-[8%] before:top-[8%] before:hidden before:h-[680px] before:w-[680px] before:rounded-full before:bg-[radial-gradient(circle,rgba(117,146,255,0.13),transparent_62%)] before:blur-sm md:before:block">
          <div className="relative mx-auto grid max-w-[1200px] gap-10 px-5 py-14 md:grid-cols-[1.05fr_0.95fr] md:py-24 lg:px-8">
            <div className="flex flex-col justify-center">
              <p className="text-sm font-bold text-[#9AB3FF]">{t("public.hero.eyebrow")}</p>
              <h1 className="mt-4 max-w-3xl text-4xl font-extrabold leading-tight text-white md:text-6xl">
                {t("public.hero.title")}
              </h1>
              <p className="mt-6 max-w-2xl text-base leading-8 text-slate-300 md:text-lg">
                {t("public.hero.body")}
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <a href="/api/auth/google" className="av-btn-primary">{t("public.cta.analyze")}</a>
                <Link to="/sample-report" className="av-btn-secondary">{t("public.cta.sample")}</Link>
              </div>
              <p className="mt-5 text-sm font-semibold text-[#AFC0FF]">{t("public.hero.free")}</p>
              <p className="mt-1.5 text-[13px] text-slate-500">{t("public.hero.trust")}</p>
            </div>
            <ReportPreviewCard />
          </div>
        </section>

        <Section className="py-8 md:py-10">
          <div className="grid gap-3 md:grid-cols-4">
            {["structured", "questions", "checklist", "buyers"].map((key) => (
              <div key={key} className="rounded-2xl border border-slate-400/10 bg-[#101722] px-5 py-4 text-sm font-bold text-slate-200">
                {t(`public.trust.${key}`)}
              </div>
            ))}
          </div>
        </Section>

        <Section>
          <SectionHeader title={t("public.inputsTitle")} lead={t("public.inputsLead")} />
          <InputSources />
        </Section>

        <Section>
          <SectionHeader title={t("public.checksTitle")} lead={t("public.checksLead")} />
          <ChecksGrid />
        </Section>

        <Section>
          <ReportIncludes />
        </Section>

        <Section>
          <div className="grid gap-8 md:grid-cols-[0.9fr_1.1fr]">
            <SectionHeader title={t("public.samplePreviewTitle")} lead={t("public.samplePreviewLead")} />
            <ReportPreviewCard compact />
          </div>
        </Section>

        <Section>
          <SectionHeader title={t("public.stepsTitle")} lead={t("public.stepsLead")} />
          <HowSteps />
        </Section>

        <Section>
          <SectionHeader title={t("public.chatbotTitle")} lead={t("public.chatbotLead")} />
          <div className="grid gap-4 md:grid-cols-2">
            <CompareCard title={t("public.chatbot.genericTitle")} body={t("public.chatbot.genericBody")} />
            <CompareCard title={t("public.chatbot.autoTitle")} body={t("public.chatbot.autoBody")} highlighted />
          </div>
        </Section>

        <Section>
          <SectionHeader title={t("public.pricingTitle")} lead={t("public.pricingLead")} />
          <PricingCards />
        </Section>

        <Section>
          <SectionHeader title={t("public.safetyTitle")} lead={t("public.safetyLead")} />
        </Section>

        <Section>
          <SectionHeader title={t("public.seoTitle")} lead={t("public.seoLead")} />
          <FaqList />
        </Section>

        <Section>
          <FinalCta />
        </Section>
      </main>
    </PublicLayout>
  );
}

function SectionHeader({ title, lead }: { title: string; lead: string }) {
  return (
    <div className="mb-9 max-w-3xl">
      <h2 className="text-3xl font-extrabold leading-tight text-white md:text-4xl">{title}</h2>
      <p className="mt-4 text-base leading-7 text-slate-400 md:text-lg">{lead}</p>
    </div>
  );
}

function CompareCard({ title, body, highlighted }: { title: string; body: string; highlighted?: boolean }) {
  return (
    <div className={`rounded-2xl border p-6 ${highlighted ? "border-[#7C9CFF]/35 bg-[#7C9CFF]/10" : "border-slate-400/10 bg-[#101722]"}`}>
      <h3 className="text-lg font-bold text-white">{title}</h3>
      <p className="mt-3 text-sm leading-6 text-slate-400">{body}</p>
    </div>
  );
}

function faqJsonLd(t: (key: string) => string) {
  const keys = ["replaceInspection", "partialInfo", "beforeTravel", "safeToBuy", "marketplaces", "notExpert", "credits", "chatbot"];
  return {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: keys.map((key) => ({
      "@type": "Question",
      name: t(`public.faq.${key}.q`),
      acceptedAnswer: { "@type": "Answer", text: t(`public.faq.${key}.a`) },
    })),
  };
}
