# API.md - Kompletní API dokumentace

Base URL: `http://localhost:3001`

## Autentizace

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/auth/register | Registrace nového uživatele |
| POST | /api/auth/login | Přihlášení (vrací JWT) |
| POST | /api/auth/google | Google OAuth login |
| GET | /api/auth/me | Aktuální uživatel (vyžaduje auth) |
| PATCH | /api/auth/me/language | Změna jazyka |
| POST | /api/auth/logout | Odhlášení |

---

## Konfigurace

### Sporty

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/sports | Seznam sportů |
| PATCH | /api/config/sports/{id} | Aktualizace sportu |
| GET | /api/config/sports/{sportId}/providers | Providers pro sport |
| POST | /api/config/sports/{sportId}/providers | Vytvoření sport-provider mapování |
| PATCH | /api/config/sports/{sportId}/providers/{providerId} | Aktualizace mapování |
| DELETE | /api/config/sports/{sportId}/providers/{providerId} | Smazání mapování |

### Země

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/countries | Seznam zemí (?sportId=) |
| POST | /api/config/countries | Vytvoření země |
| PATCH | /api/config/countries/{id} | Aktualizace země |
| DELETE | /api/config/countries/{id} | Smazání země |
| PATCH | /api/config/countries/{countryId}/providers/{providerId} | Toggle sync status |

### Ligy

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/leagues | Seznam lig (?sportId, ?countryId, ?onlyEnabled) |
| POST | /api/config/leagues | Vytvoření ligy |
| PATCH | /api/config/leagues/{id} | Aktualizace ligy |
| DELETE | /api/config/leagues/{id} | Smazání ligy (?ignoreInProvider=true → před smazáním označí provider_leagues jako Ignored, resetuje IsImported/LeagueId/ImportedAt a smaže související kola a zápasy) |
| PATCH | /api/config/leagues/{leagueId}/providers/{providerId} | Toggle sync status |
| GET | /api/config/leagues/{leagueId}/betting-availability | Betting providers pro ligu |
| POST | /api/config/leagues/{leagueId}/validate | Validace všech historických sezón ligy |
| POST | /api/config/leagues/{leagueId}/lock | Zamčení všech validních sezón ligy |
| POST | /api/config/leagues/{leagueId}/unlock | Odemčení všech zamčených sezón ligy |

### Sezóny

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/seasons | Seznam sezón |
| GET | /api/config/seasons/available | Dostupné sezóny pro ligu (?leagueId) |
| GET | /api/config/league-seasons | League-season vztahy (?leagueId) - response obsahuje isIgnored, ignoredAt, ignoredNote |
| PATCH | /api/config/seasons/league-seasons/{id}/sync-enabled | Enable/disable sync |
| POST | /api/config/seasons/league-seasons/{id}/validate | Validace jednotlivé sezóny |
| POST | /api/config/seasons/league-seasons/{id}/lock | Zamčení sezóny |
| POST | /api/config/seasons/league-seasons/{id}/unlock | Odemčení sezóny |
| PATCH | /api/config/seasons/league-seasons/{id}/ignore | Označení/odoznačení sezóny jako ignorované (body: { ignored, note? }) |

### Data Providers

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/providers | Seznam providerů (?onlyActive) |
| GET | /api/config/providers/{id} | Detail providera |
| POST | /api/config/providers | Vytvoření providera |
| PATCH | /api/config/providers/{id} | Aktualizace providera |
| DELETE | /api/config/providers/{id} | Smazání providera |
| PATCH | /api/config/providers/{providerId}/credentials | Aktualizace credentials |
| PATCH | /api/config/providers/{providerId}/configuration | Aktualizace konfigurace |
| GET | /api/config/providers/{providerId}/sync-status | Sync status |
| POST | /api/config/providers/{providerId}/sync-leagues | Sync dostupnosti lig |
| POST | /api/config/providers/auto-enable-betexplorer | Auto-enable BetExplorer |
| GET | /api/config/providers/betting | Seznam betting providerů |

