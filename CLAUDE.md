# CLAUDE.md - Projektová dokumentace pro AI asistenta

## Přehled projektu

**Sazkomat** je platforma pro import a analýzu historických sportovních sázkových dat z BetExplorer.com.

- **Fáze 1** (aktuální): Infrastruktura pro import dat
- **Fáze 2** (plánovaná): AI-powered analýza sázkových strategií

## Technologický stack

### Backend
- **.NET 9** s ASP.NET Core Minimal APIs
- **Entity Framework Core 9** (Code-First)
- **PostgreSQL 16** (hlavní databáze)
- **Hangfire** (background job processing s PostgreSQL storage)
- **Redis 7** (připraveno pro Fázi 2)
- **Serilog** (strukturované logování)
- **Polly** (resilience a retry policies)
- **HtmlAgilityPack** + **Playwright** (web scraping)

### Frontend
- **Next.js 15** s App Router
- **React 19**
- **TypeScript 5.6**
- **Tailwind CSS 3.4**
- **shadcn/ui** komponenty
- **TanStack Query** (React Query)

### Infrastruktura
- **Docker & Docker Compose**
- **pgAdmin 4**

## Architektura

### Backend - Modulární monolit

```
src/
├── Sazkomat.Core/              # Sdílené jádro
│   ├── Entities/               # Bázová Entity třída
│   └── Common/                 # Result pattern pro error handling
│
├── Sazkomat.Configuration/     # Konfigurační modul
│   ├── Entities/               # Sport, Country, League
│   ├── Repositories/           # Repository pattern
│   ├── Services/               # Business logika
│   └── Data/                   # DbContext, EF konfigurace
│
├── Sazkomat.DataImport/        # Modul pro import dat
│   ├── Entities/               # Round, ImportJob, ImportJobStatus
│   ├── Repositories/           # Repository pattern
│   ├── Services/               # Import orchestrace
│   ├── Scrapers/               # Web scraping implementace
│   └── Data/                   # DbContext, EF konfigurace
│
├── Sazkomat.Strategy/          # Strategický modul (Fáze 2)
│
└── Sazkomat.Api/               # REST API
    ├── Endpoints/              # Minimal API endpointy
    └── Middleware/             # Error handling middleware
```

### Databázové schéma

**Dvě PostgreSQL schémata:**

1. **configuration** - Katalogy (sports, countries, leagues, seasons, league_seasons)
2. **data_import** - Importovaná data + provider cache
   - **Import tables**: rounds, import_jobs
   - **Provider cache**: provider_countries, provider_leagues, provider_seasons
   - **Job queue**: sync_jobs (Hangfire používá vlastní schéma)

**Konvence:**
- Snake_case pro názvy sloupců
- JSONB pro komplexní data (metadata, progress, raw provider data)
- UUID jako primární klíče
- Auto timestamps (created_at, updated_at)

### Frontend - Next.js App Router

```
frontend/
├── app/                        # Next.js stránky
│   ├── page.tsx                # Dashboard
│   ├── leagues/page.tsx        # Správa lig
│   └── import/page.tsx         # Import rozhraní
│
├── components/ui/              # shadcn/ui komponenty
│
└── lib/
    ├── api/                    # Type-safe API klient
    └── providers.tsx           # React Query provider
```

## API Endpoints

### Configuration
- `GET /api/config/sports` - Seznam sportů
- `GET /api/config/countries` - Seznam zemí
- `GET /api/config/leagues` - Seznam lig
- `POST /api/config/leagues` - Vytvoření ligy
- `PATCH /api/config/leagues/{id}` - Aktualizace ligy
- `DELETE /api/config/leagues/{id}` - Smazání ligy

### Import
- `GET /api/import/leagues/available` - Dostupné ligy pro import
- `POST /api/import/historical` - Spuštění historického importu
- `GET /api/import/jobs/{jobId}` - Status importní úlohy
- `GET /api/import/stats?leagueId=` - Statistiky importu

### Scan (Provider Cache)
- `POST /api/scan/countries` - Scan zemí z providera do cache
- `POST /api/scan/leagues` - Scan lig z providera do cache
- `POST /api/scan/seasons` - Scan sezón z providera do cache

### Jobs (Background Processing)
- `GET /api/jobs/{jobId}` - Status konkrétního jobu
- `GET /api/jobs/recent?providerId=&count=` - Seznam posledních jobů
- `POST /api/jobs/scan` - Zařadit scan job do fronty
- `POST /api/jobs/import` - Zařadit import job do fronty
- `POST /api/jobs/livesync` - Zařadit live sync job do fronty

