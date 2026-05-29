import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { CheckCircle2, ClipboardCheck, FileText, HelpCircle, ImagePlus, Link2, MessageSquare, ShieldCheck } from "lucide-react";
import { cn } from "@/lib/utils";

export function RiskBadge({ label, tone = "medium" }: { label: string; tone?: "low" | "medium" | "high" | "unknown" | "brand" }) {
  const toneClass = {
    low: "border-emerald-400/35 bg-emerald-400/12 text-emerald-300",
    medium: "border-amber-400/40 bg-amber-400/14 text-amber-300",
    high: "border-red-400/40 bg-red-400/14 text-red-300",
    unknown: "border-slate-300/30 bg-slate-300/12 text-slate-300",
    brand: "border-[#7C9CFF]/35 bg-[#7C9CFF]/12 text-[#AFC0FF]",
  }[tone];
  return <span className={cn("inline-flex rounded-full border px-3 py-1 text-xs font-bold", toneClass)}>{label}</span>;
}

export function ReportPreviewCard({ compact = false }: { compact?: boolean }) {
  const { t } = useTranslation();
  const concerns = ["service", "import", "claim"] as const;
  return (
    <div className="rounded-2xl border border-[#7C9CFF]/25 bg-[#101827] p-5 shadow-2xl shadow-black/30">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs font-bold uppercase text-slate-500">{t("public.reportPreview.eyebrow")}</p>
          <h2 className="mt-1 text-lg font-bold text-white">{t("public.reportPreview.title")}</h2>
        </div>
        <RiskBadge label={t("public.reportPreview.risk")} />
      </div>
      <p className="mt-4 text-sm font-semibold text-slate-200">{t("public.reportPreview.vehicle")}</p>
      <p className="mt-1 text-xs font-semibold text-[#AFC0FF]">{t("public.reportPreview.confidence")}</p>
      <div className="mt-5 space-y-3">
        <p className="text-xs font-bold uppercase text-slate-500">{t("public.reportPreview.concerns")}</p>
        <ul className="space-y-2">
          {concerns.map((key) => (
            <li key={key} className="flex gap-2 text-sm text-slate-300">
              <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-amber-300" />
              {t(`public.reportPreview.items.${key}`)}
            </li>
          ))}
        </ul>
      </div>
      {!compact && (
        <div className="mt-5 space-y-4 border-t border-slate-400/10 pt-4">
          <div>
          <p className="text-xs font-bold uppercase text-slate-500">{t("public.reportPreview.nextLabel")}</p>
          <p className="mt-2 text-sm leading-6 text-slate-300">{t("public.reportPreview.next")}</p>
          </div>
          <div className="rounded-xl border border-slate-400/10 bg-[#0D1420] p-3">
            <p className="text-xs font-bold uppercase text-slate-500">{t("public.reportPreview.recommendationLabel")}</p>
            <p className="mt-1 text-sm font-semibold text-slate-200">{t("public.reportPreview.recommendation")}</p>
          </div>
        </div>
      )}
      <p className="mt-4 text-xs text-slate-500">{t("public.reportPreview.disclaimer")}</p>
    </div>
  );
}

export function InputSources() {
  const { t } = useTranslation();
  const items = [
    { key: "listing", icon: Link2 },
    { key: "messages", icon: MessageSquare },
    { key: "details", icon: FileText },
    { key: "photos", icon: ImagePlus },
  ] as const;
  return (
    <div className="grid gap-4 md:grid-cols-4">
      {items.map(({ key, icon: Icon }) => (
        <div key={key} className="rounded-2xl border border-slate-400/10 bg-[#101722] p-5">
          <Icon className="h-5 w-5 text-[#7C9CFF]" />
          <h3 className="mt-4 text-base font-bold text-white">{t(`public.inputs.${key}.title`)}</h3>
          <p className="mt-2 text-sm leading-6 text-slate-400">{t(`public.inputs.${key}.body`)}</p>
        </div>
      ))}
    </div>
  );
}

export function ChecksGrid() {
  const { t } = useTranslation();
  const keys = ["missing", "claims", "costs", "questions", "checklist", "recommendation"] as const;
  return (
    <div className="grid gap-4 md:grid-cols-3">
      {keys.map((key) => (
        <div key={key} className="rounded-2xl border border-slate-400/10 bg-[#101722] p-5">
          <ShieldCheck className="h-5 w-5 text-[#7C9CFF]" />
          <h3 className="mt-4 text-base font-bold text-white">{t(`public.checks.${key}.title`)}</h3>
          <p className="mt-2 text-sm leading-6 text-slate-400">{t(`public.checks.${key}.body`)}</p>
        </div>
      ))}
    </div>
  );
}

export function HowSteps() {
  const { t } = useTranslation();
  const keys = ["submit", "analyze", "act"] as const;
  return (
    <div className="grid gap-4 md:grid-cols-3">
      {keys.map((key, index) => (
        <div key={key} className="rounded-2xl border border-slate-400/10 bg-[#101722] p-6">
          <span className="text-xs font-bold text-slate-500">0{index + 1}</span>
          <h3 className="mt-3 text-lg font-bold text-white">{t(`public.steps.${key}.title`)}</h3>
          <p className="mt-2 text-sm leading-6 text-slate-400">{t(`public.steps.${key}.body`)}</p>
        </div>
      ))}
    </div>
  );
}

