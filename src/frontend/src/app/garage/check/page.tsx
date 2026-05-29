"use client";

import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Check, CreditCard } from "lucide-react";
import { api, type CarCheckResponse } from "@/lib/api";
import { useGarage } from "@/lib/garage-context";
import { AnalysisComposer } from "@/components/AnalysisComposer";
import { ImageLightbox } from "@/components/ImageLightbox";
import { ProcessingBar } from "@/components/ProcessingBar";

export default function CheckCarPage() {
  const navigate = useNavigate();
  const { refreshMe } = useGarage();
  const [activeCheck, setActiveCheck] = useState<CarCheckResponse | null>(null);
  const [completedCheck, setCompletedCheck] = useState<CarCheckResponse | null>(null);
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);
  const [paymentSuccess, setPaymentSuccess] = useState(false);
  const [initializing, setInitializing] = useState(true);

  // On mount, detect any existing active check so the form is locked if needed
  useEffect(() => {
    api.checks
      .list(1, 1)
      .then((checks) => {
        const first = checks[0];
        if (first && (first.status === "Pending" || first.status === "Processing")) {
          setActiveCheck(first);
        }
      })
      .catch(() => {})
      .finally(() => setInitializing(false));
  }, []);

  // Poll the active check every 3 s until it settles
  useEffect(() => {
    if (!activeCheck) return;
    if (activeCheck.status === "Completed" || activeCheck.status === "Failed") {
      setCompletedCheck(activeCheck);
      setActiveCheck(null);
      return;
    }

    function poll() {
      api.checks
        .get(activeCheck!.checkId)
        .then((check) => {
          if (check.status === "Completed" || check.status === "Failed") {
            setCompletedCheck(check);
            setActiveCheck(null);
          } else {
            setActiveCheck(check);
          }
        })
        .catch(() => {});
    }
    poll();
    const id = setInterval(poll, 3000);
    return () => clearInterval(id);
  }, [activeCheck?.checkId, activeCheck?.status]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get("payment") === "success") {
      refreshMe();
      setPaymentSuccess(true);
      const url = new URL(window.location.href);
      url.searchParams.delete("payment");
      window.history.replaceState({}, "", url.toString());
    }
  }, [refreshMe]);

  function handleSubmitSuccess(check: CarCheckResponse) {
    setActiveCheck(check);
    setCompletedCheck(null);
    refreshMe();
  }

  function handleDismissCompleted() {
    setCompletedCheck(null);
  }

  const isProcessing = !!activeCheck;
  const isCompleted = completedCheck?.status === "Completed";
  const isFailed = completedCheck?.status === "Failed";

  return (
    <div className="mx-auto max-w-[760px] space-y-8">
      {isProcessing && <ProcessingBar />}

      <div>
        <h1 className="text-[22px] font-[650] text-hi">Check car</h1>
        <p className="mt-1.5 text-sm text-dim">
          Paste everything you know about the car. AutoVerdict will turn it into a structured risk analysis.
        </p>
      </div>

      {paymentSuccess && (
        <div className="flex items-center gap-3 rounded-xl border border-ok/20 bg-ok-tint px-4 py-3">
          <CreditCard className="h-4 w-4 shrink-0 text-ok" />
          <p className="text-sm text-ok">Credits added to your account.</p>
          <button
            onClick={() => setPaymentSuccess(false)}
            className="ml-auto text-xs text-ok/70 hover:text-ok transition-colors"
          >
            Dismiss
          </button>
        </div>
      )}

      {isCompleted && (
        <div className="animate-panel-in rounded-xl border border-ok/20 bg-ok-tint p-7 space-y-5">
          <div className="flex items-center gap-3">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-ok/15">
              <Check className="h-4 w-4 text-ok" />
            </span>
            <div>
              <p className="text-sm font-semibold text-hi">Analysis complete</p>
              <p className="text-xs text-dim">Your report is ready.</p>
            </div>
          </div>
          <div className="flex flex-wrap gap-3">
            <button
              onClick={() => navigate(`/garage/reports/${completedCheck!.checkId}`)}
              className="rounded-lg bg-brand px-5 py-2.5 text-sm font-semibold text-page transition-all hover:brightness-105"
            >
              Open report
            </button>
            <button
              onClick={handleDismissCompleted}
              className="rounded-lg border border-white/6 px-5 py-2.5 text-sm text-dim transition-colors hover:text-hi"
            >
              Check another car
            </button>
          </div>
        </div>
      )}

      {isFailed && (
        <div className="animate-panel-in rounded-xl border border-bad/20 bg-bad-tint p-7 space-y-3">
          <p className="text-sm font-semibold text-bad">Analysis failed</p>
          <p className="text-sm text-mid">
            {completedCheck?.failureReason ?? "We couldn't complete this check."}
          </p>
          <button
            onClick={handleDismissCompleted}
            className="rounded-lg border border-white/6 px-5 py-2.5 text-sm text-dim transition-colors hover:text-hi"
          >
            Check another car
          </button>
        </div>
      )}

      <AnalysisComposer
        onSubmitSuccess={handleSubmitSuccess}
        onImagePreview={setLightboxUrl}
        disabled={isProcessing || initializing}
      />

      {lightboxUrl && (
        <ImageLightbox url={lightboxUrl} onClose={() => setLightboxUrl(null)} />
      )}
    </div>
  );
}