### Live Sync
- `POST /api/livesync/rounds` - Živá synchronizace kol
- `POST /api/livesync/rounds/{roundId}` - Synchronizace konkrétního kola
- `GET /api/livesync/stats?providerId=` - Statistiky live sync

### Health
- `GET /health` - Health check
- `GET /hangfire` - Hangfire Dashboard (job queue monitoring)

## Synchronizační workflow

**3-Step Workflow** pro robustní zpracování dat z providerů:

### Krok 1: SCAN (Load to Cache)
- **Endpoint**: `/api/scan/{countries|leagues|seasons}`
- **Účel**: Načte data z providera do cache tabulek (provider_countries, provider_leagues, provider_seasons)
- **Výhody**:
  - Oddělení načítání dat od importu
  - Možnost náhledu před importem
  - Zachování audit trail (data se nikdy nemažou)
- **Job Type**: `SyncJobType.Scan`

### Krok 2: IMPORT (Cache to Config)
- **Service**: `ImportService` (ne public API, voláno interně nebo přes jobs)
- **Účel**: Importuje vybraná data z cache do hlavních konfiguračních tabulek
- **Logika**:
  - Kontroluje duplicity přes `CountryProvider` a `LeagueProvider` mapování
  - Vytváří/aktualizuje entity v configuration schématu
  - Zachovává referenční integritu
- **Job Type**: `SyncJobType.Import`

### Krok 3: LIVE SYNC (Ongoing Updates)
- **Endpoint**: `/api/livesync/rounds`
- **Účel**: Průběžná synchronizace aktuálních dat (zápasy, kola)
- **Použití**:
  - Běžící sezóny
  - Real-time aktualizace výsledků
  - Může být naplánováno rekurentně přes Hangfire
- **Job Type**: `SyncJobType.LiveSync`

### Job Queue (Hangfire)
- Všechny operace (scan, import, live sync) běží asynchronně
- Tracking přes `sync_jobs` tabulku
- Status: `Pending` → `Running` → `Completed` / `Failed`
- Monitoring přes `/hangfire` dashboard nebo `/api/jobs/recent`
- Konfigurace v `appsettings.json` (`Hangfire` sekce)

### Frontend Flow
- **ScanDialog** komponenta - Spouští scan operace
- **CacheTablesView** - Náhled cachovaných dat před importem
- **Jobs Page** (`/jobs`) - Monitoring všech běžících a dokončených jobů

## Důležité informace pro vývoj

### Konvence a pravidla

1. **Naming conventions:**
   - Backend: PascalCase pro třídy, camelCase pro proměnné
   - Databáze: snake_case pro tabulky a sloupce
   - Frontend: camelCase pro funkce/proměnné, PascalCase pro komponenty

2. **Error handling:**
   - Backend používá Result pattern (`Result<T>` z `Sazkomat.Core.Common`)
   - API vrací standardizované JSON error responses
   - Frontend používá React Query error boundaries

3. **Entity Framework:**
   - Code-First approach
   - Migrations v každém modulu zvlášť
   - Fluent API konfigurace v `Data/Configurations/`
   - Auto-migration při startu (`context.Database.MigrateAsync()`)

4. **Dependency Injection:**
   - Služby registrované v `Program.cs`
   - Repository pattern s rozhraními
   - Scoped lifetime pro DbContext
   - Transient pro repository a services

5. **⚠️ KRITICKÉ PRAVIDLO - Porty:**
   - **PORTY SE NESMÍ MĚNIT BEZ EXPLICITNÍHO ODSOUHLASENÍ**
   - Pokud potřebuješ použít nový port, **VŽDY SE ZEPTEJ** uživatele PŘED jakoukoli změnou
   - Nikdy neměň stávající port mappings bez souhlasu
   - **Standardní porty projektu (NEMĚNIT):**
     - Frontend: **3000**
     - API: **3001**
     - PostgreSQL: **3002**
     - Redis: **3003**
     - pgAdmin: **3004**

### Struktura projektu

```
C:\projects\private\Sazkomat/
├── README.md                       # Hlavní dokumentace
├── QUICK_START.md                  # Quick start (3 kroky)
├── RESTART_INSTRUCTIONS.md         # Detailní restart instrukce
├── IMPLEMENTATION_SUMMARY.md       # Souhrn Fáze 1
├── TESTING.md                      # Kompletní test dokumentace
├── DOCKER.md                       # Docker dokumentace
├── docker-compose.yml              # Docker orchestrace
├── Sazkomat.sln                    # .NET solution
│
├── src/                            # Backend zdrojový kód
├── tests/                          # Unit testy
└── frontend/                       # Next.js frontend
```

