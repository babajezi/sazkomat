# Sazkomat 🎲

Platforma pro import a analýzu historických sázkových dat z BetExplorer.com

## 🚀 Quick Start (po restartu)

```bash
cd C:\projects\private\Sazkomat
docker-compose up -d
```

Otevři: **http://localhost:3000**

📖 Detaily: [QUICK_START.md](QUICK_START.md)

---

## 📋 Dokumentace

| Dokument | Popis |
|----------|-------|
| **[QUICK_START.md](QUICK_START.md)** | Rychlý start po restartu - 3 kroky |
| **[RESTART_INSTRUCTIONS.md](RESTART_INSTRUCTIONS.md)** | Detailní návod pro navázání práce |
| **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** | Přehled implementace Phase 1 |
| **[DOCKER.md](DOCKER.md)** | Docker dokumentace |

---

## 🏗️ Architektura

### Backend (.NET 9)
```
src/
├── Sazkomat.Core/          # Base entities & patterns
├── Sazkomat.Configuration/ # Sport/Country/League management
├── Sazkomat.DataImport/    # Data scraping & import
├── Sazkomat.Strategy/      # Phase 2: AI strategies
└── Sazkomat.Api/           # REST API (Minimal APIs)
```

### Frontend (Next.js 15)
```
frontend/
├── app/
│   ├── page.tsx           # Home dashboard
│   ├── leagues/           # League management
│   └── import/            # Data import
├── components/ui/         # shadcn/ui components
└── lib/api/               # Type-safe API client
```

### Infrastructure
- **PostgreSQL 16** - Databáze (2 schémata)
- **Redis** - Cache (Phase 2)
- **Docker** - Containerization
- **pgAdmin** - DB management UI

---

## ✨ Features (Phase 1)

### ✅ Implementováno
- **Konfigurace Lig** - Správa sportů, zemí a lig
- **Historický Import** - Scraping dat z BetExplorer
- **Background Jobs** - Fire-and-forget import processing
- **Auto Migration** - Automatické DB migrace při startu
- **Seed Data** - 5 top evropských lig předpřipraveno
- **REST API** - 12 endpoints (8 config + 4 import)
- **TypeScript Frontend** - Responzivní UI s React Query
- **Docker Stack** - Kompletní orchestrace

### ⚠️ Potřebuje dokončení
- **HTML Parsing** - Scraper má pouze placeholder
- **Unit Tests** - Struktura připravena
- **CRUD UI** - Frontend má pouze read operace

---

## 🛠️ Technologie

| Vrstva | Technologie |
|--------|-------------|
| **Backend** | .NET 9, ASP.NET Core Minimal APIs |
| **Database** | PostgreSQL 16, Entity Framework Core 9 |
| **Frontend** | Next.js 15, React 19, TypeScript |
| **UI** | Tailwind CSS, shadcn/ui |
| **Data Fetching** | React Query (TanStack Query) |
| **HTTP Client** | Axios |
| **Scraping** | HtmlAgilityPack, Polly (retry policy) |
| **Logging** | Serilog |
| **Containerization** | Docker, Docker Compose |
| **DB Management** | pgAdmin 4 |

---

## 📊 Databázové Schéma

### Configuration Schema
```sql
configuration.sports
configuration.countries
configuration.leagues (→ sports, countries)
```

### Data Import Schema
```sql
data_import.rounds (→ configuration.leagues)
data_import.import_jobs (→ configuration.leagues)
```

**Features:**
- Snake_case pojmenování
- JSONB columns pro komplexní data
- Auto timestamps (created_at, updated_at)
- Indexy pro výkon

---

## 🔌 API Endpoints

### Configuration
```http
GET    /api/config/sports
GET    /api/config/countries
GET    /api/config/leagues
POST   /api/config/leagues
PATCH  /api/config/leagues/{id}
DELETE /api/config/leagues/{id}
```

### Import
```http
GET  /api/import/leagues/available
POST /api/import/historical
GET  /api/import/jobs/{jobId}
GET  /api/import/stats?leagueId={id}
```

### Health
```http
GET /health
```

---

## 🎯 Použití

### 1. Spuštění
```bash
docker-compose up -d
```

### 2. Přístup k aplikaci
- **Frontend**: http://localhost:3000
- **API**: http://localhost:3001
- **pgAdmin**: http://localhost:3004

### 3. Testování API
```bash
# Health check
curl http://localhost:3001/health

# Získat všechny ligy
curl http://localhost:3001/api/config/leagues

# Aktivovat ligu
curl -X PATCH http://localhost:3001/api/config/leagues/{ID} \
  -H "Content-Type": application/json" \
  -d '{"isEnabled": true}'
```

