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
        <FlagIcon locale={currentLanguage.code} />
      </button>

      {open && (
        <div
          role="menu"
          aria-label={t("language.label")}
          className="absolute right-0 top-11 z-50 flex w-11 flex-col gap-1 rounded-lg border border-white/10 bg-[#0E1116] p-1 shadow-2xl"
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
                  "flex h-9 w-9 shrink-0 items-center justify-center rounded-md text-sm transition-colors",
                  selected ? "bg-white/[0.06] text-hi" : "text-mid hover:bg-white/[0.04] hover:text-hi"
                )}
              >
                <FlagIcon locale={language.code} />
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

function FlagIcon({ locale }: { locale: Locale }) {
  return (
    <span
      aria-hidden="true"
      className="relative block h-4 w-6 overflow-hidden rounded-[2px] shadow-[0_0_0_1px_rgba(255,255,255,0.18)]"
    >
      {locale === "en" && <UnitedKingdomFlag />}
      {locale === "pl" && <PolandFlag />}
      {locale === "de" && <GermanyFlag />}
      {locale === "uk" && <UkraineFlag />}
      {locale === "fr" && <FranceFlag />}
    </span>
  );
}

function UnitedKingdomFlag() {
  return (
    <svg viewBox="0 0 60 40" className="h-full w-full" focusable="false">
      <rect width="60" height="40" fill="#012169" />
      <path d="M0 0 60 40M60 0 0 40" stroke="#fff" strokeWidth="8" />
      <path d="M0 0 60 40M60 0 0 40" stroke="#C8102E" strokeWidth="4" />
      <path d="M30 0v40M0 20h60" stroke="#fff" strokeWidth="14" />
      <path d="M30 0v40M0 20h60" stroke="#C8102E" strokeWidth="8" />
    </svg>
  );
}

function PolandFlag() {
  return (
    <svg viewBox="0 0 3 2" className="h-full w-full" focusable="false">
      <rect width="3" height="1" fill="#fff" />
      <rect y="1" width="3" height="1" fill="#DC143C" />
    </svg>
  );
}

function GermanyFlag() {
  return (
    <svg viewBox="0 0 5 3" className="h-full w-full" focusable="false">
      <rect width="5" height="1" fill="#000" />
      <rect y="1" width="5" height="1" fill="#DD0000" />
      <rect y="2" width="5" height="1" fill="#FFCE00" />
    </svg>
  );
}

function UkraineFlag() {
  return (
    <svg viewBox="0 0 3 2" className="h-full w-full" focusable="false">
      <rect width="3" height="1" fill="#0057B7" />
      <rect y="1" width="3" height="1" fill="#FFD700" />
    </svg>
  );
}

function FranceFlag() {
  return (
    <svg viewBox="0 0 3 2" className="h-full w-full" focusable="false">
      <rect width="1" height="2" fill="#0055A4" />
      <rect x="1" width="1" height="2" fill="#fff" />
      <rect x="2" width="1" height="2" fill="#EF4135" />
    </svg>
  );
}
