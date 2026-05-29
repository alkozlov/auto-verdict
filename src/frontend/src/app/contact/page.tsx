"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { PublicSeo } from "@/lib/public-seo";

const SUPPORT_EMAIL = "support@example.com";

export default function ContactPage() {
  const { t } = useTranslation();
  return (
    <PublicLayout>
      <PublicSeo path="/contact" title={t("public.meta.contact.title")} description={t("public.meta.contact.description")} />
      <main>
        <Section className="border-t-0">
          <h1 className="text-4xl font-extrabold text-white md:text-5xl">{t("public.contact.title")}</h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-300">{t("public.contact.lead")}</p>
          <div className="mt-8 max-w-xl rounded-2xl border border-slate-400/10 bg-[#101722] p-6">
            <p className="text-sm font-bold text-white">{t("public.contact.emailLabel")}</p>
            <a href={`mailto:${SUPPORT_EMAIL}`} className="mt-2 inline-flex text-[#9AB3FF]">{SUPPORT_EMAIL}</a>
            <p className="mt-4 text-sm text-slate-400">{t("public.contact.placeholder")}</p>
          </div>
        </Section>
      </main>
    </PublicLayout>
  );
}
