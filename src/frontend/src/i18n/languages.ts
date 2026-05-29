export const DEFAULT_LOCALE = "en";
export const LOCALE_STORAGE_KEY = "auto-verdict.locale";

export const LANGUAGES = [
  { code: "en", shortLabel: "EN", label: "English" },
  { code: "pl", shortLabel: "PL", label: "Polski" },
  { code: "de", shortLabel: "DE", label: "Deutsch" },
  { code: "uk", shortLabel: "UK", label: "Українська" },
  { code: "fr", shortLabel: "FR", label: "Français" },
] as const;

export type Locale = (typeof LANGUAGES)[number]["code"];

export const SUPPORTED_LOCALES = LANGUAGES.map((language) => language.code);

export function isLocale(value: string | undefined | null): value is Locale {
  return SUPPORTED_LOCALES.includes(value as Locale);
}