### Provider Mappings

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/providers/league-mappings | League-provider mapování |
| GET | /api/config/providers/league-mappings/{id} | Detail mapování |
| POST | /api/config/providers/league-mappings | Vytvoření mapování |
| PATCH | /api/config/providers/league-mappings/{id} | Aktualizace mapování |
| POST | /api/config/providers/league-mappings/{id}/activate | Aktivace mapování |
| DELETE | /api/config/providers/league-mappings/{id} | Smazání mapování |
| GET | /api/config/providers/country-mappings | Country-provider mapování |
| GET | /api/config/providers/country-mappings/{id} | Detail mapování |
| POST | /api/config/providers/country-mappings | Vytvoření mapování |
| PATCH | /api/config/providers/country-mappings/{id} | Aktualizace mapování |
| DELETE | /api/config/providers/country-mappings/{id} | Smazání mapování |
| DELETE | /api/config/country-providers/by-provider | Smazání všech country mappings pro providera |
| DELETE | /api/config/league-providers/by-provider | Smazání všech league mappings pro providera |

### Provider Logos

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/config/providers/{id}/logo | Upload loga |
| GET | /api/config/providers/{id}/logo | Stažení loga (?size=sm\|md\|lg) |
| DELETE | /api/config/providers/{id}/logo | Smazání loga |

---

## Scan operace

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/scan/countries | Scan zemí z providera (async) |
| POST | /api/scan/leagues | Scan lig z providera (async) |
| POST | /api/scan/seasons | Scan sezón z providera (async) |
| POST | /api/scan/full | Kombinovaný scan zemí + lig |
| POST | /api/scan/apply-country-mappings | Aplikace country name mappings |
| POST | /api/scan/backfill-provider-leagues | Backfill provider_leagues |
| POST | /api/scan/backfill-provider-countries | Backfill provider_countries |
| POST | /api/scan/backfill-league-providers | Backfill LeagueProvider mappings |

### Unmatched Leagues (scan)

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/scan/unmatched-leagues | Seznam unmatched lig (?resolved, ?providerId) |
| POST | /api/scan/unmatched-leagues/{id}/resolve | Resolve přes BetExplorer slug |
| POST | /api/scan/unmatched-leagues/{id}/ignore | Označit jako ignorované |
| DELETE | /api/scan/unmatched-leagues/{id} | Smazat z fronty |

---

## Import operace

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/import/historical | Spuštění historického importu |
| GET | /api/import/jobs/{jobId} | Status import jobu |
| GET | /api/import/stats | Statistiky importu (?leagueId) |
| GET | /api/import/dashboard | Dashboard statistiky |
| GET | /api/import/matches | Seznam zápasů (filtry: leagueId, season, roundNumber, result, dateFrom, dateTo, teamName) |
| GET | /api/import/rounds | Seznam kol (?season, ?leagueId) |
| GET | /api/import/leagues/available | Dostupné ligy pro import |
| GET | /api/import/leagues/{leagueId}/seasons/available | Dostupné sezóny z BetExploreru |
| POST | /api/import/countries | Import zemí z cache |
| POST | /api/import/leagues | Import lig z cache |
| POST | /api/import/seasons | Import sezón z cache |
| GET | /api/import/seasons/imported | Sezóny s importovanými koly |
| GET | /api/import/rounds/available | Dostupná čísla kol pro filtry |
| GET | /api/import/cache/stats | Cache vs imported statistiky |

---

## Synchronizace

### Workflow

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/sync/workflow/state | Aktuální stav workflow |
| POST | /api/sync/workflow/confirm-countries | Potvrzení výběru zemí |
| POST | /api/sync/workflow/confirm-leagues | Potvrzení výběru lig |
| POST | /api/sync/workflow/reset | Reset workflow |

### Sync operace

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/sync/countries | Sync zemí z providera |
| POST | /api/sync/leagues | Sync lig z providera |
| POST | /api/sync/seasons | Sync sezón (3-letý limit) |
| POST | /api/sync/seasons/global | Global season scan (bez limitu) |
| POST | /api/sync/seasons/{leagueId} | Sync sezón pro konkrétní ligu |
| POST | /api/sync/seasons/detect-current | Detekce aktuálních sezón |
| POST | /api/sync/seasons/data | Sync dat pro všechny sync-enabled sezóny |
| POST | /api/sync/seasons/data/{leagueId}/{seasonId} | Sync dat pro jednu sezónu |
| POST | /api/sync/multi-sport | Multi-sport sync z betting providera |
| POST | /api/sync/league/{leagueId}/season-data | Sync dat pro všechny sezóny ligy |
| POST | /api/sync/league/{leagueId}/seasons-list | Refresh seznamu sezón |
| GET | /api/sync/status | Aktuální sync status |

