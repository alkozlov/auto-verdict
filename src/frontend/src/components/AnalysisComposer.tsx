"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { ImagePlus, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { api, type CarCheckResponse } from "@/lib/api";
import CodeMirror from "@uiw/react-codemirror";
import { markdown } from "@codemirror/lang-markdown";
import { EditorView, placeholder as cmPlaceholder } from "@codemirror/view";
import { useTranslation } from "react-i18next";

const MAX_IMAGES = 5;
const MAX_IMAGE_BYTES = 2560 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];

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


export function AnalysisComposer({ onSubmitSuccess, onImagePreview, disabled = false }: Props) {
  const { t } = useTranslation();
  const [description, setDescription] = useState("");
  const [images, setImages] = useState<File[]>([]);
  const [imageUrls, setImageUrls] = useState<string[]>([]);
  const [link, setLink] = useState("");
  const [linkError, setLinkError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const editorExtensions = useMemo(
    () => [markdown(), EditorView.lineWrapping, cmPlaceholder(t("garage.composer.placeholder"))],
    [t]
  );

  const fileInputRef = useRef<HTMLInputElement>(null);
  const imageUrlsRef = useRef<string[]>([]);
  imageUrlsRef.current = imageUrls;
  const editorContainerRef = useRef<HTMLDivElement>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const tdRef = useRef<any>(null);
  // Always-current reference to addImages so the stable paste handler can call it
  // without a stale closure over the `images` state.
  const addImagesRef = useRef(addImages);
  addImagesRef.current = addImages;

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
      // Intercept image files from the clipboard (screenshots, copied images).
      // We attach them as file uploads instead of letting them land as text.
      const items = e.clipboardData?.items;
      if (items) {
        const imageFiles: File[] = [];
        for (const item of Array.from(items)) {
          if (item.kind === "file" && item.type.startsWith("image/")) {
            const file = item.getAsFile();
            if (file) imageFiles.push(file);
          }
        }
        if (imageFiles.length > 0) {
          e.preventDefault();
          addImagesRef.current(imageFiles);
          return;
        }
      }

      // Fall through: convert pasted HTML to markdown.
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

  function addImages(files: FileList | File[] | null) {
    if (!files) return;
    const errors: string[] = [];
    const newFiles: File[] = [];
    const newUrls: string[] = [];
    for (const f of Array.from(files)) {
      if (images.length + newFiles.length >= MAX_IMAGES) break;
      if (!ALLOWED_TYPES.includes(f.type)) {
        errors.push(`"${f.name}": ${t("garage.composer.errorPhotoType")}`);
        continue;
      }
      if (f.size > MAX_IMAGE_BYTES) {
        errors.push(`"${f.name}": ${t("garage.composer.errorPhotoSize")}`);
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

  function handleLinkChange(v: string) {
    setLink(v);
    const trimmed = v.trim();
    if (!trimmed) {
      setLinkError(null);
    } else if (!isValidUrl(trimmed)) {
      setLinkError(t("garage.composer.errorValidUrl"));
    } else {
      setLinkError(null);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (disabled) return;
    setFormError(null);
    const desc = description.trim();
    if (!desc) {
      setFormError(t("garage.composer.errorDescriptionRequired"));
      return;
    }
    if (linkError) return;
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
        setFormError(t("garage.composer.errorAnalysisInProgress"));
      } else {
        setFormError(msg || t("garage.composer.errorGeneric"));
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
          {t("garage.composer.cardTitle")}
        </h2>
        <p className="mt-3 text-sm text-dim leading-relaxed">
          {t("garage.composer.cardBody")}
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

        {/* Link input */}
        <div className="space-y-1.5">
          <input
            type="url"
            value={link}
            disabled={disabled}
            onChange={(e) => handleLinkChange(e.target.value)}
            placeholder={t("garage.composer.linkPlaceholder")}
            className={cn(
              "w-full rounded-md border bg-field px-3 py-2 text-sm text-hi placeholder:text-dim focus:outline-none transition-colors disabled:cursor-not-allowed",
              linkError ? "border-warn focus:border-warn" : "border-white/6 focus:border-active"
            )}
          />
          {linkError && (
            <p className="text-xs text-warn leading-relaxed">{linkError}</p>
          )}
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

        {/* Photos button */}
        <div>
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
              {t("garage.composer.photosMaxAdded")}
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
                ? t("garage.composer.addPhotosCount", { count: images.length, max: MAX_IMAGES })
                : t("garage.composer.addPhotos")}
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
          {submitting
            ? t("garage.composer.submitSubmitting")
            : disabled
              ? t("garage.composer.submitInProgress")
              : t("garage.composer.submitAnalyze")}
        </button>
      </form>
    </div>
  );
}
