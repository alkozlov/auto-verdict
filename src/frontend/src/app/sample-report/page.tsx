"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta, RiskBadge } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";

export default function SampleReportPage() {
  const { t } = useTranslation();
  return (
    <PublicLayout>
      <PublicSeo path="/sample-report" title={t("public.meta.sample.title")} description={t("public.meta.sample.description")} />
      <main>
        <Section className="border-t-0">
          <h1 className="max-w-3xl text-4xl font-extrabold text-white md:text-6xl">{t("public.samplePage.title")}</h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">{t("public.samplePage.lead")}</p>
        </Section>
        <Section>
          <article className="rounded-3xl border border-slate-400/10 bg-[#101722] p-6 md:p-8">
            <div className="flex flex-wrap items-center gap-3">
              <RiskBadge label={t("public.reportPreview.risk")} />
              <RiskBadge label={t("public.samplePage.confidence")} tone="brand" />
            </div>
            {["vehicle", "riskSignals", "missing", "questions", "checklist", "recommendation", "disclaimer"].map((key) => (
              <section key={key} className="mt-8 border-t border-slate-400/10 pt-6">
                <h2 className="text-2xl font-bold text-white">{t(`public.samplePage.sections.${key}.title`)}</h2>
                <p className="mt-3 whitespace-pre-line text-sm leading-7 text-slate-300">{t(`public.samplePage.sections.${key}.body`)}</p>
              </section>
            ))}
          </article>
        </Section>
        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