---

## Live Sync

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/livesync/rounds | Live sync kol |
| POST | /api/livesync/rounds/{roundId} | Live sync konkrétního kola |
| GET | /api/livesync/stats | Live sync statistiky (?providerId) |

---

## Background Jobs

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/jobs/{jobId} | Status jobu |
| GET | /api/jobs/recent | Poslední joby (?providerId, ?count) |
| POST | /api/jobs/scan | Zařazení scan jobu |
| POST | /api/jobs/import | Zařazení import jobu |
| POST | /api/jobs/livesync | Zařazení live sync jobu |
| DELETE | /api/jobs/{jobId} | Zrušení jobu |

---

## Unmatched Leagues

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/unmatched-leagues | Seznam (?providerId, ?unresolvedOnly) |
| GET | /api/unmatched-leagues/{id} | Detail |
| POST | /api/unmatched-leagues/{id}/resolve/map | Resolve mapováním na existující ligu |
| POST | /api/unmatched-leagues/{id}/resolve/create-from-betexplorer | Vytvoření nové ligy z BetExploreru |
| POST | /api/unmatched-leagues/{id}/resolve/ignore | Označit jako ignorované |
| POST | /api/unmatched-leagues/{id}/resolve/unavailable | Označit jako nedostupné |
| POST | /api/unmatched-leagues/{id}/unresolve | Zrušit resolution |
| DELETE | /api/unmatched-leagues/{id} | Smazat záznam |
| GET | /api/unmatched-leagues/{id}/mapping | Detail mapování |
| GET | /api/unmatched-leagues/stats | Statistiky |
| GET | /api/unmatched-leagues/suggestions/{id} | Návrhy pro mapování |
| POST | /api/unmatched-leagues/copy-resolutions/preview | Preview kopírování resolutions |
| POST | /api/unmatched-leagues/copy-resolutions/execute | Kopírování resolutions |
| GET | /api/unmatched-leagues/{id}/global-rule/preview | Preview globálního pravidla |
| POST | /api/unmatched-leagues/{id}/global-rule/create | Vytvoření globálního pravidla |

---

## Unmatched Countries

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/unmatched-countries | Seznam (?providerId, ?unresolvedOnly) |
| GET | /api/unmatched-countries/{id} | Detail |
| POST | /api/unmatched-countries/{id}/resolve/map | Resolve mapováním |
| POST | /api/unmatched-countries/{id}/resolve/ignore | Označit jako ignorované |
| POST | /api/unmatched-countries/{id}/resolve/unavailable | Označit jako nedostupné |
| POST | /api/unmatched-countries/{id}/unresolve | Zrušit resolution |
| DELETE | /api/unmatched-countries/{id} | Smazat záznam |
| GET | /api/unmatched-countries/stats | Statistiky (?providerId) |
| GET | /api/unmatched-countries/suggestions/{id} | Návrhy pro mapování |

---

## Name Mappings

### League Name Mappings

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/mappings | Seznam (?providerCode, ?countryCode, ?isActive) |
| GET | /api/mappings/{id} | Detail |
| POST | /api/mappings | Vytvoření |
| PATCH | /api/mappings/{id} | Aktualizace |
| DELETE | /api/mappings/{id} | Smazání |
| POST | /api/mappings/{id}/toggle | Toggle IsActive |

### Country Name Mappings

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/country-mappings | Seznam (?providerCode, ?isActive) |
| GET | /api/country-mappings/{id} | Detail |
| POST | /api/country-mappings | Vytvoření |
| PATCH | /api/country-mappings/{id} | Aktualizace |
| DELETE | /api/country-mappings/{id} | Smazání |
| POST | /api/country-mappings/{id}/toggle | Toggle IsActive |
| POST | /api/country-mappings/test | Test pattern matching |

---

