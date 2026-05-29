"use client";

import type { ReactNode } from "react";
import { useRef } from "react";
import GithubSlugger from "github-slugger";
import ReactMarkdown, { type Components } from "react-markdown";
import rehypeSanitize from "rehype-sanitize";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";

type Severity = "low" | "medium" | "high" | "unknown";

const SEVERITY_LABELS: Record<Severity, string[]> = {
  low: ["low", "niski", "niedrig", "faible", "низький"],
  medium: ["medium", "średni", "sredni", "mittel", "moyen", "середній"],
  high: ["high", "wysoki", "hoch", "élevé", "eleve", "високий"],
  unknown: ["unknown", "nieznany", "unbekannt", "inconnu", "невідомо"],
};

const SEVERITY_STYLES: Record<Severity, string> = {
  low: "border-ok/30 bg-ok-tint text-ok",
  medium: "border-warn/30 bg-warn-tint text-warn",
  high: "border-bad/30 bg-bad-tint text-bad",
  unknown: "border-dim/30 bg-surface-soft text-mid",
};

function flattenText(node: ReactNode): string {
  if (typeof node === "string" || typeof node === "number") return String(node);
  if (Array.isArray(node)) return node.map(flattenText).join("");
  return "";
}

function detectSeverity(value: string): Severity | null {
  const normalized = value
    .trim()
    .replace(/[🟢🟠🔴⚪]/gu, "")
    .replace(/\s+/g, " ")
    .toLowerCase();

  if (!normalized || normalized.length > 24) return null;

  for (const [severity, labels] of Object.entries(SEVERITY_LABELS)) {
    if (labels.includes(normalized)) return severity as Severity;
  }
  return null;
}

function SeverityBadge({ children }: { children: ReactNode }) {
  const text = flattenText(children).trim();
  const severity = detectSeverity(text);
  if (!severity) return <>{children}</>;

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold",
        SEVERITY_STYLES[severity]
      )}
    >
      {children}
    </span>
  );
}

export function ReportMarkdownViewer({ markdown }: { markdown: string }) {
  const slugger = useRef(new GithubSlugger());
  slugger.current.reset();

  const components: Components = {
    h1: ({ children }) => (
      <h1
        id={slugger.current.slug(flattenText(children))}
        className="mt-12 first:mt-0 scroll-mt-24 border-b border-white/8 pb-4 text-[28px] font-[750] leading-tight text-hi sm:text-[32px]"
      >
        {children}
      </h1>
    ),
    h2: ({ children }) => (
      <h2
        id={slugger.current.slug(flattenText(children))}
        className="mt-10 scroll-mt-24 text-[22px] font-[720] leading-tight text-hi sm:text-[26px]"
      >
        {children}
      </h2>
    ),
    h3: ({ children }) => (
      <h3
        id={slugger.current.slug(flattenText(children))}
        className="mt-8 scroll-mt-24 text-lg font-semibold leading-snug text-hi"
      >
        {children}
      </h3>
    ),
    h4: ({ children }) => (
      <h4 className="mt-6 text-base font-semibold text-hi">{children}</h4>
    ),
    p: ({ children }) => (
      <p className="mt-4 text-[15px] leading-7 text-mid sm:text-base sm:leading-8">
        <SeverityBadge>{children}</SeverityBadge>
      </p>
    ),
    table: ({ children }) => (
      <div className="mt-5 overflow-x-auto rounded-xl border border-white/8 bg-[#0D121A]">
        <table className="min-w-full border-collapse text-left text-sm">
          {children}
        </table>
      </div>
    ),
    thead: ({ children }) => <thead className="bg-white/[0.04]">{children}</thead>,
    th: ({ children }) => (
      <th className="border-b border-white/8 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-mid">
        {children}
      </th>
    ),
    td: ({ children }) => (
      <td className="max-w-[340px] border-t border-white/6 px-4 py-3 align-top leading-6 text-mid">
        <SeverityBadge>{children}</SeverityBadge>
      </td>
    ),
    ul: ({ children }) => (
      <ul className="mt-4 space-y-2 pl-1 text-[15px] leading-7 text-mid">
        {children}
      </ul>
    ),
    ol: ({ children }) => (
      <ol className="mt-4 list-decimal space-y-3 pl-6 text-[15px] leading-7 text-mid">
        {children}
      </ol>
    ),
    li: ({ children }) => (
      <li className="ml-4 pl-1 marker:text-brand marker:font-semibold">{children}</li>
    ),
    input: ({ checked, type }) => {
      if (type !== "checkbox") return null;
      return (
        <input
          type="checkbox"
          checked={checked}
          readOnly
          disabled
          className="mr-2 h-4 w-4 rounded border-white/20 bg-surface-soft align-[-2px] accent-brand"
        />
      );
    },
    a: ({ href, children }) => (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className="font-medium text-brand underline decoration-brand/30 underline-offset-4 transition-colors hover:text-brand-hi"
      >
        {children}
      </a>
    ),
    blockquote: ({ children }) => (
      <blockquote className="mt-5 border-l-4 border-brand/40 bg-brand-tint/40 px-4 py-3 text-mid">
        {children}
      </blockquote>
    ),
    hr: () => <hr className="my-10 border-white/10" />,
    strong: ({ children }) => <strong className="font-semibold text-hi">{children}</strong>,
  };

  return (
    <article className="report-viewer text-mid">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeSanitize]}
        components={components}
      >
        {markdown}
      </ReactMarkdown>
    </article>
  );
}
