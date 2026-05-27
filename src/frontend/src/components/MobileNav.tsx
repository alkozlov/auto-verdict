"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Menu, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { removeToken } from "@/lib/auth";
import type { MeResponse } from "@/lib/api";

const NAV = [
  { label: "Check car", href: "/garage/check" },
  { label: "My reports", href: "/garage/reports" },
];

interface Props {
  me: MeResponse | null;
}

export function MobileNav({ me }: Props) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open]);

  function signOut() {
    removeToken();
    router.push("/");
  }

  return (
    <>
      <header className="lg:hidden flex items-center justify-between h-14 px-4 border-b border-white/6 bg-[#0E1116] shrink-0">
        <span className="text-[15px] font-[700] text-hi tracking-tight">AutoVerdict</span>
        <div className="flex items-center gap-3">
          {me !== null && (
            <span
              className={cn(
                "inline-flex rounded-full px-3 py-1 text-xs font-semibold",
                me.credits === 0 ? "bg-surface-raised text-off" : "bg-warn-tint text-warn"
              )}
            >
              {me.credits}
            </span>
          )}
          <button
            onClick={() => setOpen(true)}
            aria-label="Open menu"
            aria-expanded={open}
            className="flex h-8 w-8 items-center justify-center rounded-md text-dim hover:text-hi transition-colors"
          >
            <Menu className="h-5 w-5" />
          </button>
        </div>
      </header>

      {open && (
        <div
          className="lg:hidden fixed inset-0 z-40 bg-black/60"
          onClick={() => setOpen(false)}
          aria-hidden="true"
        />
      )}

      <div
        className={cn(
          "lg:hidden fixed inset-y-0 left-0 z-50 w-72 flex flex-col px-6 py-6",
          "bg-[#0E1116] border-r border-white/6",
          "transition-transform duration-200",
          open ? "translate-x-0" : "-translate-x-full"
        )}
        role="dialog"
        aria-modal="true"
        aria-label="Navigation menu"
      >
        <div className="flex items-center justify-between mb-8">
          <span className="text-[15px] font-[700] text-hi tracking-tight">AutoVerdict</span>
          <button
            onClick={() => setOpen(false)}
            aria-label="Close menu"
            className="flex h-8 w-8 items-center justify-center text-dim hover:text-hi transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav className="flex-1 space-y-0.5" aria-label="Garage navigation">
          {NAV.map(({ label, href }) => {
            const active = pathname.startsWith(href);
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  "flex items-center h-[42px] rounded-xl px-3 text-sm font-medium transition-colors",
                  active ? "bg-surface-raised text-hi" : "text-mid hover:bg-white/[0.04] hover:text-hi"
                )}
                aria-current={active ? "page" : undefined}
              >
                {label}
              </Link>
            );
          })}
        </nav>

        <div className="mt-auto space-y-3 border-t border-white/6 pt-4">
          {me && <p className="truncate text-xs text-dim">{me.email}</p>}
          <button
            onClick={signOut}
            className="text-xs text-dim transition-colors hover:text-mid"
          >
            Sign out
          </button>
        </div>
      </div>
    </>
  );
}
