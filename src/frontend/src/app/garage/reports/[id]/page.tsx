"use client";

import { lazy, Suspense, useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { api, type CarCheckResponse } from "@/lib/api";
import { StatusBadge } from "@/components/StatusBadge";
import { cn } from "@/lib/utils";

const MDPreview = lazy(() =>
  import("@uiw/react-md-editor").then((m) => ({ default: m.default.Markdown }))
);

type Verdict = "buy" | "caution" | "avoid";

function parseVerdict(report: string): Verdict | null {
  const match = report.match(/##\s+Recommendation[\s\S]*?(?=\n##|$)/i);
  if (!match) return null;
  const s = match[0].toLowerCase();
  if (s.includes("buy with caution")) return "caution";
  if (s.includes("avoid")) return "avoid";
  if (s.includes("buy")) return "buy";
  return null;
}

function extractVerdictSummary(report: string): string {
  const match = report.match(/##\s+Recommendation([\s\S]*?)(?=\n##|$)/i);
  if (!match) return "";
  return match[1]
    .replace(/\*\*(.*?)\*\*/g, "$1")
    .replace(/\*(.*?)\*/g, "$1")
    .replace(/^[\s—–-]+/, "")
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
    <div className="mx-auto max-w-[760px] space-y-6">
      <Link
        to="/garage/reports"
        className="inline-flex items-center gap-1.5 text-sm text-dim transition-colors hover:text-mid"
      >
        <ArrowLeft className="h-3.5 w-3.5" />
        Back to reports
      </Link>

      {loading ? (
        <div className="rounded-xl border border-white/6 bg-surface p-6">
          <p className="text-sm text-dim">Loading report…</p>
        </div>
      ) : !check ? (
        <div className="rounded-xl border border-white/6 bg-surface p-6">
          <p className="text-sm text-bad">Report not found.</p>
        </div>
      ) : (
        <>
          <div className="rounded-xl border border-white/6 bg-surface px-6 py-5 space-y-2">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-base font-semibold text-hi">
                {check.title ?? check.listingUrl ?? "Listing analysis"}
              </h1>
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
                  >
                    View listing
                  </a>
                </>
              )}
            </p>
          </div>

          <div className="rounded-xl border border-white/6 bg-surface p-6 space-y-5">
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
                {verdict && (
                  <VerdictCard verdict={verdict} summary={verdictSummary} />
                )}
                <div className="av-report wmde-markdown-var" data-color-mode="dark">
                  <Suspense fallback={<p className="text-sm text-dim">Loading report…</p>}>
                    <MDPreview source={check.report} />
                  </Suspense>
                </div>
                <p className="border-t border-white/6 pt-4 text-xs text-dim leading-relaxed">
                  AutoVerdict provides AI-assisted preliminary screening only. It does not replace
                  professional inspection, vehicle history verification, legal checks, or independent
                  expert advice.
                </p>
              </>
            ) : null}
          </div>
        </>
      )}
    </div>
  );
}
