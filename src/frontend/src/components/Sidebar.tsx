"use client";

import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { cn } from "@/lib/utils";
import { removeToken } from "@/lib/auth";
import type { MeResponse } from "@/lib/api";
import { PurchaseCreditsModal } from "@/components/PurchaseCreditsModal";

const NAV = [
  { label: "Check car", href: "/garage/check" },
  { label: "My reports", href: "/garage/reports" },
];

interface Props {
  me: MeResponse | null;
}

export function Sidebar({ me }: Props) {
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
        <div className="mb-8">
          <span className="text-[15px] font-[700] text-hi tracking-tight">AutoVerdict</span>
        </div>

        <nav className="flex-1 space-y-0.5" aria-label="Garage navigation">
          {NAV.map(({ label, href }) => {
            const active = pathname.startsWith(href);
            return (
              <Link
                key={href}
                to={href}
                className={cn(
                  "flex items-center h-[42px] rounded-xl px-3 text-sm font-medium transition-colors",
                  active
                    ? "bg-surface-raised text-hi"
                    : "text-mid hover:bg-white/[0.04] hover:text-hi"
                )}
                aria-current={active ? "page" : undefined}
              >
                {label}
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
                {me.credits} credit{me.credits !== 1 ? "s" : ""}
              </span>
              <button
                onClick={() => setShowModal(true)}
                className="text-xs text-dim underline underline-offset-2 transition-colors hover:text-hi"
              >
                Top up
              </button>
            </div>
          )}
          {me && <p className="truncate text-xs text-dim">{me.email}</p>}
          <button
            onClick={signOut}
            className="text-xs text-dim transition-colors hover:text-mid"
          >
            Sign out
          </button>
        </div>
      </div>
    </aside>

    {showModal && <PurchaseCreditsModal onClose={() => setShowModal(false)} />}
    </>
  );
}
