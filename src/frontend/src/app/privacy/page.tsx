"use client";

import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { PublicSeo } from "@/lib/public-seo";

export default function PrivacyPage() {
  const { t } = useTranslation();
  return <LegalPage path="/privacy" title={t("public.meta.privacy.title")} description={t("public.meta.privacy.description")} heading={t("public.privacy.title")} keys={["who","data","use","ai","files","payments","auth","retention","deletion","security","contact"]} />;
}

function LegalPage({ path, title, description, heading, keys }: { path: string; title: string; description: string; heading: string; keys: string[] }) {
  const { t } = useTranslation();
  return (
    <PublicLayout>
      <PublicSeo path={path} title={title} description={description} />
      <main>
        <Section className="border-t-0">
          <h1 className="text-4xl font-extrabold text-white md:text-5xl">{heading}</h1>
          <p className="mt-4 text-sm text-slate-500">{t("public.legal.updated")}</p>
          <div className="mt-10 max-w-3xl space-y-8">
            {keys.map((key) => (
              <section key={key}>
                <h2 className="text-xl font-bold text-white">{t(`public.privacy.sections.${key}.title`)}</h2>
                <p className="mt-3 text-sm leading-7 text-slate-400">{t(`public.privacy.sections.${key}.body`)}</p>
              </section>
            ))}
          </div>
        </Section>
      </main>
    </PublicLayout>
  );
}
