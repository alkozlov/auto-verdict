"use client";

import { useState } from "react";
import { ArrowRight, Download, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { api, type CarCheckResponse } from "@/lib/api";
import { StatusBadge } from "./StatusBadge";
import { useTranslation } from "react-i18next";

interface Props {
  checks: CarCheckResponse[];
  page: number;
  hasNextPage: boolean;
  selectedCheckId: string | null;
  onSelectCheck: (id: string) => void;
  onPageChange: (page: number) => void;
}

function formatTitle(check: CarCheckResponse, fallback: string): string {
  return check.title ?? check.listingUrl ?? fallback;
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
  const { t } = useTranslation();
  const [downloading, setDownloading] = useState<Set<string>>(new Set());
  const showPager = page > 1 || hasNextPage;

  async function handleDownload(e: React.MouseEvent, check: CarCheckResponse) {
    e.stopPropagation();
    if (downloading.has(check.checkId)) return;
    setDownloading((prev) => new Set([...prev, check.checkId]));
    try {
      const title = check.title ?? check.listingUrl ?? "report";
      const safe = title.replace(/[^a-zA-Z0-9\s-]/g, "").trim().replace(/\s+/g, "-").slice(0, 60);
      await api.checks.downloadPdf(check.checkId, `autoverdict-${safe || "report"}.pdf`);
    } finally {
      setDownloading((prev) => {
        const next = new Set(prev);
        next.delete(check.checkId);
        return next;
      });
    }
  }

  return (
    <section>
      <h2 className="mb-3 text-xs font-semibold uppercase tracking-wider text-dim">
        {t("garage.history.sectionTitle")}
      </h2>

      {checks.length === 0 ? (
        <div className="rounded-lg border border-white/6 bg-surface px-5 py-8 text-center space-y-1">
          <p className="text-sm text-mid">{t("garage.history.empty")}</p>
          <p className="text-xs text-dim">{t("garage.history.emptyHint")}</p>
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
                              {t("garage.history.created", { date: formatDate(c.createdAt) })}
                            </span>
                          </div>
                          <p className="truncate text-sm font-[650] text-hi/75">
                            {formatTitle(c, t("garage.report.fallbackTitle"))}
                          </p>
                          <p className="line-clamp-2 text-xs leading-5 text-dim">
                            {t("garage.history.preparingReport")}
                          </p>
                        </div>
                        <span className="mt-1 shrink-0 text-xs font-medium text-info/60">
                          {t("garage.history.processing")}
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
                            {t("garage.history.created", { date: formatDate(c.createdAt) })}
                          </span>
                        </div>
                        <p className="truncate text-sm font-[650] text-hi">
                          {formatTitle(c, t("garage.report.fallbackTitle"))}
                        </p>
                        <p className="line-clamp-2 text-xs leading-5 text-dim">
                          {c.status === "Completed"
                            ? t("garage.history.reportReady")
                            : c.failureReason ?? t("garage.history.analysisFailed")}
                        </p>
                      </div>
                      <div className="mt-1 flex shrink-0 items-center gap-2">
                        {c.status === "Completed" && (
                          <button
                            onClick={(e) => handleDownload(e, c)}
                            disabled={downloading.has(c.checkId)}
                            className="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-3 py-1.5 text-xs font-medium text-dim transition-colors hover:border-white/20 hover:text-hi disabled:cursor-not-allowed disabled:opacity-40"
                            title="Download PDF"
                          >
                            {downloading.has(c.checkId) ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <Download className="h-3.5 w-3.5" />
                            )}
                            PDF
                          </button>
                        )}
                        <span className="inline-flex items-center gap-1 text-xs font-medium text-brand opacity-80 transition-opacity group-hover:opacity-100">
                          {t("garage.history.view")}
                          <ArrowRight className="h-3.5 w-3.5" />
                        </span>
                      </div>
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
                {t("garage.history.previous")}
              </button>
              <span className="text-xs text-dim">
                {hasNextPage
                  ? t("garage.history.page", { page })
                  : t("garage.history.pageLast", { page })}
              </span>
              <button
                onClick={() => onPageChange(page + 1)}
                disabled={!hasNextPage}
                className="rounded-md border border-white/6 px-3 py-1.5 text-sm text-dim transition-colors hover:text-hi disabled:cursor-not-allowed disabled:opacity-40"
              >
                {t("garage.history.next")}
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