## Provider Cache

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/provider-cache/countries | Cached countries (?providerId) |
| GET | /api/provider-cache/leagues | Cached leagues (?providerId) |
| GET | /api/provider-cache/seasons | Cached seasons (?providerId) |
| DELETE | /api/provider-cache/countries | Smazání cached countries |
| DELETE | /api/provider-cache/leagues | Smazání cached leagues |
| DELETE | /api/provider-cache/seasons | Smazání cached seasons |
| GET | /api/provider-cache/countries/{id}/mapping | Mapping detail |
| GET | /api/provider-cache/leagues/{id}/mapping | Mapping detail |
| PATCH | /api/provider-cache/leagues/{id}/apply-mapping | Aplikace manuálního mapování |

---

## Scraper Recipes

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/recipes | Seznam recipes |
| GET | /api/recipes/{id} | Detail recipe |
| POST | /api/recipes | Vytvoření recipe |
| PUT | /api/recipes/{id} | Aktualizace recipe |
| DELETE | /api/recipes/{id} | Smazání recipe |
| GET | /api/recipes/stats | Statistiky recipes |
| POST | /api/recipes/{id}/test | Test recipe na konkrétní ligu/sezónu |
| GET | /api/recipes/by-provider/{provider}/{pageType} | Recipes dle providera |

---

## BetExplorer

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/betexplorer/leagues/{countryCode} | Dostupné ligy pro zemi (?forceRefresh) |

---

## Debug

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/debug/scraper/execute | Spuštění debug akcí |
| GET | /api/debug/screenshots | Seznam screenshotů |
| GET | /api/debug/screenshots/{name} | Stažení screenshotu |
| DELETE | /api/debug/screenshots | Smazání všech screenshotů |

---

## Databáze

| Method | Endpoint | Popis |
|--------|----------|-------|
| DELETE | /api/database/reset | Reset všech dat (POZOR!) |
| POST | /api/database/seed | Seed initial dat |
| POST | /api/database/reset-and-seed | Reset + seed (POZOR!) |
| POST | /api/database/reset/all | Reset včetně konfigurace |
| POST | /api/database/reset/data-only | Reset pouze importovaných dat |
| GET | /api/database/counts | Počty záznamů |
| GET | /api/database/counts/bindings | Počty bindings dle providera |
| POST | /api/database/reset/selective | Selektivní reset |
| POST | /api/database/reset/bindings/{providerCode} | Reset bindings pro providera |

---

## Import/Export

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/config/export/preview | Preview exportu |
| POST | /api/config/export | Export konfigurace do JSON |
| POST | /api/config/import/validate | Validace import dat |
| POST | /api/config/import | Import konfigurace |

---

## Admin

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /api/admin/users | Seznam uživatelů |
| GET | /api/admin/users/pending | Uživatelé čekající na schválení |
| POST | /api/admin/users/{id}/approve | Schválení uživatele |
| POST | /api/admin/users/{id}/reject | Zamítnutí uživatele |
| DELETE | /api/admin/users/{id} | Smazání uživatele |
| PATCH | /api/admin/users/{id} | Aktualizace uživatele |

---

## Analytics

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/analytics/execute | Spustí ad-hoc ViewSpec |
| GET | /api/analytics/metadata | Dostupné dimenze, metriky, sloupce |
| GET | /api/analytics/views | Seznam uložených pohledů |
| GET | /api/analytics/views/{id} | Detail pohledu |
| POST | /api/analytics/views | Vytvoří pohled |
| PUT | /api/analytics/views/{id} | Aktualizuje pohled |
| DELETE | /api/analytics/views/{id} | Smaže pohled |
| POST | /api/analytics/views/{id}/execute | Spustí uložený pohled |
| POST | /api/analytics/views/{id}/favorite | Toggle oblíbený |

---

## External Scrapers

| Method | Endpoint | Popis |
|--------|----------|-------|
| POST | /api/tipsport/leagues | Příjem lig z externího Tipsport scraperu |

---

## Health

| Method | Endpoint | Popis |
|--------|----------|-------|
| GET | /health | Health check |
| GET | /hangfire | Hangfire Dashboard |

---

## Poznámky

- Všechny endpointy vrací JSON
- Autentizované endpointy vyžadují JWT token: `Authorization: Bearer <token>`
- Admin endpointy vyžadují admin roli
- Background joby používají Hangfire
- Datetime hodnoty jsou v UTC
- ID jsou UUID (GUID)
- Enum hodnoty jsou serializovány jako **stringy** (ne čísla)

---

**Poslední aktualizace:** 2026-02-19
