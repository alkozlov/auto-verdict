"use client";

import { useCallback, useEffect, useState } from "react";
import { getToken, removeToken } from "@/lib/auth";
import { api, type CarCheckResponse, type MeResponse } from "@/lib/api";
import { Header } from "@/components/Header";
import { LoginScreen } from "@/components/LoginScreen";
import { AnalysisComposer } from "@/components/AnalysisComposer";
import { ProcessingPanel } from "@/components/ProcessingPanel";
import { InlineReportPanel } from "@/components/InlineReportPanel";
import { AnalysisHistory } from "@/components/AnalysisHistory";
import { ImageLightbox } from "@/components/ImageLightbox";

const PAGE_SIZE = 5;

type AuthState = "loading" | "unauthenticated" | "authenticated";

interface ProcessingInfo {
  checkId: string;
  hasLink: boolean;
  hasPhotos: boolean;
}

export default function HomePage() {
  const [authState, setAuthState] = useState<AuthState>("loading");
  const [me, setMe] = useState<MeResponse | null>(null);
  const [checks, setChecks] = useState<CarCheckResponse[]>([]);
  const [page, setPage] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [processingInfo, setProcessingInfo] = useState<ProcessingInfo | null>(null);
  const [selectedCheckId, setSelectedCheckId] = useState<string | null>(null);
  const [selectedCheck, setSelectedCheck] = useState<CarCheckResponse | null>(null);
  const [reportLoading, setReportLoading] = useState(false);
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  const loadChecks = useCallback(async (p: number) => {
    try {
      const [meData, data] = await Promise.all([
        api.me(),
        api.checks.list(p, PAGE_SIZE),
      ]);
      setMe(meData);
      setChecks(data);
      setHasNextPage(data.length === PAGE_SIZE);
    } catch {
      // silently ignore polling errors
    }
  }, []);

  // Auth init
  useEffect(() => {
    const token = getToken();
    if (!token) {
      setAuthState("unauthenticated");
      return;
    }
    api
      .me()
      .then((meData) => {
        setMe(meData);
        setAuthState("authenticated");
        loadChecks(1);
      })
      .catch(() => {
        removeToken();
        setAuthState("unauthenticated");
      });
  }, [loadChecks]);

  // Polling
  useEffect(() => {
    if (authState !== "authenticated") return;
    const id = setInterval(() => loadChecks(page), 5000);
    return () => clearInterval(id);
  }, [authState, page, loadChecks]);

  // Auto-open report when a submitted check finishes
  useEffect(() => {
    if (!processingInfo) return;
    const check = checks.find((c) => c.checkId === processingInfo.checkId);
    if (
      check?.status !== "Completed" &&
      check?.status !== "Failed"
    )
      return;

    const id = processingInfo.checkId;
    setProcessingInfo(null);
    setSelectedCheckId(id);
    setReportLoading(true);
    setSelectedCheck(null);
    api.checks
      .get(id)
      .then(setSelectedCheck)
      .catch(() => {})
      .finally(() => setReportLoading(false));
  }, [checks, processingInfo]);

  function openReport(checkId: string) {
    setSelectedCheckId(checkId);
    setReportLoading(true);
    setSelectedCheck(null);
    api.checks
      .get(checkId)
      .then(setSelectedCheck)
      .catch(() => {})
      .finally(() => setReportLoading(false));
  }

  function handleSubmitSuccess(
    checkId: string,
    hasLink: boolean,
    hasPhotos: boolean
  ) {
    setProcessingInfo({ checkId, hasLink, hasPhotos });
    setSelectedCheckId(null);
    setSelectedCheck(null);
    loadChecks(1);
  }

  function handlePageChange(p: number) {
    setPage(p);
    loadChecks(p);
  }

  function handleSignOut() {
    removeToken();
    setAuthState("unauthenticated");
    setMe(null);
  }

  // ── Render ────────────────────────────────────────────────────────

  if (authState === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-page">
        <p className="text-sm text-dim">Loading…</p>
      </div>
    );
  }

  if (authState === "unauthenticated") {
    return <LoginScreen />;
  }

  const processingCheck = processingInfo
    ? checks.find((c) => c.checkId === processingInfo.checkId)
    : null;
  const showProcessing =
    !!processingInfo &&
    (!processingCheck ||
      processingCheck.status === "Pending" ||
      processingCheck.status === "Processing");

  return (
    <div className="min-h-screen bg-page">
      <Header me={me} onSignOut={handleSignOut} />

      <main className="mx-auto max-w-[960px] px-4 py-8 sm:px-6 lg:px-8 space-y-8">
        <p className="text-[20px] font-medium text-mid leading-snug">
          Get a second opinion before contacting the seller.
        </p>

        <AnalysisComposer
          credits={me?.credits ?? 0}
          onSubmitSuccess={handleSubmitSuccess}
          onImagePreview={setLightboxUrl}
        />

        {showProcessing && processingInfo && (
          <ProcessingPanel
            hasLink={processingInfo.hasLink}
            hasPhotos={processingInfo.hasPhotos}
          />
        )}

        {selectedCheckId && (
          <InlineReportPanel
            check={selectedCheck}
            loading={reportLoading}
            onClose={() => {
              setSelectedCheckId(null);
              setSelectedCheck(null);
            }}
          />
        )}

        <AnalysisHistory
          checks={checks}
          page={page}
          hasNextPage={hasNextPage}
          selectedCheckId={selectedCheckId}
          onSelectCheck={openReport}
          onPageChange={handlePageChange}
        />
      </main>

      {lightboxUrl && (
        <ImageLightbox
          url={lightboxUrl}
          onClose={() => setLightboxUrl(null)}
        />
      )}
    </div>
  );
}
