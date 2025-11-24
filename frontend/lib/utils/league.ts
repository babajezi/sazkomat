import type { League } from "@/lib/api/types";

/**
 * Returns the localized league name (Czech if available, otherwise English)
 * @param league The league object
 * @returns The localized league name
 */
export function getLeagueDisplayName(league: League): string {
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
