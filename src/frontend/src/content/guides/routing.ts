import { DEFAULT_LOCALE, isLocale, type Locale } from "@/i18n/languages";
import { localesForSlug } from "./registry";

export const ORIGIN = "https://autoverdict.app";

/** English lives at /guides (no prefix); other locales at /<locale>/guides. */
export function localePrefix(locale: Locale): string {
  return locale === DEFAULT_LOCALE ? "" : `/${locale}`;
}

export function guidePath(slug: string, locale: Locale): string {
  return `${localePrefix(locale)}/guides/${slug}`;
}

export function guidesIndexPath(locale: Locale): string {
  return `${localePrefix(locale)}/guides`;
}

/**
 * Validate a route :locale param. Returns the Locale for a valid non-default
 * prefix, or null when the param should redirect to the English canonical
 * (missing-but-default "en" prefix, or an unsupported value).
 */
export function localeFromParam(param: string | undefined): Locale | null {
  if (param === undefined) return DEFAULT_LOCALE;
  if (isLocale(param) && param !== DEFAULT_LOCALE) return param;
  return null;
}

/** hreflang alternates for a guide, across every locale that has a real variant. */
export function guideAlternates(slug: string): { locale: string; path: string }[] {
  return localesForSlug(slug).map((locale) => ({
    locale,
    path: guidePath(slug, locale),
  }));
}
