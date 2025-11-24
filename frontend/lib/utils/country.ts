import type { Country } from "@/lib/api/types";

/**
 * Returns the localized country name (Czech if available, otherwise English)
 * @param country The country object
 * @returns The localized country name
 */
export function getCountryDisplayName(country: Country): string {
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