### Docker služby

```yaml
- frontend:3000     # Next.js frontend
- api:3001          # .NET API
- postgres:3002     # PostgreSQL databáze
- redis:3003        # Redis cache (připraveno)
- pgadmin:3004      # Database management
```

### Přístupové body

| Služba | URL | Credentials |
|--------|-----|-------------|
| Frontend | http://localhost:3000 | - |
| API | http://localhost:3001 | - |
| Health | http://localhost:3001/health | - |
| pgAdmin | http://localhost:3004 | admin@sazkomat.local / admin123 |
| PostgreSQL | localhost:3002 | sazkomat / sazkomat123 |

## Stav projektu

### ✅ Fáze 1 - KOMPLETNĚ DOKONČENO A OTESTOVÁNO

**Datum dokončení:** 2025-10-30
**Status:** 🎉 **PRODUCTION READY - 100% COMPLETE**

Phase 1 infrastruktury je kompletně implementována a otestována na reálných datech:

- ✅ **HTML Parsing Scraper** - Plně funkční, otestovaný na 5 ligách
  - Parsuje kola (rounds) a zápasy z BetExplorer.com
  - Extrahuje výsledky (H/D/A) a kurzy (1/X/2)
  - Počítá kumulativní odds
  - Resilient HTTP client s Polly retry policy
  - **Testováno:** 334 kol, 3,272 zápasů, 100% úspěšnost
  - Umístění: `src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs`

- ✅ **Multi-League Import** - Paralelní zpracování více lig
  - Single request podporuje multiple leagueIds
  - Background job processing
  - **Performance:** 4 ligy (144 kol) za ~6 sekund
  - Progress tracking s real-time status

- ✅ **Multi-Season Import** - Historická data
  - Importuje více sezón najednou
  - **Testováno:** 5 sezón Premier League (2020-2025)
  - Automatické vytváření Season entities
  - **Performance:** 114 kol za ~15 sekund

- ✅ **Error Handling** - Robustní validace
  - Neexistující liga ID - vrací 400 Bad Request
  - Zakázaná liga - validace isEnabled
  - Prázdné requesty - input validation
  - **Testováno:** 5/5 error scénářů prošlo

- ✅ **Frontend CRUD** - Kompletní správa lig
  - CreateLeagueDialog - formulář pro vytváření nových lig
  - EditLeagueDialog - editace existujících lig
  - Delete funkce s potvrzením
  - React Query integrace
  - Umístění: `frontend/app/leagues/page.tsx`, `frontend/components/LeagueFormDialog.tsx`

- ✅ **Foreign Key Constraints** - Databázová integrita
  - FK z `data_import.rounds.league_id` → `configuration.leagues.id`
  - FK z `data_import.import_jobs.league_id` → `configuration.leagues.id`
  - CASCADE delete pro automatické čištění
  - Umístění: `src/Sazkomat.DataImport/Migrations/20251022232856_AddForeignKeyConstraints.cs`

- ✅ **Unit Testy** - Komprehensivní test coverage (~85% critical paths)
  - **113 testů celkem - všechny prochází ✓**
  - Service Layer: 63 testů (ScanService, LiveSyncService, ImportService, SyncWorkflowService, SeasonService)
  - Repository Layer: 39 testů (všechny repositories včetně SyncJob, Match, ProviderLeague)
  - Scraper Layer: 11 testů (FootballBetExplorerScraper, ResilientHttpClient)
  - Používá xUnit, In-Memory database a Moq
  - **Detailní dokumentace:** `TESTING.md` (coverage report, best practices, příklady)
  - Umístění: `tests/Sazkomat.Tests/`

- ✅ **Integration Testing** - Plně otestováno na produkčních datech
  - 5 evropských lig (Premier League, La Liga, Bundesliga, Serie A, Ligue 1)
  - 5 sezón (2020-2021 až 2023-2024, plus 1999-2000)
  - 334 kol, 3,272 zápasů
  - 7 import jobs, 100% success rate
  - Viz: `PRIORITY_1_TEST_RESULTS.md`

### 📊 Production Data Metrics

