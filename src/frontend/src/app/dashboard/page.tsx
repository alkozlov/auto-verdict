"use client";

import { useEffect, useRef, useState } from "react";
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
  const [vehicleId, setVehicleId] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/");
      return;
    }
    loadData();
    pollRef.current = setInterval(loadData, 5000);
    return () => {
      if (pollRef.current) clearInterval(pollRef.current);
    };
  }, []);

  async function loadData() {
    try {
      const [meData, checksData] = await Promise.all([api.me(), api.checks.list()]);
      setMe(meData);
      setChecks(checksData);
      setSelected((prev) => (prev ? checksData.find((c) => c.checkId === prev.checkId) ?? null : null));
    } catch {
      // Token may have expired
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!vehicleId.trim() || !file) return;
    setError(null);
    setSubmitting(true);
    try {
      const upload = await api.uploads.upload(file);
      await api.checks.create(vehicleId.trim(), upload.storageKey);
      setVehicleId("");
      setFile(null);
      if (fileRef.current) fileRef.current.value = "";
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
        <p className="text-muted-foreground">Loading…</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
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
        {/* New check form */}
        <div>
          <h2 className="text-base font-semibold text-foreground mb-4">New Check</h2>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">
                Vehicle Identifier (VIN / plate)
              </label>
              <input
                type="text"
                value={vehicleId}
                onChange={(e) => setVehicleId(e.target.value)}
                placeholder="e.g. WBA3A5G59DNP26082"
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">
                Document (PDF, JPG, PNG — max 10 MB)
              </label>
              <input
                ref={fileRef}
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,.webp"
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                className="w-full text-sm text-muted-foreground file:mr-4 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-secondary-foreground hover:file:bg-secondary/80"
                required
              />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <button
              type="submit"
              disabled={submitting || !vehicleId.trim() || !file}
              className="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {submitting ? "Submitting…" : "Run Check (1 credit)"}
            </button>
          </form>
        </div>

        {/* Check history */}
        <div>
          <h2 className="text-base font-semibold text-foreground mb-4">Check History</h2>
          {checks.length === 0 ? (
            <p className="text-sm text-muted-foreground">No checks yet.</p>
          ) : (
            <ul className="space-y-2">
              {checks.map((c) => (
                <li key={c.checkId}>
                  <button
                    onClick={() => setSelected(c.checkId === selected?.checkId ? null : c)}
                    className="w-full text-left rounded-md border border-border px-4 py-3 hover:bg-secondary/50 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-foreground">{c.vehicleIdentifier}</span>
                      <StatusBadge status={c.status} />
                    </div>
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

      {/* Report detail */}
      {selected?.report && (
        <div className="mx-auto max-w-5xl px-6 pb-8">
          <ReportView report={selected.report} />
        </div>
      )}
      {selected?.status === "Failed" && selected.failureReason && (
        <div className="mx-auto max-w-5xl px-6 pb-8">
          <div className="rounded-md border border-destructive/30 bg-destructive/5 px-4 py-3">
            <p className="text-sm font-medium text-destructive">Check failed</p>
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
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${styles[status] ?? ""}`}>
      {status}
    </span>
  );
}

function ReportView({ report }: { report: VehicleReport }) {
  const verdict = report.verdict?.toLowerCase() ?? "";
  const verdictColor =
    verdict === "clean"
      ? "text-green-600 dark:text-green-400"
      : verdict === "caution"
      ? "text-yellow-600 dark:text-yellow-400"
      : "text-red-600 dark:text-red-400";

  return (
    <div className="rounded-md border border-border p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-base font-semibold text-foreground">
          Report — {report.vehicleIdentifier}
        </h3>
        <span className={`text-sm font-bold uppercase ${verdictColor}`}>{report.verdict}</span>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Section title="Ownership">
          <Row label="Owners" value={String(report.ownership.ownersCount)} />
          <Row label="Commercial use" value={report.ownership.commercialUseDetected ? "Yes" : "No"} />
          {report.ownership.notes && <Row label="Notes" value={report.ownership.notes} />}
        </Section>

        <Section title="Mileage">
          <Row label="Inconsistency" value={report.mileage.inconsistencyDetected ? "Detected" : "None"} />
          {report.mileage.lastRecordedKm != null && (
            <Row label="Last recorded" value={`${report.mileage.lastRecordedKm.toLocaleString()} km`} />
          )}
          {report.mileage.notes && <Row label="Notes" value={report.mileage.notes} />}
        </Section>

        <Section title="Accidents">
          <Row label="Total" value={String(report.accidents.totalCount)} />
          <Row label="Severe damage" value={report.accidents.severeDamageDetected ? "Yes" : "No"} />
          {report.accidents.notes && <Row label="Notes" value={report.accidents.notes} />}
        </Section>

        <Section title="Service">
          <Row label="Regular maintenance" value={report.service.regularMaintenanceConfirmed ? "Confirmed" : "Not confirmed"} />
          {report.service.lastServiceDate && (
            <Row label="Last service" value={new Date(report.service.lastServiceDate).toLocaleDateString()} />
          )}
          {report.service.notes && <Row label="Notes" value={report.service.notes} />}
        </Section>

        <Section title="Legal">
          <Row label="Pledge" value={report.legal.pledgeDetected ? "Detected" : "None"} />
          <Row label="Stolen" value={report.legal.stolenDetected ? "Detected" : "None"} />
          <Row label="Wanted" value={report.legal.wantedDetected ? "Detected" : "None"} />
          {report.legal.notes && <Row label="Notes" value={report.legal.notes} />}
        </Section>
      </div>
    </div>
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
    <div className="flex justify-between text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="text-foreground font-medium">{value}</span>
    </div>
  );
}
