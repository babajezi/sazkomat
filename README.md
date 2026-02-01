# Sazkomat

Platforma pro import a analýzu historických sportovních sázkových dat z BetExplorer.com.

## Quick Start

```bash
# 1. Spustit Docker Desktop
# 2. Spustit aplikaci
docker-compose up -d
# 3. Otevřít http://localhost:3000
```

## Porty a přístupy

| Služba | Port | URL |
|--------|------|-----|
| Frontend | 3000 | http://localhost:3000 |
| API | 3001 | http://localhost:3001 |
| PostgreSQL | 3002 | localhost:3002 |
| Redis | 3003 | localhost:3003 |
| pgAdmin | 3004 | http://localhost:3004 |

**Credentials:**
- pgAdmin: `admin@sazkomat.local` / `admin123`
- PostgreSQL: `sazkomat` / `sazkomat123`

## Dokumentace

| Dokument | Popis |
|----------|-------|
| [CLAUDE.md](CLAUDE.md) | Projektová dokumentace pro AI asistenta |
| [docs/API.md](docs/API.md) | Kompletní API dokumentace (126+ endpointů) |
| [docs/DATABASE_SCHEMA.md](docs/DATABASE_SCHEMA.md) | Databázové schéma |
| [docs/DOCKER.md](docs/DOCKER.md) | Docker příkazy a konfigurace |
| [docs/BUILD.md](docs/BUILD.md) | Build guide |
| [docs/TESTING.md](docs/TESTING.md) | Test dokumentace |

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, EF Core 9, PostgreSQL 16
- **Frontend:** Next.js 15, React 19, TypeScript, Tailwind CSS
- **Infra:** Docker, Hangfire, Redis, Playwright

## Status

**Fáze 1:** Production Ready
**Fáze 2:** Plánováno (AI analýza)
