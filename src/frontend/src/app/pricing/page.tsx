"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FaqList, FinalCta, PricingCards } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";

export default function PricingPage() {
  const { t } = useTranslation();
  return (
    <PublicLayout>
      <PublicSeo
        path="/pricing"
        title={t("public.meta.pricing.title")}
        description={t("public.meta.pricing.description")}
        jsonLd={{ "@context": "https://schema.org", "@type": "Product", name: "AutoVerdict checks", offers: [{ "@type": "Offer", price: "20", priceCurrency: "PLN" }, { "@type": "Offer", price: "40", priceCurrency: "PLN" }] }}
      />
      <main>
        <Section className="border-t-0">
          <h1 className="max-w-3xl text-4xl font-extrabold text-white md:text-6xl">{t("public.pricingPage.title")}</h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">{t("public.pricingPage.lead")}</p>
        </Section>
        <Section><PricingCards /></Section>
        <Section>
          <h2 className="text-3xl font-extrabold text-white">{t("public.pricingPage.creditsTitle")}</h2>
          <p className="mt-4 max-w-3xl text-slate-400">{t("public.pricingPage.creditsBody")}</p>
        </Section>
        <Section><FaqList /></Section>
        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
