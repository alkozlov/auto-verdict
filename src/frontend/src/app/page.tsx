"use client";

import dynamic from "next/dynamic";
import { useEffect, useRef, useState } from "react";
import { getToken, removeToken } from "@/lib/auth";
import { api, CarCheckResponse, MeResponse } from "@/lib/api";

const MDEditor = dynamic(() => import("@uiw/react-md-editor"), { ssr: false });
const MDPreview = dynamic(
  () => import("@uiw/react-md-editor").then((m) => m.default.Markdown),
  { ssr: false }
);

const MAX_IMAGES = 5;
const MAX_IMAGE_BYTES = 2560 * 1024;
const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"];
const PAGE_SIZE = 5;

function isValidUrl(url: string): boolean {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function HomePage() {
  type AuthState = "loading" | "unauthenticated" | "authenticated";

  const [authState, setAuthState] = useState<AuthState>("loading");
  const [me, setMe] = useState<MeResponse | null>(null);

  const [checks, setChecks] = useState<CarCheckResponse[]>([]);
  const [page, setPage] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);

  // Form
  const [description, setDescription] = useState("");
  const [images, setImages] = useState<File[]>([]);
  const [imageUrls, setImageUrls] = useState<string[]>([]);
  const [link, setLink] = useState("");
  const [linkDraft, setLinkDraft] = useState("");
  const [showLinkInput, setShowLinkInput] = useState(false);
  const [linkError, setLinkError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Modal
  const [modalCheck, setModalCheck] = useState<CarCheckResponse | null>(null);
  const [modalLoading, setModalLoading] = useState(false);

  // Lightbox
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  // Editor color mode
  const [colorMode, setColorMode] = useState<"light" | "dark">("light");

  const fileInputRef = useRef<HTMLInputElement>(null);
  const imageUrlsRef = useRef<string[]>([]);
  imageUrlsRef.current = imageUrls;
  const editorContainerRef = useRef<HTMLDivElement>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const tdRef = useRef<any>(null);

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
  }, []);

  // Revoke object URLs on unmount
  useEffect(() => {
    return () => imageUrlsRef.current.forEach(URL.revokeObjectURL);
  }, []);

  // Poll for updates
  useEffect(() => {
    if (authState !== "authenticated") return;
    const id = setInterval(() => loadChecks(page), 5000);
    return () => clearInterval(id);
  }, [authState, page]);

  // Sync editor color mode with system preference
  useEffect(() => {
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    setColorMode(mq.matches ? "dark" : "light");
    const handler = (e: MediaQueryListEvent) =>
      setColorMode(e.matches ? "dark" : "light");
    mq.addEventListener("change", handler);
    return () => mq.removeEventListener("change", handler);
  }, []);

  // Initialise turndown (HTML → Markdown) once
  useEffect(() => {
    import("turndown").then(({ default: TurndownService }) => {
      tdRef.current = new TurndownService({
        headingStyle: "atx",
        bulletListMarker: "-",
      });
    });
  }, []);

  // Intercept paste events before CodeMirror to preserve HTML formatting
  useEffect(() => {
    const container = editorContainerRef.current;
    if (!container) return;
    function handlePaste(e: ClipboardEvent) {
      const html = e.clipboardData?.getData("text/html");
      if (!html || !tdRef.current) return;
      e.preventDefault();
      e.stopPropagation();
      const markdown = (tdRef.current.turndown(html) as string).trim();
      if (markdown) {
        setDescription((prev) =>
          prev.trim() ? `${prev.trimEnd()}\n\n${markdown}` : markdown
        );
      }
    }
    container.addEventListener("paste", handlePaste, true);
    return () => container.removeEventListener("paste", handlePaste, true);
  }, []);

  async function loadChecks(p: number) {
    try {
      const [meData, data] = await Promise.all([
        api.me(),
        api.checks.list(p, PAGE_SIZE),
      ]);
      setMe(meData);
      setChecks(data);
      setHasNextPage(data.length === PAGE_SIZE);
    } catch {
      // Silently ignore polling errors
    }
  }

  function addImages(files: FileList | null) {
    if (!files) return;
    const errors: string[] = [];
    const newFiles: File[] = [];
    const newUrls: string[] = [];

    for (const f of Array.from(files)) {
      if (images.length + newFiles.length >= MAX_IMAGES) break;
      if (!ALLOWED_IMAGE_TYPES.includes(f.type)) {
        errors.push(`"${f.name}": unsupported type — use JPEG, PNG, or WEBP`);
        continue;
      }
      if (f.size > MAX_IMAGE_BYTES) {
        errors.push(`"${f.name}": exceeds the 2560 KB limit`);
        continue;
      }
      newFiles.push(f);
      newUrls.push(URL.createObjectURL(f));
    }

    setImages((prev) => [...prev, ...newFiles]);
    setImageUrls((prev) => [...prev, ...newUrls]);
    if (errors.length > 0) setFormError(errors.join("; "));
  }

  function removeImage(i: number) {
    URL.revokeObjectURL(imageUrls[i]);
    setImages((prev) => prev.filter((_, idx) => idx !== i));
    setImageUrls((prev) => prev.filter((_, idx) => idx !== i));
  }

  function confirmLink() {
    const trimmed = linkDraft.trim();
    if (!trimmed) {
      setLink("");
      setShowLinkInput(false);
      setLinkError(null);
      return;
    }
    if (!isValidUrl(trimmed)) {
      setLinkError("Please enter a valid URL.");
      return;
    }
    setLink(trimmed);
    setLinkDraft("");
    setShowLinkInput(false);
    setLinkError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);

    const desc = description.trim();
    if (!desc) {
      setFormError("A description is required. Paste the listing text into the editor above.");
      return;
    }

    setSubmitting(true);
    try {
      await api.checks.create({
        description: desc,
        link: link || undefined,
        images,
      });
      setDescription("");
      imageUrls.forEach(URL.revokeObjectURL);
      setImages([]);
      setImageUrls([]);
      setLink("");
      setPage(1);
      await loadChecks(1);
    } catch (err) {
      setFormError(
        err instanceof Error ? err.message : "Something went wrong."
      );
    } finally {
      setSubmitting(false);
    }
  }

  async function openModal(checkId: string) {
    setModalLoading(true);
    setModalCheck(null);
    try {
      const check = await api.checks.get(checkId);
      setModalCheck(check);
    } finally {
      setModalLoading(false);
    }
  }

  function closeModal() {
    setModalCheck(null);
    setModalLoading(false);
  }

  async function goToPage(p: number) {
    setPage(p);
    await loadChecks(p);
  }

  // ── Render ────────────────────────────────────────────────────────────────

  if (authState === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <p className="text-sm text-muted-foreground">Loading…</p>
      </div>
    );
  }

  if (authState === "unauthenticated") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-6 text-center px-4">
          <h1 className="text-4xl font-bold tracking-tight text-foreground">
            AutoVerdict
          </h1>
          <p className="max-w-sm text-sm text-muted-foreground">
            AI-powered car listing analysis. Spot risks, verify facts, and get
            a purchase recommendation.
          </p>
          <a
            href="/api/auth/google"
            className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            Sign in with Google
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b border-border px-6 py-3 flex items-center justify-between">
        <span className="text-sm font-semibold text-foreground">AutoVerdict</span>
        <div className="flex items-center gap-4 text-sm">
          <span className="hidden text-muted-foreground sm:inline">
            {me?.email}
          </span>
          <span className="text-muted-foreground">
            Credits:{" "}
            <span className="font-semibold text-foreground">
              {me?.credits ?? 0}
            </span>
          </span>
          <button
            onClick={() => {
              removeToken();
              setAuthState("unauthenticated");
              setMe(null);
            }}
            className="text-muted-foreground transition-colors hover:text-foreground"
          >
            Sign out
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-6 py-8 space-y-10">
        {/* ── Submission form ── */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Markdown editor */}
          <div className="space-y-1.5">
            <p className="text-sm text-muted-foreground">
              Paste the listing text here — ad copy, seller messages, specs, VIN,
              inspection notes, anything relevant. Use the toolbar to bold, italicise,
              or list key details.
            </p>
            <div
              ref={editorContainerRef}
              data-color-mode={colorMode}
              className="overflow-hidden rounded-md border border-input"
            >
              <MDEditor
                value={description}
                onChange={(v) => {
                  setDescription(v ?? "");
                  setFormError(null);
                }}
                preview="edit"
                height={220}
                style={{ border: "none", borderRadius: 0 }}
              />
            </div>
          </div>

          {/* Image thumbnails */}
          {imageUrls.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {imageUrls.map((url, i) => (
                <div key={i} className="group relative h-16 w-16">
                  <button
                    type="button"
                    onClick={() => setLightboxUrl(url)}
                    title={images[i].name}
                    className="block h-full w-full overflow-hidden rounded-md border border-border bg-secondary focus:outline-none"
                  >
                    <img
                      src={url}
                      alt={images[i].name}
                      className="h-full w-full object-cover"
                    />
                  </button>
                  <button
                    type="button"
                    onClick={() => removeImage(i)}
                    aria-label={`Remove ${images[i].name}`}
                    className="absolute -right-1.5 -top-1.5 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-[10px] text-destructive-foreground opacity-0 transition-opacity group-hover:opacity-100"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Attached link pill */}
          {link && !showLinkInput && (
            <div className="flex w-fit max-w-full items-center gap-2 rounded-full border border-border bg-secondary/60 px-3 py-1">
              <span className="max-w-xs truncate text-xs text-foreground">
                {link}
              </span>
              <button
                type="button"
                onClick={() => setLink("")}
                aria-label="Remove link"
                className="shrink-0 text-xs text-muted-foreground transition-colors hover:text-destructive"
              >
                ✕
              </button>
            </div>
          )}

          {/* Link input */}
          {showLinkInput && (
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <input
                  type="url"
                  value={linkDraft}
                  autoFocus
                  onChange={(e) => {
                    setLinkDraft(e.target.value);
                    setLinkError(null);
                  }}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      confirmLink();
                    }
                  }}
                  placeholder="https://www.otomoto.pl/..."
                  className="flex-1 rounded-md border border-input bg-background px-3 py-1.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
                <button
                  type="button"
                  onClick={confirmLink}
                  className="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                >
                  Attach
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowLinkInput(false);
                    setLinkDraft("");
                    setLinkError(null);
                  }}
                  className="rounded-md border border-border px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                >
                  Cancel
                </button>
              </div>
              {linkError && (
                <p className="text-xs text-destructive">{linkError}</p>
              )}
            </div>
          )}

          {/* Attachment buttons */}
          <div className="flex flex-wrap items-center gap-2">
            {images.length < MAX_IMAGES && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp"
                  className="hidden"
                  onChange={(e) => {
                    addImages(e.target.files);
                    e.target.value = "";
                  }}
                />
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="rounded-md border border-dashed border-input px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:border-ring hover:text-foreground"
                >
                  {images.length > 0
                    ? `Attach Images (${images.length}/${MAX_IMAGES})`
                    : "Attach Images"}
                </button>
              </>
            )}
            {!showLinkInput && (
              <button
                type="button"
                onClick={() => {
                  setLinkDraft(link);
                  setShowLinkInput(true);
                }}
                className="rounded-md border border-dashed border-input px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:border-ring hover:text-foreground"
              >
                {link ? "Change Link" : "Attach Link"}
              </button>
            )}
          </div>

          {formError && (
            <p className="text-sm text-destructive">{formError}</p>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {submitting ? "Submitting…" : "Analyze Listing"}
          </button>
        </form>

        {/* ── History ── */}
        <section>
          <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Analysis History
          </h2>
          {checks.length === 0 ? (
            <p className="text-sm text-muted-foreground">No analyses yet.</p>
          ) : (
            <>
              <ul className="space-y-2">
                {checks.map((c) => (
                  <li key={c.checkId}>
                    <button
                      onClick={() => openModal(c.checkId)}
                      className="w-full rounded-md border border-border px-4 py-3 text-left transition-colors hover:bg-secondary/50"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <span className="min-w-0 truncate text-sm font-medium text-foreground">
                          {formatCheckTitle(c)}
                        </span>
                        <StatusBadge status={c.status} />
                      </div>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {new Date(c.createdAt).toLocaleString()}
                      </p>
                    </button>
                  </li>
                ))}
              </ul>

              {(page > 1 || hasNextPage) && (
                <div className="mt-4 flex items-center justify-between text-sm">
                  <button
                    onClick={() => goToPage(page - 1)}
                    disabled={page === 1}
                    className="rounded-md border border-border px-3 py-1.5 text-muted-foreground transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    ← Previous
                  </button>
                  <span className="text-xs text-muted-foreground">
                    Page {page}
                  </span>
                  <button
                    onClick={() => goToPage(page + 1)}
                    disabled={!hasNextPage}
                    className="rounded-md border border-border px-3 py-1.5 text-muted-foreground transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Next →
                  </button>
                </div>
              )}
            </>
          )}
        </section>
      </main>

      {/* ── Analysis modal ── */}
      {(modalLoading || modalCheck) && (
        <CheckModal
          check={modalCheck}
          loading={modalLoading}
          onClose={closeModal}
        />
      )}

      {/* ── Image lightbox ── */}
      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80"
          onClick={() => setLightboxUrl(null)}
        >
          <img
            src={lightboxUrl}
            alt="Full size preview"
            className="max-h-[90vh] max-w-[90vw] rounded-md object-contain"
            onClick={(e) => e.stopPropagation()}
          />
          <button
            onClick={() => setLightboxUrl(null)}
            aria-label="Close image"
            className="absolute right-4 top-4 text-2xl leading-none text-white opacity-80 transition-opacity hover:opacity-100"
          >
            ✕
          </button>
        </div>
      )}
    </div>
  );
}

