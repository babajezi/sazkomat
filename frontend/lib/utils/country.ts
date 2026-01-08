import type { Country, LanguagePreference } from "@/lib/api/types";
import { LanguagePreference as LP } from "@/lib/api/types";

/**
 * Returns the localized country name based on user's language preference
 * @param country The country object
 * @param language User's language preference (defaults to Czech)
 * @returns The localized country name
 */
export function getCountryDisplayName(
  country: Country,
  language: LanguagePreference = LP.Czech
): string {
  if (language === LP.English) {
    return country.name;
  }
  // Czech: prefer Czech name, fallback to English
  return country.nameCs || country.name;
}

/**
 * Returns both names if available (for tooltips, etc.)
 * @param country The country object
 * @returns A string with both Czech and English names, or just one if only one is available
 */
export function getCountryFullName(country: Country): string {
  if (country.nameCs && country.nameCs !== country.name) {
    return `${country.nameCs} (${country.name})`;
  }
  return country.name;
}
