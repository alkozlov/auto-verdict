"use client";

import { ChevronUp } from "lucide-react";
import { cn } from "@/lib/utils";
import { StatusBadge } from "./StatusBadge";
import { ReportMarkdownViewer } from "./ReportMarkdownViewer";
import type { CarCheckResponse } from "@/lib/api";

type Verdict = "buy" | "caution" | "avoid";

function parseVerdict(report: string): Verdict | null {
  const s = report.slice(0, 1000).toLowerCase();
  if (
    s.includes("buy with caution") ||
    s.includes("kupuj ostrożnie") ||
    s.includes("mit vorsicht kaufen") ||
    s.includes("купувати обережно") ||
    s.includes("acheter avec prudence")
  )
    return "caution";
  if (
    s.includes("avoid") ||
    s.includes("unikaj") ||
    s.includes("vermeiden") ||
    s.includes("уникати") ||
    s.includes("éviter") ||
    s.includes("eviter")
  )
    return "avoid";
  if (
    s.includes("buy") ||
    s.includes("kup") ||
    s.includes("kaufen") ||
    s.includes("купувати") ||
    s.includes("acheter")
  )
    return "buy";
  return null;
}

function extractVerdictSummary(report: string): string {
  const match = report.match(/^#\s+.*?\n([\s\S]*?)(?=\n#{1,3}\s+At a glance|\n#{1,3}\s+|$)/i);
  if (!match) return "";
  return match[1]
    .replace(/\*\*(.*?)\*\*/g, "$1")
    .replace(/\*(.*?)\*/g, "$1")
    .replace(/[🟢🟠🔴⚪]/gu, "")
    .replace(/^[\s-]+/, "")
    .trim()
    .slice(0, 300);
}

const VERDICT_STYLES: Record<
  Verdict,
  { card: string; badge: string; label: string }
> = {
  buy: {
    card: "bg-ok-tint border-ok/20",
    badge: "bg-ok-tint text-ok border border-ok/30",
    label: "Buy",
  },
  caution: {
    card: "bg-warn-tint border-warn/20",
    badge: "bg-warn-tint text-warn border border-warn/30",
    label: "Buy with caution",
  },
  avoid: {
    card: "bg-bad-tint border-bad/20",
    badge: "bg-bad-tint text-bad border border-bad/30",
    label: "Avoid",
  },
};

function VerdictCard({ verdict, summary }: { verdict: Verdict; summary: string }) {
  const s = VERDICT_STYLES[verdict];
  return (
    <div className={cn("rounded-lg border p-5 space-y-2.5", s.card)}>
      <span className={cn("inline-flex items-center rounded-sm px-3 py-1 text-sm font-semibold", s.badge)}>
        {s.label}
      </span>
      {summary && (
        <p className="text-sm text-mid leading-relaxed">{summary}</p>
      )}
    </div>
  );
}

interface Props {
  check: CarCheckResponse | null;
  loading: boolean;
  onClose: () => void;
}

export function InlineReportPanel({ check, loading, onClose }: Props) {
  if (loading) {
    return (
      <div className="rounded-xl border border-white/6 bg-surface p-6">
        <p className="text-sm text-dim">Loading…</p>
      </div>
    );
  }

  if (!check) return null;

  const verdict = check.report ? parseVerdict(check.report) : null;
  const verdictSummary = check.report ? extractVerdictSummary(check.report) : "";

  return (
    <div className="animate-panel-in rounded-xl border border-white/6 bg-surface overflow-hidden">
      {/* Header */}
      <div className="flex items-start justify-between gap-4 border-b border-white/6 px-5 py-4">
        <div className="min-w-0 space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-sm font-semibold text-hi">
              {check.title ?? check.listingUrl ?? "Listing analysis"}
            </h2>
            <StatusBadge status={check.status} />
          </div>
          <p className="text-xs text-dim">
            {new Date(check.createdAt).toLocaleString()}
            {check.listingUrl && (
              <>
                {" · "}
                <a
                  href={check.listingUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-brand hover:text-brand-hi transition-colors"
                  onClick={(e) => e.stopPropagation()}
                >
                  View listing
                </a>
              </>
            )}
          </p>
        </div>
        <button
          onClick={onClose}
          aria-label="Collapse report"
          className="shrink-0 flex items-center gap-1 text-xs text-dim transition-colors hover:text-mid"
        >
          <ChevronUp className="h-4 w-4" />
          <span className="hidden sm:inline">Collapse</span>
        </button>
      </div>

      {/* Content */}
      <div className="p-5 space-y-5">
        {check.status === "Pending" || check.status === "Processing" ? (
          <div className="flex items-center gap-3">
            <StatusBadge status={check.status} />
            <span className="text-sm text-mid">— analysis in progress</span>
          </div>
        ) : check.status === "Failed" ? (
          <div className="space-y-1.5">
            <p className="text-sm font-medium text-bad">Analysis failed</p>
            {check.failureReason && (
              <p className="text-sm text-mid">{check.failureReason}</p>
            )}
          </div>
        ) : check.report ? (
          <>
            {verdict && <VerdictCard verdict={verdict} summary={verdictSummary} />}
            <div className="rounded-xl border border-white/6 bg-[#0D121A] p-5">
              <ReportMarkdownViewer markdown={check.report} />
            </div>
            <p className="border-t border-white/6 pt-4 text-xs text-dim leading-relaxed">
              AutoVerdict provides AI-assisted preliminary screening only. It does not replace
              professional inspection, vehicle history verification, legal checks, or independent
              expert advice.
            </p>
          </>
        ) : null}
      </div>
    </div>
  );
}
