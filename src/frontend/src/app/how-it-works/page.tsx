"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta, HowSteps, InputSources, ReportPreviewCard } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";

export default function HowItWorksPage() {
  const { t } = useTranslation();
  return (
    <PublicLayout>
      <PublicSeo path="/how-it-works" title={t("public.meta.how.title")} description={t("public.meta.how.description")} />
      <main>
        <Section className="border-t-0 bg-[#070A0F]">
          <h1 className="max-w-3xl text-4xl font-extrabold text-white md:text-6xl">{t("public.howPage.title")}</h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">{t("public.howPage.lead")}</p>
        </Section>
        <Section><HowSteps /></Section>
        <Section><InputSources /></Section>
        <Section>
          <div className="grid gap-8 md:grid-cols-2">
            <div>
              <h2 className="text-3xl font-extrabold text-white">{t("public.howPage.outputTitle")}</h2>
              <p className="mt-4 text-slate-400">{t("public.howPage.outputLead")}</p>
            </div>
            <ReportPreviewCard />
          </div>
        </Section>
        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
