import type { League, LanguagePreference } from "@/lib/api/types";
import { LanguagePreference as LP } from "@/lib/api/types";

/**
 * Returns the localized league name based on user's language preference
 * @param league The league object
 * @param language User's language preference (defaults to Czech)
 * @returns The localized league name
 */
export function getLeagueDisplayName(
  league: League,
  language: LanguagePreference = LP.Czech
): string {
  if (language === LP.English) {
    return league.displayName || league.name;
  }
  // Czech: prefer Czech name, fallback to displayName/name
  return league.nameCs || league.displayName || league.name;
}

/**
 * Returns both names if available (for tooltips, etc.)
 * @param league The league object
 * @returns A string with both Czech and English names, or just one if only one is available
 */
export function getLeagueFullName(league: League): string {
  if (league.nameCs && league.nameCs !== league.displayName && league.nameCs !== league.name) {
    return `${league.nameCs} (${league.displayName || league.name})`;
  }
  return league.displayName || league.name;
}