**Aktuální stav databáze:**
- Ligy: 5 (všechny top evropské ligy)
- Sezóny: 5
- Kola: 334
- Zápasy: 3,272
- Import Jobs: 7 (100% úspěšnost)

**Performance:**
- Single league: ~3-6s pro 38 kol
- Multi-league: ~6s pro 4 ligy paralelně
- Throughput: ~25 zápasů/sekundu
- Peak: 229 zápasů/s (při paralelním importu)

**Data Quality:**
- Home wins: 43.80%
- Draws: 24.85%
- Away wins: 31.36%
- Odds completeness: 100%

### Známé problémy

1. **Docker Frontend Build** - Frontend public folder problém
   - Build selhává na `COPY --from=builder /app/public ./public`
   - Next.js negeneruje public folder během buildu
   - Obejít lze lokálním developmentem (`npm run dev`)
   - **Priorita:** LOW (lokální dev funguje perfektně)

### Fáze 1 - Volitelné vylepšení ✅ KOMPLETNĚ DOKONČENO

- [✅] ~~Implementovat rate limiting pro scraper~~ (již implementováno v ResilientHttpClient)
- [✅] ~~Přidat progress tracking pro import jobs~~ (funkční)
- [✅] **Real-time job monitoring** - Polling každé 2s s React Query (`frontend/app/import/page.tsx:46-60`)
- [✅] **Dashboard s importními statistikami** - Plně funkční s grafy (`frontend/app/dashboard/page.tsx`)
  - KPI karty (Ligy, Kola, Sezóny, Zápasy)
  - Pie chart - Rozdělení výsledků zápasů (H/D/A)
  - Bar chart - Top 10 lig podle počtu kol
  - Bar chart - Rozložení dat podle sezón
  - Tabulka - Historie posledních 10 import jobů
  - Tabulka - Detailní statistiky lig
- [✅] **Docker frontend build** - Opraveno, standalone mode nakonfigurován (`frontend/next.config.ts:4`)

### Fáze 2 - Plán

- [ ] AI Strategy modul
- [ ] Python service pro ML predikce
- [ ] Kafka messaging
- [ ] Redis caching
- [ ] Analytics dashboard
- [ ] Kubernetes deployment

## Vývojové příkazy

### Quick Start
```bash
# 1. Spustit Docker Desktop
# 2. Spustit aplikaci
docker-compose up -d
# 3. Otevřít http://localhost:3000
```

### Backend development
```bash
cd src/Sazkomat.Api
dotnet run
```

### Frontend development
```bash
cd frontend
npm run dev
```

### Databázové migrace
```bash
# Configuration modul
dotnet ef migrations add MigrationName \
  --project src/Sazkomat.Configuration \
  --startup-project src/Sazkomat.Api

# DataImport modul
dotnet ef migrations add MigrationName \
  --project src/Sazkomat.DataImport \
  --startup-project src/Sazkomat.Api
```

### Docker management
```bash
docker-compose ps              # Status služeb
docker-compose logs -f api     # Logy API
docker-compose restart api     # Restart služby
docker-compose down -v         # Úplný reset
```

### Testování
```bash
cd tests/Sazkomat.Tests
dotnet test

# Detailní výstup s verbose mode
dotnet test --verbosity detailed

# Spustit konkrétní test file
dotnet test --filter "FullyQualifiedName~ScanServiceTests"
```

**Dokumentace:** Viz `TESTING.md` pro kompletní přehled testů, coverage report a best practices

## Důležité soubory

### Konfigurace
- `docker-compose.yml` - Docker orchestrace
- `src/Sazkomat.Api/appsettings.json` - API konfigurace
- `frontend/.env.local` - Frontend environment proměnné

### Data seeding
- `src/Sazkomat.Configuration/Data/SeedData.cs` - Seed data pro sports, countries, leagues

### Entity konfigurace
- `src/Sazkomat.Configuration/Data/Configurations/` - EF fluent API
- `src/Sazkomat.DataImport/Data/Configurations/` - EF fluent API

## Design Patterns

1. **Repository Pattern** - Abstrakce data access layer
2. **Service Layer Pattern** - Oddělení business logiky
3. **Result Pattern** - Funkcionální error handling
4. **Dependency Injection** - Built-in .NET DI
5. **Background Jobs** - Fire-and-forget pro dlouhotrvající úlohy

## Best Practices

### Při práci s projektem