export function PricingCards() {
  const { t } = useTranslation();
  const cards = [
    { key: "one", price: "20 PLN", checks: "1" },
    { key: "three", price: "40 PLN", checks: "3", featured: true },
  ] as const;
  return (
    <div className="space-y-5">
      <div className="rounded-2xl border border-[#7C9CFF]/30 bg-[#7C9CFF]/10 p-5 md:flex md:items-center md:justify-between md:gap-6">
        <div>
          <p className="text-xs font-bold uppercase text-[#AFC0FF]">{t("public.pricing.freeLabel")}</p>
          <h3 className="mt-1 text-lg font-bold text-white">{t("public.pricing.freeTitle")}</h3>
        </div>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-300 md:mt-0">{t("public.pricing.freeBody")}</p>
      </div>
      <div className="grid gap-5 md:grid-cols-2">
      {cards.map((card) => (
        <div key={card.key} className={cn("rounded-2xl border bg-[#101827] p-6 shadow-xl shadow-black/15", card.featured ? "border-[#7C9CFF]/45" : "border-slate-400/14")}>
          <div className="flex items-center justify-between gap-4">
            <h3 className="text-xl font-bold text-white">{t(`public.pricing.${card.key}.title`)}</h3>
            {card.featured && <RiskBadge label={t("public.pricing.better")} tone="brand" />}
          </div>
          <p className="mt-4 text-4xl font-extrabold text-white">{card.price}</p>
          <p className="mt-3 text-sm leading-6 text-slate-400">{t(`public.pricing.${card.key}.body`)}</p>
          <p className="mt-3 text-sm font-semibold text-slate-300">{t("public.pricing.creditRule")}</p>
          <ul className="mt-5 space-y-2">
            {["report", "risk", "missing", "questions", "checklist"].map((item) => (
              <li key={item} className="flex gap-2 text-sm text-slate-300">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-300" />
                {t(`public.pricing.includes.${item}`)}
              </li>
            ))}
          </ul>
          <a href="/api/auth/google" className="av-btn-primary mt-6 w-full justify-center">
            {t(`public.pricing.${card.key}.cta`)}
          </a>
        </div>
      ))}
      </div>
      <p className="text-xs leading-5 text-slate-500">{t("public.pricing.safetyNote")}</p>
    </div>
  );
}

export function FaqList() {
  const { t } = useTranslation();
  const keys = ["replaceInspection", "partialInfo", "beforeTravel", "safeToBuy", "marketplaces", "notExpert", "credits", "chatbot"] as const;
  return (
    <div className="grid gap-4 md:grid-cols-2">
      {keys.map((key) => (
        <div key={key} className="rounded-2xl border border-slate-400/10 bg-[#101722] p-5">
          <HelpCircle className="h-5 w-5 text-[#7C9CFF]" />
          <h3 className="mt-3 text-base font-bold text-white">{t(`public.faq.${key}.q`)}</h3>
          <p className="mt-2 text-sm leading-6 text-slate-400">{t(`public.faq.${key}.a`)}</p>
        </div>
      ))}
    </div>
  );
}

export function FinalCta() {
  const { t } = useTranslation();
  return (
    <div className="rounded-3xl border border-[#7C9CFF]/25 bg-[#7C9CFF]/10 p-8 text-center md:p-10">
      <h2 className="text-3xl font-extrabold text-white md:text-4xl">{t("public.finalCta.title")}</h2>
      <p className="mx-auto mt-3 max-w-2xl text-base text-slate-300">{t("public.finalCta.body")}</p>
      <p className="mt-4 text-sm font-bold text-[#AFC0FF]">{t("public.finalCta.free")}</p>
      <div className="mt-6 flex flex-wrap justify-center gap-3">
        <a href="/api/auth/google" className="av-btn-primary">{t("public.cta.startFree")}</a>
        <Link to="/sample-report" className="av-btn-secondary">{t("public.cta.sample")}</Link>
      </div>
    </div>
  );
}

export function ReportIncludes() {
  const { t } = useTranslation();
  const keys = ["risk", "facts", "missing", "signals", "questions", "checklist", "model", "recommendation"] as const;
  return (
    <div className="grid gap-8 md:grid-cols-[0.85fr_1.15fr] md:items-center">
      <div>
        <h2 className="text-3xl font-extrabold leading-tight text-white md:text-4xl">{t("public.reportIncludes.title")}</h2>
        <p className="mt-4 text-base leading-7 text-slate-400 md:text-lg">{t("public.reportIncludes.lead")}</p>
        <Link to="/sample-report" className="av-btn-secondary mt-6">{t("public.cta.sample")}</Link>
      </div>
      <div className="rounded-2xl border border-[#7C9CFF]/28 bg-[#101827] p-5 shadow-xl shadow-black/20">
        <div className="mb-4 flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase text-[#AFC0FF]">{t("public.reportIncludes.badge")}</p>
            <h3 className="mt-1 text-xl font-extrabold text-white">{t("public.reportIncludes.cardTitle")}</h3>
          </div>
          <ClipboardCheck className="h-6 w-6 text-[#7C9CFF]" />
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          {keys.map((key) => (
            <div key={key} className="rounded-xl border border-slate-400/10 bg-[#0D1420] p-3">
              <div className="flex gap-2">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-300" />
                <div>
                  <p className="text-sm font-bold text-white">{t(`public.reportIncludes.items.${key}.title`)}</p>
                  <p className="mt-1 text-xs leading-5 text-slate-400">{t(`public.reportIncludes.items.${key}.body`)}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
