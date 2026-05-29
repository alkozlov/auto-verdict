"use client";

import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getToken } from "@/lib/auth";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";

const HOW_IT_WORKS = [
  {
    step: "01",
    titleKey: "home.steps.paste.title",
    descKey: "home.steps.paste.desc",
  },
  {
    step: "02",
    titleKey: "home.steps.review.title",
    descKey: "home.steps.review.desc",
  },
  {
    step: "03",
    titleKey: "home.steps.nextSteps.title",
    descKey: "home.steps.nextSteps.desc",
  },
];

const CHECKS_LIST = [
  "home.checks.suspiciousWording",
  "home.checks.missingServiceHistory",
  "home.checks.mileageConcerns",
  "home.checks.importUncertainty",
  "home.checks.accidentAmbiguity",
  "home.checks.sellerClaims",
  "home.checks.modelIssues",
  "home.checks.ownershipCosts",
  "home.checks.sellerQuestions",
  "home.checks.inspectionChecklist",
];

export default function HomePage() {
  const { t } = useTranslation();
  const [loggedIn, setLoggedIn] = useState(false);

  useEffect(() => {
    setLoggedIn(!!getToken());
  }, []);

  const primaryCta = loggedIn ? (
    <Link
      to="/garage/check"
      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand px-7 text-sm font-semibold text-page transition-all hover:brightness-105"
    >
      {t("home.goToGarage")}
    </Link>
  ) : (
    <a
      href="/api/auth/google"
      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand px-7 text-sm font-semibold text-page transition-all hover:brightness-105"
    >
      {t("auth.continueWithGoogle")}
    </a>
  );

  return (
    <div className="min-h-screen bg-page flex flex-col">
      {/* Header */}
      <header className="sticky top-0 z-30 border-b border-white/6 bg-page/80 backdrop-blur-sm">
        <div className="mx-auto max-w-[1120px] px-5 lg:px-8 flex h-16 items-center justify-between">
          <span className="text-[15px] font-[700] text-hi tracking-tight">{t("app.name")}</span>
          <div className="flex items-center gap-5">
            <a
              href="#how-it-works"
              className="hidden sm:block text-sm text-dim transition-colors hover:text-mid"
            >
              {t("home.howItWorksLink")}
            </a>
            <LanguageSwitcher />
            {primaryCta}
          </div>
        </div>
      </header>

      <main className="flex-1">
        {/* Hero */}
        <section className="mx-auto max-w-[1120px] px-5 lg:px-8 pt-20 pb-24 lg:pt-28 lg:pb-32">
          <div className="max-w-[640px]">
            <h1 className="text-[38px] sm:text-[48px] font-[680] text-hi leading-[1.08] tracking-tight">
              {t("home.heroTitleLine1")}<br />{t("home.heroTitleLine2")}
            </h1>
            <p className="mt-6 text-base text-mid leading-relaxed max-w-[520px]">
              {t("home.heroBody")}
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-5">
              {primaryCta}
              <p className="text-xs text-dim leading-relaxed">
                {t("home.disclaimerShortLine1")}<br />
                {t("home.disclaimerShortLine2")}
              </p>
            </div>
          </div>

          {/* Preview card */}
          <div className="mt-14 max-w-[400px] rounded-xl border border-white/6 bg-surface p-5 space-y-4">
            <div className="inline-flex items-center rounded-sm border border-warn/30 bg-warn-tint px-3 py-1 text-sm font-semibold text-warn">
              {t("home.previewVerdict")}
            </div>
            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wider text-dim">
                {t("home.previewConcernsLabel")}
              </p>
              <ul className="space-y-1.5">
                {[
                  "home.previewConcerns.serviceHistory",
                  "home.previewConcerns.importHistory",
                  "home.previewConcerns.sellerDetails",
                ].map((item) => (
                  <li key={item} className="flex items-start gap-2 text-sm text-mid">
                    <span className="mt-[7px] h-1.5 w-1.5 shrink-0 rounded-full bg-warn" />
                    {t(item)}
                  </li>
                ))}
              </ul>
            </div>
            <div className="space-y-1.5 border-t border-white/6 pt-4">
              <p className="text-xs font-semibold uppercase tracking-wider text-dim">
                {t("home.recommendedNextStepLabel")}
              </p>
              <p className="text-sm text-mid">
                {t("home.recommendedNextStep")}
              </p>
            </div>
          </div>
        </section>

        {/* How it works */}
        <section id="how-it-works" className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-10 text-[22px] font-[650] text-hi">{t("home.howItWorksTitle")}</h2>
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
              {HOW_IT_WORKS.map(({ step, titleKey, descKey }) => (
                <div
                  key={step}
                  className="rounded-xl border border-white/6 bg-surface p-6 space-y-3"
                >
                  <span className="text-xs font-semibold text-dim">{step}</span>
                  <p className="text-sm font-semibold text-hi">{t(titleKey)}</p>
                  <p className="text-sm text-dim leading-relaxed">{t(descKey)}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* What AutoVerdict checks */}
        <section className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-8 text-[22px] font-[650] text-hi">{t("home.checksTitle")}</h2>
            <div className="grid grid-cols-1 gap-x-12 gap-y-3 sm:grid-cols-2">
              {CHECKS_LIST.map((item) => (
                <div key={item} className="flex items-center gap-2.5 text-sm text-mid">
                  <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-brand" />
                  {t(item)}
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Who it's for */}
        <section className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <div className="max-w-[640px]">
              <h2 className="mb-4 text-[22px] font-[650] text-hi">
                {t("home.audienceTitle")}
              </h2>
              <p className="text-sm text-mid leading-relaxed">
                {t("home.audienceBody")}
              </p>
            </div>
          </div>
        </section>

        {/* Safety / Disclaimer */}
        <section className="border-t border-white/6 bg-surface py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-3 text-[18px] font-[650] text-hi">
              {t("home.safetyTitle")}
            </h2>
            <p className="max-w-[600px] text-sm text-dim leading-relaxed">
              {t("home.safetyBody")}
            </p>
          </div>
        </section>

        {/* SEO content */}
        <section className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8 space-y-8">
            <h2 className="text-[18px] font-[650] text-hi">
              {t("home.seoTitle")}
            </h2>
            <p className="max-w-[680px] text-sm text-dim leading-relaxed">
              {t("home.seoBody")}
            </p>
            <div className="space-y-6">
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  {t("home.seoWhyTitle")}
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  {t("home.seoWhyBody")}
                </p>
              </div>
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  {t("home.seoInfoTitle")}
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  {t("home.seoInfoBody")}
                </p>
              </div>
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  {t("home.seoOtomotoTitle")}
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  {t("home.seoOtomotoBody")}
                </p>
              </div>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-white/6 py-8">
        <div className="mx-auto max-w-[1120px] px-5 lg:px-8 flex flex-wrap items-center justify-between gap-4">
          <span className="text-sm font-[700] text-hi">{t("app.name")}</span>
          <div className="flex gap-6 text-xs text-dim">
            <span>{t("home.footer.privacy")}</span>
            <span>{t("home.footer.terms")}</span>
            <span>{t("home.footer.contact")}</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
