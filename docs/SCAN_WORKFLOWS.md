# Scan Workflows Documentation

Tento dokument popisuje workflow pro skenování dat z providerů do systému Sazkomat.

## Přehled

Systém podporuje dva typy providerů:
1. **Scraping Providery** (BetExplorer) - primární zdroj dat
2. **Betting Providery** (Betano, Fortuna) - sekundární zdroje, data se mapují na BetExplorer

## 1. Scan Zemí (Country Scan)

### 1.1 BetExplorer (Scraping Provider)

**Flow:**
1. Scraper načte všechny země z BetExploreru
2. Pro každou zemi se vytvoří `ProviderCountry` záznam (cache)
3. Země jsou připraveny pro další zpracování

**Tabulky:**
- `data_import.provider_countries` - cache skenovaných zemí

### 1.2 Betano (Betting Provider)

**Flow:**
1. Scraper načte všechny regiony z Betana
2. Pro každý region se pokusí napárovat na číselník zemí z BetExploreru:
   - **Krok 1:** Manuální mapování (`CountryNameMapping` s `IsActive=true`)
   - **Krok 2:** Shoda podle ISO kódu
   - **Krok 3:** Shoda podle provider kódu
3. Pokud se **podaří napárovat**:
   - Vytvoří se `ProviderCountry` záznam s vazbou na `configuration.countries`
   - Vytvoří se `CountryProvider` mapping pro budoucí scan lig
4. Pokud se **nepodaří napárovat**:
   - Vytvoří se `CountryNameMapping` s `IsActive=false` pro manuální review
   - **Nevytváří se** `ProviderCountry` záznam!

**Tabulky:**
- `data_import.provider_countries` - cache skenovaných zemí (jen napárované)
- `data_import.country_name_mappings` - manuální mapování názvů
- `configuration.country_providers` - vazba země ↔ provider

### 1.3 Zpracování Manuálních Mapování

Po manuální úpravě `CountryNameMapping` (nastavení `BetExplorerCode` a `IsActive=true`):

1. Klikni na tlačítko **"Zpracovat mapování (Betano)"** na stránce `/country-mappings`
2. Systém projde všechna aktivní mapování se správně nastaveným `BetExplorerCode`
3. Pro každé mapování vytvoří:
   - `ProviderCountry` záznam
   - `CountryProvider` mapping

**API Endpoint:** `POST /api/scan/apply-country-mappings`

---

## 2. Scan Lig (League Scan)

### 2.1 BetExplorer (Scraping Provider)

**Flow:**
1. Pro každou zemi v `ProviderCountry` cache
2. Scraper načte ligy z BetExploreru
3. Vytvoří se `ProviderLeague` záznamy

### 2.2 Betano (Betting Provider)

**Flow:**
1. Pro každou zemi s aktivním `CountryProvider` mappingem
2. Scraper načte ligy z Betana
3. Každá liga se **enrichuje** pomocí BetExplorer dat:
   - Fuzzy matching názvu ligy
   - Manuální `LeagueNameMapping`
   - Výsledkem je liga s BetExplorer slugem
4. Pouze enrichnuté ligy se ukládají do `ProviderLeague`
5. Pokud se v zemi najdou ligy, země se automaticky aktivuje (`IsActive=true`)

**Tabulky:**
- `data_import.provider_leagues` - cache skenovaných lig
- `data_import.league_name_mappings` - manuální mapování lig

---

## 3. Scan Sezón (Season Scan)

**Flow:**
1. Pro každou ligu v `ProviderLeague` cache
2. Scraper načte dostupné sezóny
3. Vytvoří se `ProviderSeason` záznamy

**Tabulky:**
- `data_import.provider_seasons` - cache skenovaných sezón

---

## Databázové Tabulky

### Cache Tabulky (data_import schema)

| Tabulka | Účel |
|---------|------|
| `provider_countries` | Cache skenovaných zemí |
| `provider_leagues` | Cache skenovaných lig |
| `provider_seasons` | Cache skenovaných sezón |
| `country_name_mappings` | Manuální mapování názvů zemí |
| `league_name_mappings` | Manuální mapování názvů lig |
| `sync_jobs` | Historie a status scan jobů |

### Konfigurační Tabulky (configuration schema)

| Tabulka | Účel |
|---------|------|
| `countries` | Číselník zemí (z BetExploreru) |
| `country_providers` | Vazba země ↔ provider |
| `leagues` | Číselník lig |
| `league_providers` | Vazba liga ↔ provider |

---

## API Endpointy

### Scan Endpointy

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/scan/countries` | POST | Spustí scan zemí (async) |
| `/api/scan/leagues` | POST | Spustí scan lig (async) |
| `/api/scan/seasons` | POST | Spustí scan sezón (async) |
| `/api/scan/apply-country-mappings` | POST | Zpracuje manuální mapování zemí |

### Job Monitoring

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/jobs/{jobId}` | GET | Status konkrétního jobu |
| `/api/jobs/recent` | GET | Seznam posledních jobů |
| `/hangfire` | GET | Hangfire Dashboard |

---

## Workflow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    SCAN WORKFLOW                            │
└─────────────────────────────────────────────────────────────┘

1. SCAN ZEMÍ
   │
   ├─── BetExplorer ───> ProviderCountry (všechny)
   │
   └─── Betano ─────────┬─── Napárováno ───> ProviderCountry + CountryProvider
                        │
                        └─── Nenapárováno ─> CountryNameMapping (inactive)
                                                    │
                                                    ▼
                                            [Manuální úprava]
                                                    │
                                                    ▼
                                            [Zpracovat mapování]
                                                    │
                                                    ▼
                                            ProviderCountry + CountryProvider

2. SCAN LIG
   │
   ├─── BetExplorer ───> ProviderLeague (všechny)
   │
   └─── Betano ─────────> Enrichment s BetExplorer ───> ProviderLeague
                          (jen úspěšně enrichnuté)

3. SCAN SEZÓN
   │
   └─── Všechny providery ───> ProviderSeason
```

---

## Poznámky

1. **Betting providery (Betano, Fortuna) se VŽDY mapují na BetExplorer**
   - Bez úspěšného napárování se data neukládají do cache
   - Manuální mapování umožňuje opravit nerozpoznané země/ligy

2. **Scan běží asynchronně přes Hangfire**
   - API vrací ihned `jobId`
   - Status lze sledovat přes `/api/jobs/{jobId}` nebo `/hangfire`

3. **CountryProvider je klíčový pro scan lig**
   - Bez `CountryProvider` mappingu se pro danou zemi nebudou skenovat ligy
   - Mapping se vytváří automaticky při úspěšném napárování země

---

**Poslední aktualizace:** 2025-12-06
