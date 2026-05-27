"use client";

import { cn } from "@/lib/utils";
import type { MeResponse } from "@/lib/api";

interface Props {
  me: MeResponse | null;
  onSignOut: () => void;
}

export function Header({ me, onSignOut }: Props) {
  const credits = me?.credits ?? 0;
  const lowCredits = credits === 0;

  return (
    <header
      className="sticky top-0 z-30 border-b border-white/6 bg-page/90 backdrop-blur-sm"
      style={{ height: 64 }}
    >
      <div className="mx-auto flex h-full max-w-[960px] items-center justify-between px-4 md:px-6 lg:px-8">
        <span className="text-sm font-semibold tracking-tight text-hi">AutoVerdict</span>
        <div className="flex items-center gap-4">
          {me?.email && (
            <span className="hidden max-w-[200px] truncate text-sm text-dim opacity-70 sm:block">
              {me.email}
            </span>
          )}
          <span
            className={cn(
              "rounded-full px-3.5 py-2 text-[13px] font-semibold",
              lowCredits
                ? "bg-warn-tint text-warn"
                : "border border-white/10 bg-surface text-mid"
            )}
          >
            Credits: {credits}
          </span>
          <button
            onClick={onSignOut}
            className="text-sm text-dim opacity-70 transition-opacity hover:opacity-100"
          >
            Sign out
          </button>
        </div>
      </div>
    </header>
  );
}
