"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { PublicSeo } from "@/lib/public-seo";

export default function TermsPage() {
  const { t } = useTranslation();
  const keys = ["description","disclaimer","guarantee","responsibility","credits","refund","acceptable","account","liability","contact"];
  return (
    <PublicLayout>
      <PublicSeo path="/terms" title={t("public.meta.terms.title")} description={t("public.meta.terms.description")} />
      <main>
        <Section className="border-t-0">
          <h1 className="text-4xl font-extrabold text-white md:text-5xl">{t("public.terms.title")}</h1>
          <p className="mt-4 text-sm text-slate-500">{t("public.legal.updated")}</p>
          <div className="mt-10 max-w-3xl space-y-8">
            {keys.map((key) => (
              <section key={key}>
                <h2 className="text-xl font-bold text-white">{t(`public.terms.sections.${key}.title`)}</h2>
                <p className="mt-3 text-sm leading-7 text-slate-400">{t(`public.terms.sections.${key}.body`)}</p>
              </section>
            ))}
          </div>
        </Section>
      </main>
    </PublicLayout>
  );
}
