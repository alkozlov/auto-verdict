"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Menu, X } from "lucide-react";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { TestModeBanner } from "@/components/TestModeBanner";
import { getToken } from "@/lib/auth";
import { cn } from "@/lib/utils";

interface Props {
  children: ReactNode;
}

const NAV = [
  { href: "/how-it-works", key: "public.nav.how" },
  { href: "/sample-report", key: "public.nav.sample" },
  { href: "/pricing", key: "public.nav.pricing" },
];

export function PublicLayout({ children }: Props) {
  const { t } = useTranslation();
  const [loggedIn, setLoggedIn] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    setLoggedIn(!!getToken());
  }, []);

  const cta = loggedIn ? (
    <Link to="/garage/check" className="av-btn-primary">
      {t("public.cta.goAnalyses")}
    </Link>
  ) : (
    <a href="/api/auth/google" className="av-btn-primary">
      {t("public.cta.startFree")}
    </a>
  );

  return (
    <div className="min-h-screen bg-[#070A0F] text-[#F8FAFC]">
      <TestModeBanner />
      <header className="sticky top-0 z-40 border-b border-slate-400/10 bg-[#070A0F]/80 backdrop-blur-[14px]">
        <div className="mx-auto flex h-16 max-w-[1200px] items-center justify-between px-5 lg:px-8">
          <Link to="/" className="text-lg font-extrabold text-white">
            {t("app.name")}
          </Link>
          <nav className="hidden items-center gap-7 md:flex" aria-label={t("public.nav.label")}>
            {NAV.map((item) => (
              <Link key={item.href} to={item.href} className="text-sm text-slate-400 hover:text-white">
                {t(item.key)}
              </Link>
            ))}
          </nav>
          <div className="hidden items-center gap-3 md:flex">
            <LanguageSwitcher />
            {cta}
          </div>
          <div className="flex items-center gap-2 md:hidden">
            <LanguageSwitcher />
            <button
              type="button"
              onClick={() => setOpen((value) => !value)}
              aria-label={open ? t("nav.closeMenu") : t("nav.openMenu")}
              className="flex h-9 w-9 items-center justify-center rounded-md border border-slate-400/15 text-slate-300"
            >
              {open ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
            </button>
          </div>
        </div>
        {open && (
          <div className="border-t border-slate-400/10 px-5 py-4 md:hidden">
            <nav className="flex flex-col gap-3" aria-label={t("public.nav.label")}>
              {NAV.map((item) => (
                <Link
                  key={item.href}
                  to={item.href}
                  onClick={() => setOpen(false)}
                  className="text-sm text-slate-300"
                >
                  {t(item.key)}
                </Link>
              ))}
              <div className="pt-2">{cta}</div>
            </nav>
          </div>
        )}
      </header>
      {children}
      <footer className="border-t border-slate-400/10 bg-[#0B1017]">
        <div className="mx-auto grid max-w-[1200px] gap-8 px-5 py-10 md:grid-cols-[1.4fr_1fr] lg:px-8">
          <div>
            <Link to="/" className="text-base font-extrabold text-white">
              {t("app.name")}
            </Link>
            <p className="mt-3 max-w-md text-sm leading-6 text-slate-400">
              {t("public.footer.description")}
            </p>
          </div>
          <nav className="flex flex-wrap gap-x-6 gap-y-3 text-sm text-slate-400" aria-label={t("public.footer.label")}>
            {[...NAV, { href: "/privacy", key: "home.footer.privacy" }, { href: "/terms", key: "home.footer.terms" }, { href: "/contact", key: "home.footer.contact" }].map((item) => (
              <Link key={item.href} to={item.href} className="hover:text-white">
                {t(item.key)}
              </Link>
            ))}
          </nav>
        </div>
      </footer>
    </div>
  );
}

export function Section({
  children,
  className,
  id,
}: {
  children: ReactNode;
  className?: string;
  id?: string;
}) {
  return (
    <section id={id} className={cn("border-t border-slate-400/10 py-12 md:py-20", className)}>
      <div className="mx-auto max-w-[1200px] px-5 lg:px-8">{children}</div>
    </section>
  );
}
