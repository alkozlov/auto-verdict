"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
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

export function Sidebar({ me }: Props) {
  const pathname = usePathname();
  const router = useRouter();

  function signOut() {
    removeToken();
    router.push("/");
  }

  return (
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
                href={href}
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
            <div>
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
  );
}
