"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getToken, removeToken } from "@/lib/auth";
import { api, CarCheckResponse, MeResponse, VehicleReport } from "@/lib/api";

export default function Dashboard() {
  const router = useRouter();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [checks, setChecks] = useState<CarCheckResponse[]>([]);
  const [selected, setSelected] = useState<CarCheckResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [listingUrl, setListingUrl] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/");
      return;
    }
    loadData();
    const poll = setInterval(loadData, 5000);
    return () => clearInterval(poll);
  }, [router]);

  async function loadData() {
    try {
      const [meData, checksData] = await Promise.all([api.me(), api.checks.list()]);
      setMe(meData);
      setChecks(checksData);
      setSelected((prev) => (prev ? checksData.find((c) => c.checkId === prev.checkId) ?? null : null));
    } catch {
      // Token may have expired.
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!listingUrl.trim()) return;
    setError(null);
    setSubmitting(true);
    try {
      await api.checks.create(listingUrl.trim());
      setListingUrl("");
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setSubmitting(false);
    }
  }

  function handleSignOut() {
    removeToken();
    router.replace("/");
  }

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-muted-foreground">Loading...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b border-border px-6 py-4 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold text-foreground">AutoVerdict</h1>
          <p className="text-sm text-muted-foreground">{me?.email}</p>
        </div>
        <div className="flex items-center gap-4">
          <div className="text-sm">
            <span className="text-muted-foreground">Credits: </span>
            <span className="font-semibold text-foreground">{me?.credits ?? 0}</span>
          </div>
          <button
            onClick={handleSignOut}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            Sign out
          </button>
        </div>
      </header>

      <div className="mx-auto max-w-5xl px-6 py-8 grid grid-cols-1 gap-8 lg:grid-cols-2">
        <div>
          <h2 className="text-base font-semibold text-foreground mb-4">New Otomoto Check</h2>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">
                Otomoto listing URL
              </label>
              <input
                type="url"
                value={listingUrl}
                onChange={(e) => setListingUrl(e.target.value)}
                placeholder="https://www.otomoto.pl/osobowe/oferta/..."
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                required
              />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <button
              type="submit"
              disabled={submitting || !listingUrl.trim()}
              className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {submitting ? "Submitting..." : "Run Check"}
            </button>
          </form>
        </div>

        <div>
          <h2 className="text-base font-semibold text-foreground mb-4">Requests</h2>
          {checks.length === 0 ? (
            <p className="text-sm text-muted-foreground">No requests yet.</p>
          ) : (
            <ul className="space-y-2">
              {checks.map((c) => (
                <li key={c.checkId}>
                  <button
                    onClick={() => setSelected(c.checkId === selected?.checkId ? null : c)}
                    className="w-full text-left rounded-md border border-border px-4 py-3 hover:bg-secondary/50 transition-colors"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <span className="min-w-0 truncate text-sm font-medium text-foreground">
                        {formatCheckTitle(c)}
                      </span>
                      <StatusBadge status={c.status} />
                    </div>
                    <p className="text-xs text-muted-foreground mt-1">{formatCheckMeta(c)}</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      {new Date(c.createdAt).toLocaleString()}
                    </p>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {selected?.report && (
        <div className="mx-auto max-w-5xl px-6 pb-8">
          <ReportView report={selected.report} />
        </div>
      )}
      {selected?.status === "Failed" && selected.failureReason && (
        <div className="mx-auto max-w-5xl px-6 pb-8">
          <div className="rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3">
            <p className="text-sm font-medium text-destructive">Request failed</p>
            <p className="text-sm text-muted-foreground mt-1">{selected.failureReason}</p>
          </div>
        </div>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: CarCheckResponse["status"] }) {
  const styles: Record<string, string> = {
    Pending: "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400",
    Processing: "bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400",
    Completed: "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400",
    Failed: "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400",
  };
  return (
    <span className={`shrink-0 inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${styles[status] ?? ""}`}>
      {status}
    </span>
  );
}

function ReportView({ report }: { report: VehicleReport }) {
  return (
    <div className="rounded-md border border-border p-6 space-y-4">
      <div>
        <h3 className="text-base font-semibold text-foreground">
          {report.listingFacts.title ?? "Listing report"}
        </h3>
        <p className="text-sm text-muted-foreground mt-1">{report.carSummary}</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Section title="Listing">
          <Row label="Make" value={report.listingFacts.make ?? "Unknown"} />
          <Row label="Model" value={report.listingFacts.model ?? "Unknown"} />
          <Row label="Year" value={report.listingFacts.year?.toString() ?? "Unknown"} />
          <Row
            label="Mileage"
            value={report.listingFacts.mileageKm != null ? `${report.listingFacts.mileageKm.toLocaleString()} km` : "Unknown"}
          />
          <Row label="Price" value={formatMoney(report.listingFacts.price, report.listingFacts.currency)} />
        </Section>

        <Section title="Costs">
          <Row label="Purchase" value={formatMoney(report.estimatedCosts.purchasePrice, report.estimatedCosts.currency)} />
          <Row label="Registration" value={formatMoney(report.estimatedCosts.registrationFee, report.estimatedCosts.currency)} />
          <Row label="Insurance" value={formatMoney(report.estimatedCosts.insuranceCost, report.estimatedCosts.currency)} />
          <Row label="Repairs" value={formatMoney(report.estimatedCosts.potentialRepairs, report.estimatedCosts.currency)} />
          <Row label="Total" value={formatMoney(report.estimatedCosts.total, report.estimatedCosts.currency)} />
        </Section>
      </div>

      <ListSection title="Model Risks" items={report.modelRisks} />
      <ListSection title="Listing Risks" items={report.listingRisks} />
      <ListSection title="Deal Risks" items={report.dealRisks} />
      <ListSection title="Seller Questions" items={report.sellerQuestions} />
      <ListSection title="Inspection Checklist" items={report.inspectionChecklist} />

      <Section title="Recommendation">
        <p className="text-sm text-foreground">{report.recommendation}</p>
      </Section>
      <Section title="Disclaimer">
        <p className="text-sm text-muted-foreground">{report.disclaimer}</p>
      </Section>
    </div>
  );
}

function ListSection({ title, items }: { title: string; items: string[] }) {
  return (
    <Section title={title}>
      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">None identified.</p>
      ) : (
        <ul className="list-disc space-y-1 pl-5 text-sm text-foreground">
          {items.map((item, index) => (
            <li key={index}>{item}</li>
          ))}
        </ul>
      )}
    </Section>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</h4>
      {children}
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="text-right text-foreground font-medium">{value}</span>
    </div>
  );
}

function formatCheckTitle(check: CarCheckResponse) {
  const makeModel = [check.make, check.model].filter(Boolean).join(" ");
  return check.title ?? (makeModel || check.listingUrl);
}

function formatCheckMeta(check: CarCheckResponse) {
  const parts = [
    check.year?.toString(),
    check.mileageKm != null ? `${check.mileageKm.toLocaleString()} km` : null,
    check.price != null ? formatMoney(check.price, check.currency) : null,
  ].filter(Boolean);
  return parts.length > 0 ? parts.join(" | ") : check.listingUrl;
}

function formatMoney(value: number | null | undefined, currency: string | null | undefined) {
  return value != null ? `${value.toLocaleString()} ${currency ?? "PLN"}` : "Unknown";
}