### 4. Import dat
1. Otevři http://localhost:3000/import
2. Vyber ligy
3. Zadej sezóny (např: 2023-2024, 2022-2023)
4. Klikni "Spustit Import"

---

## 🗂️ Struktura projektu

```
Sazkomat/
├── 📄 README.md                    # Tento soubor
├── 📄 QUICK_START.md               # Rychlý start
├── 📄 RESTART_INSTRUCTIONS.md      # Detailní návod
├── 📄 IMPLEMENTATION_SUMMARY.md    # Přehled implementace
├── 📄 DOCKER.md                    # Docker dokumentace
├── 🐳 docker-compose.yml           # Docker orchestrace
├── 📦 Sazkomat.sln                 # .NET solution
├── src/
│   ├── Sazkomat.Core/             # ✅ Core entities
│   ├── Sazkomat.Configuration/    # ✅ Config module + migrations
│   ├── Sazkomat.DataImport/       # ✅ Import module + migrations
│   ├── Sazkomat.Strategy/         # 🚧 Phase 2
│   └── Sazkomat.Api/              # ✅ REST API
├── tests/
│   └── Sazkomat.Tests/            # 🚧 Unit tests
└── frontend/
    ├── app/                        # ✅ Next.js pages
    ├── components/                 # ✅ UI components
    └── lib/                        # ✅ API client
```

**Legend:**
- ✅ Implementováno a funkční
- 🚧 Připraveno, potřebuje dokončení
- ⚠️ Placeholder implementace

---

## 🔧 Vývoj

### Prerekvizity
- .NET 9 SDK
- Node.js 20+
- Docker Desktop
- PostgreSQL 16 (nebo přes Docker)

### Backend Development
```bash
cd src/Sazkomat.Api
dotnet run
```

### Frontend Development
```bash
cd frontend
npm run dev
```

### Database Migrations
```bash
# Vytvořit novou migraci
dotnet ef migrations add MigrationName \
  --project src/Sazkomat.Configuration \
  --startup-project src/Sazkomat.Api \
  --context ConfigurationDbContext

# Aplikovat migrace
dotnet ef database update \
  --project src/Sazkomat.Configuration \
  --startup-project src/Sazkomat.Api
```

---

## 📈 Roadmap

### Phase 1 (Aktuální) ✅
- [x] Konfigurace lig
- [x] Historický import (infrastruktura)
- [x] PostgreSQL persistence
- [x] Basic frontend
- [ ] Skutečný HTML parsing
- [ ] Unit testy
- [ ] Kompletní CRUD UI

### Phase 2 (Plánováno)
- [ ] Strategy module
- [ ] Python AI service
- [ ] Kafka messaging
- [ ] Redis caching
- [ ] Prediction API
- [ ] Advanced analytics
- [ ] Kubernetes deployment

---

## 🐛 Known Issues

1. **Scraper Placeholder**
   - `FootballBetExplorerScraper.cs` má pouze placeholder
   - Potřebuje implementovat skutečný HTML parsing
   - TODO komentáře označují, co doplnit

2. **Foreign Keys**
   - DataImport migrace neobsahuje FK constraints na configuration.leagues
   - Lze přidat ručně v PostgreSQL

3. **Frontend CRUD**
   - League management má pouze READ operace
   - CREATE, UPDATE, DELETE potřebují UI implementaci

---

## 🤝 Contributing

Projekt je v rané fázi vývoje. Hlavní oblasti pro přispění:

1. **Scraping Implementation**
   - Analyzovat BetExplorer.com HTML strukturu
   - Implementovat robustní parsování
   - Přidat error handling

2. **Testing**
   - Unit testy pro repositories
   - Integration testy pro API
   - End-to-end testy

3. **Frontend Features**
   - League CRUD dialogy
   - Real-time job monitoring
   - Grafy a vizualizace
   - Import statistics dashboard

---

## 📝 License

Tento projekt je pro interní použití.

---

## 🆘 Support

### Troubleshooting

**Docker nespouští?**
```bash
docker-compose logs
docker-compose restart
```

**API nemůže připojit k DB?**
```bash
docker-compose ps
docker-compose logs postgres
```

**Frontend nemůže volat API?**
- Zkontroluj CORS v `Program.cs`
- Ověř `NEXT_PUBLIC_API_URL` v `.env.local`

### Dokumentace
- [Next.js Docs](https://nextjs.org/docs)
- [.NET Docs](https://learn.microsoft.com/en-us/aspnet/core)
- [PostgreSQL Docs](https://www.postgresql.org/docs/16/)
- [Docker Docs](https://docs.docker.com/)

---

**Vytvořeno:** 2025-10-23
**Verze:** 1.0.0 (Phase 1)
**Status:** ✅ Ready for testing (s omezeními)
