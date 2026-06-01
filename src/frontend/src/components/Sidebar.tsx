"use client";

import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Car, FileText } from "lucide-react";
import { cn } from "@/lib/utils";
import { removeToken } from "@/lib/auth";
import type { MeResponse } from "@/lib/api";
import { PurchaseCreditsModal } from "@/components/PurchaseCreditsModal";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";

const NAV = [
  { labelKey: "nav.checkCar", href: "/garage/check", icon: Car },
  { labelKey: "nav.myReports", href: "/garage/reports", icon: FileText },
];

interface Props {
  me: MeResponse | null;
}

export function Sidebar({ me }: Props) {
  const { t } = useTranslation();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [showModal, setShowModal] = useState(false);

  function signOut() {
    removeToken();
    navigate("/");
  }

  return (
    <>
    <aside className="hidden lg:flex lg:flex-col w-[260px] shrink-0 min-h-screen border-r border-white/6 bg-[#0E1116]">
      <div className="flex flex-col h-full px-6 py-6">
        <div className="mb-8 flex items-center justify-between gap-3">
          <span className="text-[15px] font-[700] text-hi tracking-tight">{t("app.name")}</span>
          <LanguageSwitcher />
        </div>

        <nav className="flex-1 space-y-0.5" aria-label={t("nav.garageNavigation")}>
          {NAV.map(({ labelKey, href, icon: Icon }) => {
            const active = pathname.startsWith(href);
            return (
              <Link
                key={href}
                to={href}
                className={cn(
                  "flex items-center gap-2.5 h-[42px] rounded-xl px-3 text-sm font-medium transition-colors",
                  active
                    ? "bg-surface-raised text-hi"
                    : "text-mid hover:bg-white/[0.04] hover:text-hi"
                )}
                aria-current={active ? "page" : undefined}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {t(labelKey)}
              </Link>
            );
          })}
        </nav>

        <div className="mt-auto space-y-3 border-t border-white/6 pt-4">
          {me !== null && (
            <div className="flex items-center gap-2">
              <span
                className={cn(
                  "inline-flex rounded-full px-3 py-1 text-xs font-semibold",
                  me.credits === 0
                    ? "bg-surface-raised text-off"
                    : "bg-warn-tint text-warn"
                )}
              >
                {t("credits.available", { count: me.credits })}
              </span>
              <button
                onClick={() => setShowModal(true)}
                className="text-xs text-dim underline underline-offset-2 transition-colors hover:text-hi"
              >
                {t("credits.topUp")}
              </button>
            </div>
          )}
          {me && <p className="truncate text-xs text-dim">{me.email}</p>}
          <button
            onClick={signOut}
            className="text-xs text-dim transition-colors hover:text-mid"
          >
            {t("auth.signOut")}
          </button>
        </div>
      </div>
    </aside>

    {showModal && <PurchaseCreditsModal onClose={() => setShowModal(false)} />}
    </>
  );
}
