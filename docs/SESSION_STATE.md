# Session State - 2026-01-17

## Právě dokončeno

### ✅ Chance Provider - KOMPLETNĚ DOKONČEN

**Datum:** 2026-01-17
**Status:** ✅ PRODUCTION READY

#### Implementované features

1. **ChanceScraper** (`src/Sazkomat.BettingProviders/Scrapers/ChanceScraper.cs`)
   - Extrakce lig z Chance REST API
   - Country mapping z českých názvů (sdíleno s Tipsport - stejná SAZKA Group)
   - Fallback dictionary + database-driven mappings

2. **ChanceJsonExtractor** (`src/Sazkomat.BettingProviders/Services/ChanceJsonExtractor.cs`)
   - Parsování JSON odpovědí z Chance API
   - Použití FlareSolverr persistent sessions (Cloudflare bypass)

3. **ChanceCountryScraper** (`src/Sazkomat.BettingProviders/Scrapers/ChanceCountryScraper.cs`)
   - Extrakce zemí z dostupných lig

4. **FlareSolverr Session Support** (`src/Sazkomat.BettingProviders/Services/FlareSolverrClient.cs`)
   - Nové metody: `CreateSessionAsync`, `DestroySessionAsync`, `GetWithSessionAsync`
   - Řeší problém Chance.cz vyžadující zachování browser session state

#### Stav dat pro Chance

- **36 zemí** načteno a namapováno
- **1 country_name_mapping** přidán (SAE → united-arab-emirates)
- Provider ID: `b0000000-0000-0000-0000-000000000002`

---

### ✅ Validace BetExplorer kódů - DOKONČENO

**Datum:** 2026-01-17

Backend nyní validuje existenci BetExplorer entit před uložením:

1. **CountryNameMappingEndpoints.cs**
   - POST/PATCH validuje že `betExplorerCode` existuje v countries

2. **LeagueNameMappingEndpoints.cs**
   - POST/PATCH validuje že `betExplorerSlug` existuje v leagues

---

### ✅ UI Vylepšení - DOKONČENO

**Datum:** 2026-01-17

1. **CountryNameMappingDialog.tsx**
   - `betExplorerCode` je nyní searchable combobox místo textového pole
   - Načítá země z API pro výběr

2. **Unmatched Leagues page**
   - Copy resolutions workflow (kopírování mapování mezi providery)
   - Preview + Execute endpoints

---

### ✅ Fuzzy Matching - VYPNUTO

**Datum:** 2026-01-17

- Fuzzy matching v `BetExplorerEnrichmentService` byl zakázán
- Důvod: Nespolehlivé výsledky (např. "1. turecká liga" matchoval "1. Lig" místo "Super Lig")
- Ligy bez přesné shody jdou do `unmatched_leagues` pro manuální řešení

---

### ✅ Cleanup country_name_mappings

**Datum:** 2026-01-17

- 50 neaktivních záznamů přesunuto z `country_name_mappings` do `unmatched_countries`
- `country_name_mappings` nyní obsahuje pouze aktivní mapovací pravidla

---

## Aktuální stav

- **Docker:** Běží (všechny kontejnery healthy)
- **Frontend:** http://localhost:3000
- **API:** http://localhost:3001
- **PostgreSQL:** localhost:3002
- **Redis:** localhost:3003
- **pgAdmin:** http://localhost:3004
- **FlareSolverr:** localhost:8191

## Betting Providers - Stav implementace

| Provider | Status | Poznámky |
|----------|--------|----------|
| BetExplorer | ✅ Reference | Zdroj pravdy pro ligy/země |
| Betano | ✅ Kompletní | Plně funkční scraper |
| Tipsport | ✅ Kompletní | Plně funkční scraper |
| Fortuna | ✅ Kompletní | Plně funkční scraper |
| Chance | ✅ Kompletní | Plně funkční scraper (FlareSolverr sessions) |

## Další kroky

1. Scan lig pro Chance
2. Případně další betting providers

---

## Předchozí dokončené práce

### ✅ Fortuna Provider - KOMPLETNĚ DOKONČEN

**Datum:** 2026-01-08
**Status:** ✅ PRODUCTION READY

- FortunaScraper s přímým API přístupem
- Country/League mapping
- LeagueProvider automatické vytváření

### ✅ Tipsport Provider - KOMPLETNĚ DOKONČEN

**Datum:** 2026-01-07
**Status:** ✅ PRODUCTION READY

- TipsportScraper s Playwright (Cloudflare bypass)
- Country mapping z českých názvů
- Backfill mechanismy

### ✅ Betano Provider - KOMPLETNĚ DOKONČEN

**Datum:** 2025-12-xx
**Status:** ✅ PRODUCTION READY

- BetanoScraper s Playwright
- Country/League mapping
- LeagueProvider automatické vytváření

---

## Porty (NESMÍ SE MĚNIT bez explicitního souhlasu)
- Frontend: 3000
- API: 3001
- PostgreSQL: 3002
- Redis: 3003
- pgAdmin: 3004
- FlareSolverr: 8191

---

**Poslední aktualizace:** 2026-01-17
**Build status:** ✅ SUCCESS
**Všichni betting providers implementováni:** ✅ ANO
