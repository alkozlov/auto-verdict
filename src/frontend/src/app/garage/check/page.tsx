"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Check } from "lucide-react";
import { api, type CarCheckResponse } from "@/lib/api";
import { useGarage } from "@/lib/garage-context";
import { AnalysisComposer } from "@/components/AnalysisComposer";
import { ProcessingPanel } from "@/components/ProcessingPanel";
import { ImageLightbox } from "@/components/ImageLightbox";

interface SubmissionState {
  checkId: string;
  hasLink: boolean;
  hasPhotos: boolean;
}

export default function CheckCarPage() {
  const router = useRouter();
  const { me, refreshMe } = useGarage();
  const [submission, setSubmission] = useState<SubmissionState | null>(null);
  const [currentCheck, setCurrentCheck] = useState<CarCheckResponse | null>(null);
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!submission) return;
    if (
      currentCheck?.status === "Completed" ||
      currentCheck?.status === "Failed"
    )
      return;

    function poll() {
      api.checks
        .get(submission!.checkId)
        .then(setCurrentCheck)
        .catch(() => {});
    }
    poll();
    const id = setInterval(poll, 3000);
    return () => clearInterval(id);
  }, [submission, currentCheck?.status]);

  function handleSubmitSuccess(checkId: string, hasLink: boolean, hasPhotos: boolean) {
    setSubmission({ checkId, hasLink, hasPhotos });
    setCurrentCheck(null);
    refreshMe();
  }

  function handleCheckAnother() {
    setSubmission(null);
    setCurrentCheck(null);
  }

  const isProcessing =
    !!submission &&
    (!currentCheck ||
      currentCheck.status === "Pending" ||
      currentCheck.status === "Processing");

  const isCompleted = currentCheck?.status === "Completed";
  const isFailed = currentCheck?.status === "Failed";

  return (
    <div className="mx-auto max-w-[760px] space-y-8">
      <div>
        <h1 className="text-[22px] font-[650] text-hi">Check car</h1>
        <p className="mt-1.5 text-sm text-dim">
          Paste everything you know about the car. AutoVerdict will turn it into a structured risk analysis.
        </p>
      </div>

      {!submission ? (
        <AnalysisComposer
          onSubmitSuccess={handleSubmitSuccess}
          onImagePreview={setLightboxUrl}
        />
      ) : isProcessing ? (
        <ProcessingPanel
          key={submission.checkId}
          hasLink={submission.hasLink}
          hasPhotos={submission.hasPhotos}
        />
      ) : isCompleted ? (
        <div className="animate-panel-in rounded-xl border border-white/6 bg-surface p-7 space-y-5">
          <div className="flex items-center gap-3">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-ok-tint">
              <Check className="h-4 w-4 text-ok" />
            </span>
            <div>
              <p className="text-sm font-semibold text-hi">Analysis complete</p>
              <p className="text-xs text-dim">Your report is ready.</p>
            </div>
          </div>
          <div className="flex flex-wrap gap-3">
            <button
              onClick={() => router.push(`/garage/reports/${submission.checkId}`)}
              className="rounded-lg bg-brand px-5 py-2.5 text-sm font-semibold text-page transition-all hover:brightness-105"
            >
              Open report
            </button>
            <button
              onClick={handleCheckAnother}
              className="rounded-lg border border-white/6 px-5 py-2.5 text-sm text-dim transition-colors hover:text-hi"
            >
              Check another car
            </button>
          </div>
        </div>
      ) : isFailed ? (
        <div className="animate-panel-in rounded-xl border border-bad/20 bg-bad-tint p-7 space-y-3">
          <p className="text-sm font-semibold text-bad">Analysis failed</p>
          <p className="text-sm text-mid">
            {currentCheck?.failureReason ?? "We couldn't complete this check."}
          </p>
          <button
            onClick={handleCheckAnother}
            className="rounded-lg border border-white/6 px-5 py-2.5 text-sm text-dim transition-colors hover:text-hi"
          >
            Check another car
          </button>
        </div>
      ) : null}

      {lightboxUrl && (
        <ImageLightbox url={lightboxUrl} onClose={() => setLightboxUrl(null)} />
      )}
    </div>
  );
}
