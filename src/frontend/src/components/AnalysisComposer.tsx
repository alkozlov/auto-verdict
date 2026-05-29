"use client";

import { useEffect, useRef, useState } from "react";
import { ImagePlus, Link2, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { api, type CarCheckResponse } from "@/lib/api";
import CodeMirror from "@uiw/react-codemirror";
import { markdown } from "@codemirror/lang-markdown";
import { EditorView, placeholder as cmPlaceholder } from "@codemirror/view";

const MAX_IMAGES = 5;
const MAX_IMAGE_BYTES = 2560 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];

const PLACEHOLDER =
  "Paste listing text, seller messages, VIN, concerns, inspection notes, or ask AutoVerdict specific questions.\n\nExample:\n\n\"I'm considering this Toyota Corolla from Otomoto. What should I verify before contacting the seller?\"";

const editorTheme = EditorView.theme(
  {
    "&": { backgroundColor: "#0F1217", color: "#F4F6F8" },
    ".cm-content": {
      padding: "12px 16px",
      fontFamily: "Inter, system-ui, -apple-system, sans-serif",
      fontSize: "15px",
      lineHeight: "1.6",
      caretColor: "#F4F6F8",
    },
    ".cm-cursor, .cm-dropCursor": { borderLeftColor: "#F4F6F8" },
    "&.cm-focused .cm-selectionBackground, .cm-selectionBackground": {
      backgroundColor: "#162A45",
    },
    ".cm-activeLine": { backgroundColor: "transparent" },
    ".cm-activeLineGutter": { backgroundColor: "transparent" },
    ".cm-gutters": { display: "none" },
    ".cm-placeholder": { color: "#596270" },
    "&.cm-editor.cm-focused": { outline: "none" },
  },
  { dark: true }
);

const editorExtensions = [
  markdown(),
  EditorView.lineWrapping,
  cmPlaceholder(PLACEHOLDER),
];

interface Props {
  onSubmitSuccess: (check: CarCheckResponse) => void;
  onImagePreview: (url: string) => void;
  disabled?: boolean;
}

function isValidUrl(s: string): boolean {
  try {
    new URL(s);
    return true;
  } catch {
    return false;
  }
}

function isOtomotoUrl(s: string): boolean {
  try {
    return new URL(s).hostname.toLowerCase().includes("otomoto");
  } catch {
    return false;
  }
}

function formatLinkPreview(url: string): string {
  try {
    const u = new URL(url);
    const path = u.pathname.length > 22 ? u.pathname.slice(0, 22) + "…" : u.pathname;
    return `${u.hostname}${path}`;
  } catch {
    return url.slice(0, 40);
  }
}

