"use client";

import { cn } from "@/lib/utils";
import type { CarCheckResponse } from "@/lib/api";

type Status = CarCheckResponse["status"];

const STATUS_COLOR: Record<Status, string> = {
  Pending: "text-warn",
  Processing: "text-info",
  Completed: "text-ok",
  Failed: "text-bad",
};

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
            {checks.map((c) => (
              <li key={c.checkId}>
                <button
                  onClick={() => onSelectCheck(c.checkId)}
                  className={cn(
                    "w-full rounded-lg border px-6 py-5 text-left",
                    "transition-[transform,background-color,border-color] duration-150",
                    c.checkId === selectedCheckId
                      ? "border-brand/40 bg-brand-tint"
                      : "border-white/6 bg-surface hover:-translate-y-px hover:bg-surface-raised"
                  )}
                >
                  <p className="truncate text-sm font-[650] text-hi">
                    {formatTitle(c)}
                  </p>
                  <p className="mt-2 text-xs">
                    <span className={STATUS_COLOR[c.status]}>{c.status}</span>
                    <span className="text-off"> · </span>
                    <span className="text-dim">{formatDate(c.createdAt)}</span>
                  </p>
                </button>
              </li>
            ))}
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
