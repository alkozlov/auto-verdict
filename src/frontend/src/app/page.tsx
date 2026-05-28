"use client";

import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getToken } from "@/lib/auth";

const HOW_IT_WORKS = [
  {
    step: "01",
    title: "Paste what you know",
    desc: "Add listing text, seller replies, VIN, notes, photos, or an Otomoto link.",
  },
  {
    step: "02",
    title: "AI reviews the risks",
    desc: "AutoVerdict checks for missing information, suspicious wording, deal risks, and model-specific concerns.",
  },
  {
    step: "03",
    title: "Get practical next steps",
    desc: "Receive seller questions, inspection points, estimated costs, and a clear recommendation.",
  },
];

const CHECKS_LIST = [
  "Suspicious wording",
  "Missing service history",
  "Mileage concerns",
  "Import-related uncertainty",
  "Accident or repair ambiguity",
  "Unclear seller claims",
  "Model-specific common issues",
  "First-year ownership costs",
  "Questions to ask before viewing",
  "Inspection checklist",
];

export default function HomePage() {
  const [loggedIn, setLoggedIn] = useState(false);

  useEffect(() => {
    setLoggedIn(!!getToken());
  }, []);

  const primaryCta = loggedIn ? (
    <Link
      to="/garage/check"
      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand px-7 text-sm font-semibold text-page transition-all hover:brightness-105"
    >
      Go to Garage
    </Link>
  ) : (
    <a
      href="/api/auth/google"
      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand px-7 text-sm font-semibold text-page transition-all hover:brightness-105"
    >
      Continue with Google
    </a>
  );

  return (
    <div className="min-h-screen bg-page flex flex-col">
      {/* Header */}
      <header className="sticky top-0 z-30 border-b border-white/6 bg-page/80 backdrop-blur-sm">
        <div className="mx-auto max-w-[1120px] px-5 lg:px-8 flex h-16 items-center justify-between">
          <span className="text-[15px] font-[700] text-hi tracking-tight">AutoVerdict</span>
          <div className="flex items-center gap-5">
            <a
              href="#how-it-works"
              className="hidden sm:block text-sm text-dim transition-colors hover:text-mid"
            >
              How it works
            </a>
            {primaryCta}
          </div>
        </div>
      </header>

      <main className="flex-1">
        {/* Hero */}
        <section className="mx-auto max-w-[1120px] px-5 lg:px-8 pt-20 pb-24 lg:pt-28 lg:pb-32">
          <div className="max-w-[640px]">
            <h1 className="text-[38px] sm:text-[48px] font-[680] text-hi leading-[1.08] tracking-tight">
              Avoid expensive<br />used-car mistakes.
            </h1>
            <p className="mt-6 text-base text-mid leading-relaxed max-w-[520px]">
              Paste listing text, seller messages, an Otomoto link, photos, VIN, or your own
              questions. AutoVerdict gives you a structured AI risk analysis before you contact the
              seller.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-5">
              {primaryCta}
              <p className="text-xs text-dim leading-relaxed">
                AI-assisted preliminary screening.<br />
                Not a replacement for professional inspection.
              </p>
            </div>
          </div>

          {/* Preview card */}
          <div className="mt-14 max-w-[400px] rounded-xl border border-white/6 bg-surface p-5 space-y-4">
            <div className="inline-flex items-center rounded-sm border border-warn/30 bg-warn-tint px-3 py-1 text-sm font-semibold text-warn">
              Buy with caution
            </div>
            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wider text-dim">
                Main concerns
              </p>
              <ul className="space-y-1.5">
                {[
                  "Missing service history",
                  "Imported vehicle history unclear",
                  "Seller description lacks details",
                ].map((item) => (
                  <li key={item} className="flex items-start gap-2 text-sm text-mid">
                    <span className="mt-[7px] h-1.5 w-1.5 shrink-0 rounded-full bg-warn" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
            <div className="space-y-1.5 border-t border-white/6 pt-4">
              <p className="text-xs font-semibold uppercase tracking-wider text-dim">
                Recommended next step
              </p>
              <p className="text-sm text-mid">
                Ask for VIN, invoices, and accident history before inspection.
              </p>
            </div>
          </div>
        </section>

        {/* How it works */}
        <section id="how-it-works" className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-10 text-[22px] font-[650] text-hi">How it works</h2>
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
              {HOW_IT_WORKS.map(({ step, title, desc }) => (
                <div
                  key={step}
                  className="rounded-xl border border-white/6 bg-surface p-6 space-y-3"
                >
                  <span className="text-xs font-semibold text-dim">{step}</span>
                  <p className="text-sm font-semibold text-hi">{title}</p>
                  <p className="text-sm text-dim leading-relaxed">{desc}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* What AutoVerdict checks */}
        <section className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-8 text-[22px] font-[650] text-hi">What AutoVerdict checks</h2>
            <div className="grid grid-cols-1 gap-x-12 gap-y-3 sm:grid-cols-2">
              {CHECKS_LIST.map((item) => (
                <div key={item} className="flex items-center gap-2.5 text-sm text-mid">
                  <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-brand" />
                  {item}
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
                Built for cautious used-car buyers.
              </h2>
              <p className="text-sm text-mid leading-relaxed">
                AutoVerdict is designed for private buyers who are not car experts and want a
                structured second opinion before calling the seller, arranging inspection, or
                spending money on a risky listing.
              </p>
            </div>
          </div>
        </section>

        {/* Safety / Disclaimer */}
        <section className="border-t border-white/6 bg-surface py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8">
            <h2 className="mb-3 text-[18px] font-[650] text-hi">
              A screening tool, not a guarantee.
            </h2>
            <p className="max-w-[600px] text-sm text-dim leading-relaxed">
              AutoVerdict helps you identify questions and risks. It does not replace professional
              diagnostics, legal verification, official vehicle history reports, or an independent
              inspection.
            </p>
          </div>
        </section>

        {/* SEO content */}
        <section className="border-t border-white/6 py-20">
          <div className="mx-auto max-w-[1120px] px-5 lg:px-8 space-y-8">
            <h2 className="text-[18px] font-[650] text-hi">
              AI used-car listing analysis for buyers in Poland
            </h2>
            <p className="max-w-[680px] text-sm text-dim leading-relaxed">
              Buying a used car in Poland often means comparing listings, checking seller claims,
              reviewing service history, and deciding whether a car is worth inspecting. AutoVerdict
              helps buyers analyze Otomoto listings and other seller-provided information by
              highlighting possible risks, missing details, practical seller questions, and
              inspection points.
            </p>
            <div className="space-y-6">
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  Why analyze a used-car listing before contacting the seller?
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  Contacting a seller about a problematic listing wastes time for both parties. A
                  quick AI screening helps you filter out obvious risks before investing time in
                  phone calls or viewings.
                </p>
              </div>
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  What information should you check before buying a used car?
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  Service history, accident records, mileage consistency, import documentation,
                  seller reputation, and model-specific known issues are all important signals when
                  evaluating a used-car listing.
                </p>
              </div>
              <div>
                <h3 className="mb-2 text-sm font-semibold text-hi">
                  How AutoVerdict helps with Otomoto listings
                </h3>
                <p className="max-w-[640px] text-sm text-dim leading-relaxed">
                  Paste an Otomoto listing URL or copy the listing text. AutoVerdict reads the
                  content, identifies gaps, flags suspicious patterns, and gives you practical
                  questions to ask before deciding whether to arrange an inspection.
                </p>
              </div>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-white/6 py-8">
        <div className="mx-auto max-w-[1120px] px-5 lg:px-8 flex flex-wrap items-center justify-between gap-4">
          <span className="text-sm font-[700] text-hi">AutoVerdict</span>
          <div className="flex gap-6 text-xs text-dim">
            <span>Privacy</span>
            <span>Terms</span>
            <span>Contact</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
