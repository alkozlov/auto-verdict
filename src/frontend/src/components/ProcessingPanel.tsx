"use client";

import { useEffect, useState } from "react";
import { Check, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { useTranslation } from "react-i18next";

interface Props {
  hasLink: boolean;
  hasPhotos: boolean;
}

export function ProcessingPanel({ hasLink, hasPhotos }: Props) {
  const { t } = useTranslation();
  const steps = [
    t("garage.processing.readingNotes"),
    hasLink ? t("garage.processing.checkingOtomoto") : t("garage.processing.usingText"),
    hasPhotos ? t("garage.processing.reviewingPhotos") : t("garage.processing.continuingWithoutPhotos"),
    t("garage.processing.detectingMissing"),
    t("garage.processing.generatingRecommendation"),
  ];
  const [current, setCurrent] = useState(0);

  useEffect(() => {
    if (current >= steps.length - 1) return;
    const id = setTimeout(() => setCurrent((c) => c + 1), 2400);
    return () => clearTimeout(id);
  }, [current, steps.length]);

  return (
    <div
      className="animate-panel-in rounded-lg border border-white/6 bg-surface p-5 space-y-4"
      aria-live="polite"
      aria-label={t("garage.processing.ariaLabel")}
    >
      <div className="flex items-center gap-2">
        <Loader2 className="h-4 w-4 shrink-0 animate-spin text-brand" />
        <p className="text-sm font-medium text-hi">{t("garage.processing.title")}</p>
      </div>
      <ol className="space-y-2.5">
        {steps.map((step, i) => {
          const done = i < current;
          const active = i === current;
          return (
            <li
              key={step}
              className={cn(
                "flex items-center gap-2.5 text-sm transition-colors duration-300",
                done ? "text-ok" : active ? "text-mid" : "text-off"
              )}
            >
              {done ? (
                <Check className="h-3.5 w-3.5 shrink-0 text-ok" />
              ) : (
                <span
                  className={cn(
                    "h-3.5 w-3.5 shrink-0 rounded-full border transition-colors duration-300",
                    active ? "border-brand bg-brand-tint" : "border-subtle"
                  )}
                />
              )}
              {step}
            </li>
          );
        })}
      </ol>
    </div>
  );
}
