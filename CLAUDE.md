# CLAUDE.md

## Přehled projektu

**Sazkomat** je platforma pro import a analýzu historických sportovních sázkových dat z BetExplorer.com. Fáze 1 (aktuální) = infrastruktura pro import dat, Fáze 2 (plánovaná) = AI analýza sázkových strategií.

## Tech Stack

### Backend
- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 9 (Code-First)
- PostgreSQL 16
- Hangfire (background jobs)
- Redis 7 (připraveno)
- HtmlAgilityPack + Playwright (scraping)

### Frontend
- Next.js 15 (App Router)
- React 19, TypeScript 5.6
- Tailwind CSS 3.4, shadcn/ui
- TanStack Query

### Infrastruktura
- Docker & Docker Compose
- pgAdmin 4

## Architektura

```
src/
├── Sazkomat.Core/           # Sdílené jádro (Entity, Result pattern)
├── Sazkomat.Configuration/  # Sport, Country, League, Season, Providers
├── Sazkomat.DataImport/     # Round, Match, Scrapers, Jobs, Cache
├── Sazkomat.Strategy/       # (Fáze 2)
└── Sazkomat.Api/            # REST API endpointy

frontend/
├── app/                     # Next.js stránky
├── components/              # React komponenty
└── lib/api/                 # Type-safe API klient
```

## Databáze

Dvě PostgreSQL schémata:
- **configuration** - sports, countries, leagues, seasons, league_seasons, data_providers, *_providers
- **data_import** - rounds, matches, import_jobs, sync_jobs, provider_*, unmatched_*, *_name_mappings, scraper_recipes

Kompletní schéma: viz `docs/DATABASE_SCHEMA.md`

## API

126+ endpointů organizovaných do kategorií:
- Config (sports, countries, leagues, seasons, providers)
- Scan, Import, Sync, LiveSync
- Jobs, Unmatched Leagues/Countries
- Name Mappings, Provider Cache
- Recipes, Debug, Database

Kompletní dokumentace: viz `docs/API.md`

## Quick Start

```bash
docker-compose up -d
# Frontend: http://localhost:3000
# API: http://localhost:3001
```

## Vývojové příkazy

```bash
# Backend
cd src/Sazkomat.Api && dotnet run

# Frontend
cd frontend && npm run dev

# Testy
cd tests/Sazkomat.Tests && dotnet test
```

## Migrace

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

## Dokumentace

| Soubor | Obsah |
|--------|-------|
| `docs/DATABASE_SCHEMA.md` | Kompletní databázové schéma |
| `docs/API.md` | Všechny API endpointy |
| `docs/TESTING.md` | Test dokumentace |
| `docs/BUILD.md` | Build guide |
| `docs/QUICK_START.md` | Quick start |
| `.claude/rules/` | Pravidla pro Claude |
| `README.md` | Hlavní dokumentace |