// ── Check modal ───────────────────────────────────────────────────────────────

function CheckModal({
  check,
  loading,
  onClose,
}: {
  check: CarCheckResponse | null;
  loading: boolean;
  onClose: () => void;
}) {
  return (
    <div
      className="fixed inset-0 z-40 flex items-start justify-center overflow-y-auto bg-black/50 px-4 py-8"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-3xl rounded-lg border border-border bg-background shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          onClick={onClose}
          aria-label="Close"
          className="absolute right-4 top-4 text-muted-foreground transition-colors hover:text-foreground"
        >
          ✕
        </button>

        <div className="p-6">
          {loading && (
            <p className="text-sm text-muted-foreground">Loading…</p>
          )}

          {check && !loading && (
            <>
              {check.report ? (
                <ReportView report={check.report} />
              ) : check.status === "Failed" && check.failureReason ? (
                <div className="space-y-1">
                  <p className="text-sm font-medium text-destructive">
                    Analysis failed
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {check.failureReason}
                  </p>
                </div>
              ) : (
                <div className="flex items-center gap-3">
                  <span className="text-sm font-medium text-foreground">
                    {formatCheckTitle(check)}
                  </span>
                  <StatusBadge status={check.status} />
                  <span className="text-sm text-muted-foreground">
                    — analysis in progress
                  </span>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Report view ───────────────────────────────────────────────────────────────

function ReportView({ report }: { report: string }) {
  return (
    <div data-color-mode="light" className="wmde-markdown-var">
      <MDPreview source={report} />
    </div>
  );
}

// ── Status badge ──────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: CarCheckResponse["status"] }) {
  const styles: Record<string, string> = {
    Pending:
      "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400",
    Processing:
      "bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400",
    Completed:
      "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400",
    Failed: "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400",
  };
  return (
    <span
      className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${styles[status] ?? ""}`}
    >
      {status}
    </span>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatCheckTitle(check: CarCheckResponse): string {
  return check.title ?? check.listingUrl ?? "Listing analysis";
}
