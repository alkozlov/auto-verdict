"use client";

import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, ExternalLink } from "lucide-react";
import { api, type CarCheckResponse } from "@/lib/api";
import { StatusBadge } from "@/components/StatusBadge";
import { ReportMarkdownViewer } from "@/components/ReportMarkdownViewer";
import { cn } from "@/lib/utils";

type Verdict = "buy" | "caution" | "avoid";

function parseVerdict(report: string): Verdict | null {
  const firstSection = report.split(/\n#\s+/)[0].toLowerCase();
  const s = firstSection || report.slice(0, 1000).toLowerCase();
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

const VERDICT_STYLES: Record<Verdict, { card: string; badge: string; label: string }> = {
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

function VerdictBadge({ verdict }: { verdict: Verdict }) {
  const s = VERDICT_STYLES[verdict];
  return (
    <span className={cn("inline-flex items-center rounded-full px-3 py-1 text-sm font-semibold", s.badge)}>
      {s.label}
    </span>
  );
}

export default function ReportPage() {
  const { id } = useParams<{ id: string }>();
  const [check, setCheck] = useState<CarCheckResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    api.checks
      .get(id)
      .then(setCheck)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!check) return;
    if (check.status !== "Pending" && check.status !== "Processing") return;
    const pollId = setInterval(() => {
      api.checks.get(id!).then(setCheck).catch(() => {});
    }, 3000);
    return () => clearInterval(pollId);
  }, [id, check?.status]);

  const verdict = check?.report ? parseVerdict(check.report) : null;
  const verdictSummary = check?.report ? extractVerdictSummary(check.report) : "";

  return (
    <div className="mx-auto max-w-[960px] space-y-6 pb-16">
      <Link
        to="/garage/reports"
        className="inline-flex items-center gap-1.5 text-sm text-dim transition-colors hover:text-mid"
      >
        <ArrowLeft className="h-3.5 w-3.5" />
        Back to reports
      </Link>

      {loading ? (
        <div className="rounded-xl border border-white/8 bg-surface p-6">
          <p className="text-sm text-dim">Loading report…</p>
        </div>
      ) : !check ? (
        <div className="rounded-xl border border-white/8 bg-surface p-6">
          <p className="text-sm text-bad">Report not found.</p>
        </div>
      ) : (
        <>
          <div className="rounded-2xl border border-white/8 bg-gradient-to-b from-[#111823] to-[#0D121A] px-6 py-5 shadow-[0_18px_60px_rgba(0,0,0,0.25)] sm:px-7">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div className="min-w-0 space-y-2">
                <div className="flex flex-wrap items-center gap-2">
                  {verdict && <VerdictBadge verdict={verdict} />}
                  <StatusBadge status={check.status} />
                </div>
                <h1 className="text-xl font-[720] leading-tight text-hi sm:text-2xl">
                  {check.title ?? check.listingUrl ?? "Listing analysis"}
                </h1>
                {verdictSummary && (
                  <p className="max-w-3xl text-sm leading-6 text-mid">{verdictSummary}</p>
                )}
              </div>
              <div className="shrink-0 text-left sm:text-right">
                <p className="text-xs uppercase tracking-wide text-off">Created</p>
                <p className="mt-1 text-sm text-mid">
                  {new Date(check.createdAt).toLocaleString()}
                </p>
              </div>
            </div>
            {check.listingUrl && (
              <a
                href={check.listingUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="mt-5 inline-flex max-w-full items-center gap-2 rounded-lg border border-white/8 bg-white/[0.03] px-3 py-2 text-sm text-brand transition-colors hover:text-brand-hi"
              >
                <ExternalLink className="h-4 w-4 shrink-0" />
                <span className="truncate">{check.listingUrl}</span>
              </a>
            )}
          </div>

          <div className="rounded-[18px] border border-white/8 bg-gradient-to-b from-[#111823] to-[#0D121A] p-5 shadow-[0_24px_80px_rgba(0,0,0,0.35)] sm:rounded-3xl sm:p-10">
            {check.status === "Pending" || check.status === "Processing" ? (
              <div className="space-y-2">
                <p className="text-sm font-medium text-mid">Analysis in progress</p>
                <p className="text-sm text-dim">
                  AutoVerdict is still generating this report.
                </p>
              </div>
            ) : check.status === "Failed" ? (
              <div className="space-y-1.5">
                <p className="text-sm font-medium text-bad">Analysis failed</p>
                <p className="text-sm text-mid">
                  {check.failureReason ?? "We couldn't complete this report."}
                </p>
              </div>
            ) : check.report ? (
              <>
                <ReportMarkdownViewer markdown={check.report} />
                <p className="border-t border-white/6 pt-4 text-xs text-dim leading-relaxed">
                  AutoVerdict provides AI-assisted preliminary screening only. It does not replace
                  professional inspection, vehicle history verification, legal checks, or independent
                  expert advice.
                </p>
              </>
            ) : null}
          </div>

          <div className="flex flex-wrap gap-3">
            <Link
              to="/garage/check"
              className="rounded-lg bg-brand px-5 py-2.5 text-sm font-semibold text-page transition-all hover:brightness-105"
            >
              Check another car
            </Link>
            <Link
              to="/garage/reports"
              className="rounded-lg border border-white/8 px-5 py-2.5 text-sm text-dim transition-colors hover:text-hi"
            >
              Back to reports
            </Link>
          </div>
        </>
      )}
    </div>
  );
}
