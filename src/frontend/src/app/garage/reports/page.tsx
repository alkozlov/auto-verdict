"use client";

import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type CarCheckResponse } from "@/lib/api";
import { AnalysisHistory } from "@/components/AnalysisHistory";

const PAGE_SIZE = 10;

export default function ReportsPage() {
  const navigate = useNavigate();
  const [checks, setChecks] = useState<CarCheckResponse[]>([]);
  const [page, setPage] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [loading, setLoading] = useState(true);

  const loadChecks = useCallback(async (p: number) => {
    try {
      const data = await api.checks.list(p, PAGE_SIZE);
      setChecks(data);
      setHasNextPage(data.length === PAGE_SIZE);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadChecks(1);
  }, [loadChecks]);

  useEffect(() => {
    const hasActive = checks.some(
      (c) => c.status === "Pending" || c.status === "Processing"
    );
    if (!hasActive) return;
    const id = setInterval(() => loadChecks(page), 5000);
    return () => clearInterval(id);
  }, [checks, page, loadChecks]);

  function handlePageChange(p: number) {
    setPage(p);
    loadChecks(p);
  }

  return (
    <div className="mx-auto max-w-[760px] space-y-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-[22px] font-[650] text-hi">My reports</h1>
          <p className="mt-1.5 text-sm text-dim">
            Review your previous car analyses and open completed reports.
          </p>
        </div>
        <Link
          to="/garage/check"
          className="shrink-0 rounded-lg bg-brand px-4 py-2.5 text-sm font-semibold text-page transition-all hover:brightness-105"
        >
          Check another car
        </Link>
      </div>

      {loading ? (
        <p className="text-sm text-dim">Loading…</p>
      ) : (
        <AnalysisHistory
          checks={checks}
          page={page}
          hasNextPage={hasNextPage}
          selectedCheckId={null}
          onSelectCheck={(id) => navigate(`/garage/reports/${id}`)}
          onPageChange={handlePageChange}
        />
      )}
    </div>
  );
}
