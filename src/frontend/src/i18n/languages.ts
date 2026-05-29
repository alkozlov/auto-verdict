export const DEFAULT_LOCALE = "en";
export const LOCALE_STORAGE_KEY = "auto-verdict.locale";

export const LANGUAGES = [
  { code: "en", shortLabel: "EN", label: "English", flag: "🇬🇧" },
  { code: "pl", shortLabel: "PL", label: "Polski", flag: "🇵🇱" },
  { code: "de", shortLabel: "DE", label: "Deutsch", flag: "🇩🇪" },
  { code: "uk", shortLabel: "UK", label: "Українська", flag: "🇺🇦" },
  { code: "fr", shortLabel: "FR", label: "Français", flag: "🇫🇷" },
] as const;

export type Locale = (typeof LANGUAGES)[number]["code"];

export const SUPPORTED_LOCALES = LANGUAGES.map((language) => language.code);

export function isLocale(value: string | undefined | null): value is Locale {
  return SUPPORTED_LOCALES.includes(value as Locale);
}