export function AnalysisComposer({ onSubmitSuccess, onImagePreview, disabled = false }: Props) {
  const [description, setDescription] = useState("");
  const [images, setImages] = useState<File[]>([]);
  const [imageUrls, setImageUrls] = useState<string[]>([]);
  const [link, setLink] = useState("");
  const [linkDraft, setLinkDraft] = useState("");
  const [showLinkInput, setShowLinkInput] = useState(false);
  const [linkError, setLinkError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const imageUrlsRef = useRef<string[]>([]);
  imageUrlsRef.current = imageUrls;
  const editorContainerRef = useRef<HTMLDivElement>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const tdRef = useRef<any>(null);

  useEffect(() => {
    return () => imageUrlsRef.current.forEach(URL.revokeObjectURL);
  }, []);

  useEffect(() => {
    import("turndown").then(({ default: TurndownService }) => {
      tdRef.current = new TurndownService({
        headingStyle: "atx",
        bulletListMarker: "-",
      });
    });
  }, []);

  useEffect(() => {
    const container = editorContainerRef.current;
    if (!container) return;
    function handlePaste(e: ClipboardEvent) {
      const html = e.clipboardData?.getData("text/html");
      if (!html || !tdRef.current) return;
      e.preventDefault();
      e.stopPropagation();
      const md = (tdRef.current.turndown(html) as string).trim();
      if (md) {
        setDescription((prev) =>
          prev.trim() ? `${prev.trimEnd()}\n\n${md}` : md
        );
      }
    }
    container.addEventListener("paste", handlePaste, true);
    return () => container.removeEventListener("paste", handlePaste, true);
  }, []);

  function addImages(files: FileList | null) {
    if (!files) return;
    const errors: string[] = [];
    const newFiles: File[] = [];
    const newUrls: string[] = [];
    for (const f of Array.from(files)) {
      if (images.length + newFiles.length >= MAX_IMAGES) break;
      if (!ALLOWED_TYPES.includes(f.type)) {
        errors.push(`"${f.name}": Photos must be JPEG, PNG, or WEBP.`);
        continue;
      }
      if (f.size > MAX_IMAGE_BYTES) {
        errors.push(`"${f.name}": Each photo must be 2.5 MB or smaller.`);
        continue;
      }
      newFiles.push(f);
      newUrls.push(URL.createObjectURL(f));
    }
    setImages((prev) => [...prev, ...newFiles]);
    setImageUrls((prev) => [...prev, ...newUrls]);
    if (errors.length > 0) setFormError(errors.join(" "));
  }

  function removeImage(i: number) {
    URL.revokeObjectURL(imageUrls[i]);
    setImages((prev) => prev.filter((_, j) => j !== i));
    setImageUrls((prev) => prev.filter((_, j) => j !== i));
  }

  function confirmLink() {
    const v = linkDraft.trim();
    if (!v) {
      setLink("");
      setShowLinkInput(false);
      setLinkError(null);
      return;
    }
    if (!isValidUrl(v)) {
      setLinkError("Enter a valid URL.");
      return;
    }
    if (!isOtomotoUrl(v)) {
      setLinkError(
        "For now, AutoVerdict can only crawl Otomoto.pl listings. You can still paste text from other sites into the main field."
      );
      return;
    }
    setLink(v);
    setLinkDraft("");
    setShowLinkInput(false);
    setLinkError(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (disabled) return;
    setFormError(null);
    const desc = description.trim();
    if (!desc) {
      setFormError(
        "Add at least a short description, question, or copied listing text before analyzing."
      );
      return;
    }
    setSubmitting(true);
    try {
      const check = await api.checks.create({
        description: desc,
        link: link || undefined,
        images,
      });
      setDescription("");
      imageUrls.forEach(URL.revokeObjectURL);
      setImages([]);
      setImageUrls([]);
      setLink("");
      onSubmitSuccess(check);
    } catch (err) {
      const msg = err instanceof Error ? err.message : "";
      if (msg.startsWith("409:")) {
        setFormError("An analysis is already in progress. Please wait for it to complete.");
      } else {
        setFormError(msg || "Something went wrong.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={cn("rounded-xl border border-white/6 bg-surface overflow-hidden transition-opacity", disabled && "opacity-60")}>
      {/* Card header */}
      <div className="border-b border-white/6 px-7 py-5">
        <h2 className="text-[15px] font-[650] text-hi">
          Tell AutoVerdict what to analyze
        </h2>
        <p className="mt-3 text-sm text-dim leading-relaxed">
          Paste the listing text, seller messages, VIN, inspection notes, or ask specific
          questions. Add an Otomoto link or photos if you have them.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="p-7 space-y-5">
        {/* Markdown editor */}
        <div
          ref={editorContainerRef}
          className="overflow-hidden rounded-md border border-white/6 transition-colors focus-within:border-active"
        >
          <CodeMirror
            value={description}
            onChange={(v) => {
              if (!disabled) {
                setDescription(v);
                setFormError(null);
              }
            }}
            height="220px"
            theme={editorTheme}
            extensions={editorExtensions}
            editable={!disabled}
            basicSetup={{
              lineNumbers: false,
              foldGutter: false,
              dropCursor: false,
              allowMultipleSelections: false,
              indentOnInput: false,
              highlightActiveLine: false,
              highlightSelectionMatches: false,
              closeBrackets: false,
              autocompletion: false,
              rectangularSelection: false,
              crosshairCursor: false,
            }}
          />
        </div>

        {/* Image thumbnails */}
        {imageUrls.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {imageUrls.map((url, i) => (
              <div key={i} className="group relative h-[72px] w-[72px]">
                <button
                  type="button"
                  onClick={() => onImagePreview(url)}
                  title={images[i].name}
                  className="block h-full w-full overflow-hidden rounded-md border border-white/6 bg-surface-raised focus:outline-none focus:ring-2 focus:ring-active"
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
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
                  className="absolute -right-1.5 -top-1.5 flex h-5 w-5 items-center justify-center rounded-full border border-white/10 bg-surface text-dim opacity-0 transition-opacity group-hover:opacity-100 hover:text-bad"
                >
                  <X className="h-3 w-3" />
                </button>
              </div>
            ))}
          </div>
        )}

        {/* Link pill */}
        {link && !showLinkInput && (
          <div className="flex w-fit max-w-full items-center gap-2 rounded-md border border-white/6 bg-surface-raised px-3 py-1.5">
            <Link2 className="h-3.5 w-3.5 shrink-0 text-dim" />
            <span className="max-w-[300px] truncate text-xs text-mid">
              Otomoto link added · {formatLinkPreview(link)}
            </span>
            <button
              type="button"
              onClick={() => setLink("")}
              aria-label="Remove link"
              className="shrink-0 text-dim transition-colors hover:text-bad"
            >
              <X className="h-3 w-3" />
            </button>
          </div>
        )}

        {/* Link input */}
        {showLinkInput && (
          <div className="space-y-1.5">
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
                placeholder="Paste Otomoto listing URL"
                className="flex-1 rounded-md border border-white/6 bg-field px-3 py-1.5 text-sm text-hi placeholder:text-dim focus:border-active focus:outline-none transition-colors"
              />
              <button
                type="button"
                onClick={confirmLink}
                className="rounded-md bg-brand px-3 py-1.5 text-sm font-medium text-page transition-all hover:brightness-105"
              >
                Add link
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowLinkInput(false);
                  setLinkDraft("");
                  setLinkError(null);
                }}
                className="rounded-md border border-white/6 px-3 py-1.5 text-sm text-dim transition-colors hover:text-hi"
              >
                Cancel
              </button>
            </div>
            {linkError && (
              <p className="text-xs text-warn leading-relaxed">{linkError}</p>
            )}
          </div>
        )}

        {/* Attachment buttons */}
        <div className="flex flex-wrap items-center gap-2">
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
          {images.length >= MAX_IMAGES ? (
            <span className="rounded-md border border-dashed border-white/12 bg-white/1 px-3 py-1.5 text-xs text-off">
              5 photos added
            </span>
          ) : (
            <button
              type="button"
              disabled={disabled}
              onClick={() => fileInputRef.current?.click()}
              className="inline-flex items-center gap-1.5 rounded-md border border-dashed border-white/12 bg-white/1 px-3 py-1.5 text-sm text-dim transition-colors hover:bg-white/3 hover:text-mid disabled:cursor-not-allowed"
            >
              <ImagePlus className="h-3.5 w-3.5" />
              {images.length > 0
                ? `Add photos (${images.length}/${MAX_IMAGES})`
                : "Add photos"}
            </button>
          )}
          {!showLinkInput && (
            <button
              type="button"
              disabled={disabled}
              onClick={() => {
                setLinkDraft(link);
                setShowLinkInput(true);
              }}
              className="inline-flex items-center gap-1.5 rounded-md border border-dashed border-white/12 bg-white/1 px-3 py-1.5 text-sm text-dim transition-colors hover:bg-white/3 hover:text-mid disabled:cursor-not-allowed"
            >
              <Link2 className="h-3.5 w-3.5" />
              {link ? "Change link" : "Add Otomoto link"}
            </button>
          )}
        </div>

        {formError && (
          <p className="text-sm text-bad" role="alert">
            {formError}
          </p>
        )}

        {/* Primary submit button */}
        <button
          type="submit"
          disabled={submitting || disabled}
          className={cn(
            "flex h-14 w-full items-center justify-center rounded-lg bg-brand px-4",
            "text-sm font-semibold text-page transition-all",
            "hover:brightness-105 disabled:cursor-not-allowed disabled:opacity-50"
          )}
        >
          {submitting ? "Submitting…" : disabled ? "Analysis in progress…" : "Analyze with AI"}
        </button>
      </form>
    </div>
  );
}
