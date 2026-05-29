"use client";

import { ArrowRight } from "lucide-react";
import { cn } from "@/lib/utils";
import type { CarCheckResponse } from "@/lib/api";
import { StatusBadge } from "./StatusBadge";

interface Props {
  checks: CarCheckResponse[];
  page: number;
  hasNextPage: boolean;
  selectedCheckId: string | null;
  onSelectCheck: (id: string) => void;
  onPageChange: (page: number) => void;
}

function formatTitle(check: CarCheckResponse): string {
  return check.title ?? check.listingUrl ?? "Listing analysis";
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
  });
}

export function AnalysisHistory({
  checks,
  page,
  hasNextPage,
  selectedCheckId,
  onSelectCheck,
  onPageChange,
}: Props) {
  const showPager = page > 1 || hasNextPage;

  return (
    <section>
      <h2 className="mb-3 text-xs font-semibold uppercase tracking-wider text-dim">
        Recent analyses
      </h2>

      {checks.length === 0 ? (
        <div className="rounded-lg border border-white/6 bg-surface px-5 py-8 text-center space-y-1">
          <p className="text-sm text-mid">No analyses yet.</p>
          <p className="text-xs text-dim">Your completed checks will appear here.</p>
        </div>
      ) : (
        <>
          <ul className="space-y-2">
            {checks.map((c) => {
              const isActive = c.status === "Pending" || c.status === "Processing";

              if (isActive) {
                return (
                  <li key={c.checkId}>
                    <div className="relative w-full overflow-hidden rounded-xl border border-info/15 bg-info-tint/60 px-6 py-5">
                      <div className="flex items-start justify-between gap-4">
                        <div className="min-w-0 space-y-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <StatusBadge status={c.status} />
                            <span className="text-xs text-dim">
                              Created {formatDate(c.createdAt)}
                            </span>
                          </div>
                          <p className="truncate text-sm font-[650] text-hi/75">
                            {formatTitle(c)}
                          </p>
                          <p className="line-clamp-2 text-xs leading-5 text-dim">
                            AutoVerdict is preparing the buyer report.
                          </p>
                        </div>
                        <span className="mt-1 shrink-0 text-xs font-medium text-info/60">
                          Processing…
                        </span>
                      </div>
                      {/* Animated bottom progress bar */}
                      <div className="absolute bottom-0 left-0 h-[2px] w-full overflow-hidden bg-info/10">
                        <div className="animate-progress-slide absolute h-full w-[45%] bg-gradient-to-r from-transparent via-info to-transparent" />
                      </div>
                    </div>
                  </li>
                );
              }

              return (
                <li key={c.checkId}>
                  <button
                    onClick={() => onSelectCheck(c.checkId)}
                    className={cn(
                      "group w-full rounded-xl border px-6 py-5 text-left",
                      "transition-[transform,background-color,border-color] duration-150",
                      c.checkId === selectedCheckId
                        ? "border-brand/40 bg-brand-tint"
                        : "border-white/6 bg-surface hover:-translate-y-px hover:bg-surface-raised"
                    )}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0 space-y-2">
                        <div className="flex flex-wrap items-center gap-2">
                          <StatusBadge status={c.status} />
                          <span className="text-xs text-dim">
                            Created {formatDate(c.createdAt)}
                          </span>
                        </div>
                        <p className="truncate text-sm font-[650] text-hi">
                          {formatTitle(c)}
                        </p>
                        <p className="line-clamp-2 text-xs leading-5 text-dim">
                          {c.status === "Completed"
                            ? "Report ready. Open the buyer memo for verdict, risks, seller questions, checklist, and estimated costs."
                            : c.failureReason ?? "The analysis could not be completed."}
                        </p>
                      </div>
                      <span className="mt-1 inline-flex items-center gap-1 text-xs font-medium text-brand opacity-80 transition-opacity group-hover:opacity-100">
                        View
                        <ArrowRight className="h-3.5 w-3.5" />
                      </span>
                    </div>
                  </button>
                </li>
              );
            })}
          </ul>

          {showPager && (
            <div className="mt-4 flex items-center justify-between">
              <button
                onClick={() => onPageChange(page - 1)}
                disabled={page === 1}
                className="rounded-md border border-white/6 px-3 py-1.5 text-sm text-dim transition-colors hover:text-hi disabled:cursor-not-allowed disabled:opacity-40"
              >
                Previous
              </button>
              <span className="text-xs text-dim">
                Page {page}{!hasNextPage && " (last)"}
              </span>
              <button
                onClick={() => onPageChange(page + 1)}
                disabled={!hasNextPage}
                className="rounded-md border border-white/6 px-3 py-1.5 text-sm text-dim transition-colors hover:text-hi disabled:cursor-not-allowed disabled:opacity-40"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