1. **Vždy používat Repository pattern** - Nikdy nepřistupovat k DbContext přímo z API endpointů
2. **Result pattern pro error handling** - Vracet `Result<T>` z business logiky
3. **Async/await všude** - Všechny DB operace a HTTP calls musí být async
4. **Logování přes Serilog** - Strukturované logy s kontextem
5. **Type-safe API klient** - Frontend používá typované rozhraní z `lib/api/types.ts`

6. **⚠️ KRITICKÉ PRAVIDLO - TypeScript API Types:**
   - **VŽDY ZKONTROLUJ** backend enum serializaci před vytvořením TypeScript typů
   - Backend používá `JsonStringEnumConverter` (Program.cs:49) - všechny enums jsou **STRINGY**, ne čísla
   - **POVINNÝ WORKFLOW při přidání/změně enum:**
     1. Zjisti hodnoty backendu (zavolej API endpoint, zkontroluj C# enum definici)
     2. Ověř serializaci - spusť `curl` nebo `Invoke-RestMethod` na endpoint
     3. Vytvoř TypeScript enum se **STRINGOVÝMI** hodnotami odpovídajícími backendu
     4. Zkontroluj naming - backend může mít jiný název (např. `LiveUpdate` vs `LiveSync`)
     5. Zkontroluj všechny hodnoty - backend může mít více hodnot než očekáváš (např. `Cancelled`, `Rounds`)
   - **NIKDY NEPŘEDPOKLÁDEJ** numerické hodnoty nebo názvy - VŽDY OVĚŘ NA REÁLNÝCH DATECH
   - Chybné enum types způsobují tiché selhání ve filtrovacích komponentách a business logice

### Při přidávání nových features

1. **Entita** - Vytvořit v `Entities/`
2. **Repository** - Interface + implementace v `Repositories/`
3. **Service** - Business logika v `Services/`
4. **Configuration** - EF Fluent API v `Data/Configurations/`
5. **Migration** - `dotnet ef migrations add`
6. **Endpoint** - Minimal API v `Api/Endpoints/`
7. **Frontend** - API type, komponenta, stránka

### Code Style

- **Backend:** Dodržovat C# coding conventions
- **Frontend:** ESLint + Prettier (konfigurace v projektu)
- **Databáze:** snake_case, nikdy mixed case
- **API:** RESTful conventions, JSON responses

## Troubleshooting

### API nereaguje
```bash
docker-compose logs -f api
# Zkontrolovat connection string k PostgreSQL
```

### Frontend se nenačítá
```bash
docker-compose logs -f frontend
# Zkontrolovat NEXT_PUBLIC_API_URL v .env.local
```

### Databáze není dostupná
```bash
docker-compose ps
docker-compose restart postgres
# Počkat ~10s než PostgreSQL nastartuje
```

### Migrace selhávají
```bash
# Zkontrolovat, že startup project je Sazkomat.Api
# Zkontrolovat connection string
# Smazat databázi a znovu: docker-compose down -v && docker-compose up -d
```

## Užitečné odkazy

- [.NET 9 Docs](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Next.js 15 Docs](https://nextjs.org/docs)
- [shadcn/ui Components](https://ui.shadcn.com/)
- [TanStack Query](https://tanstack.com/query/latest)

## Kontakt a poznámky

- Fáze 1 je **KOMPLETNĚ DOKONČENA** a plně otestována
- Scraper je **production-ready** (otestován na 3,272 zápasech)
- Kód je připraven pro rozšíření do Fáze 2
- Clean Architecture s jasně oddělenými vrstvami
- Moderní stack s nejnovějšími verzemi technologií

---

**Poslední aktualizace:** 2025-10-30
**Status:** 🎉 **Fáze 1 - 100% DOKONČENO** | Připraveno na Fázi 2

**Fáze 1 Features - Všechny Implementováno:**
- Backend infrastruktura (.NET 9, EF Core, PostgreSQL)
- HTML scraping s Playwright (production-tested na 3,272 zápasech)
- Multi-league & multi-season import s paralelním zpracováním
- Frontend CRUD pro správu lig (Create, Edit, Delete)
- Real-time job monitoring s polling (2s interval)
- Dashboard s grafy a statistikami (Recharts)
- Docker configuration s standalone mode
- Unit testy (113 testů - všechny prochází, ~85% coverage kritických cest)
- Foreign key constraints a databázová integrita
- Error handling a validace

**Test Reports:**
- `TESTING.md` - Kompletní test dokumentace (coverage, best practices, příklady)
- `PRIORITY_1_TEST_RESULTS.md` - Výsledky produkčního testování
