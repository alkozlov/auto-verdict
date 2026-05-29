import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import en from "./locales/en.json";
import pl from "./locales/pl.json";
import de from "./locales/de.json";
import uk from "./locales/uk.json";
import fr from "./locales/fr.json";
import {
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  type Locale,
  isLocale,
} from "./languages";

const resources = {
  en: { translation: en },
  pl: { translation: pl },
  de: { translation: de },
  uk: { translation: uk },
  fr: { translation: fr },
};

function getConfiguredDefaultLocale(): Locale {
  return isLocale(import.meta.env.VITE_DEFAULT_LOCALE)
    ? import.meta.env.VITE_DEFAULT_LOCALE
    : DEFAULT_LOCALE;
}

function getStoredLocale(): Locale | null {
  if (typeof window === "undefined") return null;
  const storedLocale = window.localStorage.getItem(LOCALE_STORAGE_KEY);
  return isLocale(storedLocale) ? storedLocale : null;
}

function getInitialLocale(): Locale {
  return getStoredLocale() ?? getConfiguredDefaultLocale();
}

i18n.use(initReactI18next).init({
  resources,
  lng: getInitialLocale(),
  fallbackLng: DEFAULT_LOCALE,
  supportedLngs: Object.keys(resources),
  interpolation: {
    escapeValue: false,
  },
  returnNull: false,
});

function persistLocale(locale: string) {
  if (!isLocale(locale) || typeof window === "undefined") return;
  window.localStorage.setItem(LOCALE_STORAGE_KEY, locale);
  document.documentElement.lang = locale;
}

persistLocale(i18n.language);
i18n.on("languageChanged", persistLocale);

export { i18n };
