# Synchronizační workflow

## 4-Step Workflow

Pro robustní zpracování dat z providerů:

### Krok 1: SCAN (Load to Cache)

- **Endpoint**: `/api/scan/{countries|leagues|seasons}`
- **Účel**: Načte data z providera do cache tabulek
- **Tabulky**: `provider_countries`, `provider_leagues`, `provider_seasons`
- **Výhody**:
  - Oddělení načítání dat od importu
  - Možnost náhledu před importem
  - Zachování audit trail (data se nikdy nemažou)

### Krok 2: IMPORT (Cache to Config)

- **Service**: `ImportService`
- **Účel**: Importuje vybraná data z cache do hlavních konfiguračních tabulek
- **Logika**:
  - Kontroluje duplicity přes `CountryProvider` a `LeagueProvider` mapování
  - Vytváří/aktualizuje entity v configuration schématu
  - Zachovává referenční integritu

### Krok 3: DETECT CURRENT SEASONS

- **Endpoint**: `POST /api/sync/seasons/detect-current`
- **Účel**: Auto-detekce aktuálních sezón pomocí provider patterns
- **Logika**:
  ```
  patterns = ["2025", "2025-2026"]  // z DataProvider.CurrentSeasonPatterns
  isCurrent = patterns.Contains(season.Name)
  syncMode = isCurrent ? Current : Historical
  ```

### Krok 4: SEASON DATA SYNC

- **Endpoint**: `POST /api/sync/seasons/data`
- **Účel**: Sync kol a zápasů pro označené sezóny
- **Granulární kontrola**: Na úrovni `LeagueSeason`, ne globální `Season`

#### SyncMode enum

| Mode | Chování |
|------|---------|
| **Historical** | Jednorázový import. Přeskočí pokud `HasData=true` (úspora bandwidth) |
| **Current** | Průběžné aktualizace. Vždy synchronizuje pro získání nejnovějších výsledků |

#### Skip Logic

```csharp
if (!forceUpdate && leagueSeason.SyncMode == Historical && leagueSeason.HasData)
{
    // Skip - historická sezóna už má data
}
```

#### LeagueSeason fields

- `SyncEnabled` - toggle pro každou sezónu
- `IsCurrent` - auto-detected z provider patterns
- `SyncMode` - Historical nebo Current
- `LastDataSyncAt` - timestamp posledního sync

---

## Scraping Recipes (Adaptive Fallback)

Systém konfigurovatelných "receptů" pro web scraping s automatickým fallbackem.

### Princip

1. Načti recepty seřazené dle priority
2. Pokud `LeagueSeason.LastSuccessfulRecipeId` existuje → zkus první
3. Loop: zkus recept → success? → break : další
4. Ulož který recept fungoval

### Default Recipes

| Priority | Name | Popis |
|----------|------|-------|
| 1 | BetExplorer Full Workflow | Sort + Show more loop (10 iterací) |
| 2 | BetExplorer Sort Only | Pouze sort, bez Show more |
| 3 | BetExplorer Direct | Přímá navigace |
| 4 | BetExplorer URL Sort | URL parametr `?s=r` |

### Recipe Actions

- `navigate` - Navigace na URL s variable substitution (`{baseUrl}`, `{season}`)
- `click` - Klik na element (CSS selector)
- `wait` - Čekání (ms)
- `waitForLoadState` - Playwright load state
- `extractHtml` - Extrakce HTML

### LeagueSeason tracking

- `LastSuccessfulRecipeId` - který recept fungoval
- `LastRecipeTestedAt` - kdy byl testován

---

## Validace a zamykání sezón

Workflow pro finalizaci historických dat:

### Účel

- **Validace** - kontrola kvality dat před uzamčením
- **Zamčení** - označení sezóny jako finální (nelze synchronizovat)
- **Odemčení** - umožnění zpětných úprav

### Validační pravidla

| Pravidlo | Závažnost | Podmínka |
|----------|-----------|----------|
| Bez dat | Error | `SyncMode == Historical && !HasData` |
| Chyba parsování | Error | `NoDataReason == ParsingError` |
| Síťová chyba | Error | `NoDataReason == NetworkError` |
| Bez receptu | Warning | `HasData && LastSuccessfulRecipeId == null` |
| Neobvyklý počet zápasů | Warning | `MatchesCount` mimo 50-200% průměru ligy |
| Neobvyklý počet kol | Warning | `RoundsCount` mimo 50-200% průměru ligy |
| Stránka neexistuje | Warning | `NoDataReason == PageNotFound` (může být legitimní) |

### Logika zamykání

- `CanBeLocked = true` pokud žádné **Error** issues
- **Warning** issues umožní zamknout s upozorněním
- Zamčená sezóna **NELZE synchronizovat** (sync endpoint vrátí chybu)

### API Endpointy

| Metoda | Endpoint | Popis |
|--------|----------|-------|
| POST | `/api/config/seasons/league-seasons/{id}/validate` | Spustí validaci |
| POST | `/api/config/seasons/league-seasons/{id}/lock` | Zamkne sezónu |
| POST | `/api/config/seasons/league-seasons/{id}/unlock` | Odemkne sezónu |

### LeagueSeason fields

- `IsLocked` - zda je sezóna zamčená
- `LockedAt` - kdy byla zamčena
- `LastValidatedAt` - kdy byla naposledy validována

### Sync blokování

```csharp
if (leagueSeason.IsLocked)
{
    return Result.Failure("Season is locked and cannot be synced");
}
```

---

## Auto-Activation (Betting Providers)

Pro betting providery existuje speciální auto-aktivační logika:

1. **Problém**: Země začínají jako neaktivní (`IsActive = false`)
2. **Řešení**: Když ScanService aktivuje zemi, automaticky vytvoří i `CountryProvider` mapping
3. **Důvod**: Bez `CountryProvider` mappingu by se země neskenovaly v budoucích league scans

---

## Job Queue (Hangfire)

- Všechny operace běží asynchronně
- Tracking přes `sync_jobs` tabulku
- Status: `Pending` → `Running` → `Completed` / `Failed`
- Monitoring: `/hangfire` dashboard nebo `/api/jobs/recent`
