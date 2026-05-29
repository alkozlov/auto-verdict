"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";
import { LANGUAGES, type Locale } from "@/i18n/languages";

interface Props {
  className?: string;
}

export function LanguageSwitcher({ className }: Props) {
  const { i18n, t } = useTranslation();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const currentLanguage =
    LANGUAGES.find((language) => language.code === i18n.language) ?? LANGUAGES[0];

  useEffect(() => {
    if (!open) return;

    function closeOnOutsideClick(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", closeOnOutsideClick);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", closeOnOutsideClick);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [open]);

  function selectLanguage(locale: Locale) {
    void i18n.changeLanguage(locale);
    setOpen(false);
  }

  return (
    <div ref={rootRef} className={cn("relative", className)}>
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-label={t("language.switcher")}
        aria-haspopup="menu"
        aria-expanded={open}
        className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-white/10 bg-surface text-mid transition-colors hover:border-white/16 hover:text-hi"
      >
        <span aria-hidden="true" className="text-sm leading-none">
          {currentLanguage.flag}
        </span>
      </button>

      {open && (
        <div
          role="menu"
          aria-label={t("language.label")}
          className="absolute right-0 top-11 z-50 grid grid-cols-5 gap-1 rounded-lg border border-white/10 bg-[#0E1116] p-1 shadow-2xl"
        >
          {LANGUAGES.map((language) => {
            const selected = language.code === currentLanguage.code;
            return (
              <button
                key={language.code}
                type="button"
                role="menuitemradio"
                aria-checked={selected}
                aria-label={language.label}
                onClick={() => selectLanguage(language.code)}
                className={cn(
                  "flex h-9 w-9 items-center justify-center rounded-md text-sm transition-colors",
                  selected ? "bg-white/[0.06] text-hi" : "text-mid hover:bg-white/[0.04] hover:text-hi"
                )}
              >
                <span aria-hidden="true" className="text-base leading-none">
                  {language.flag}
                </span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
