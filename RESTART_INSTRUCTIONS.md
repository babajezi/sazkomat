# Sazkomat - Instrukce pro navázání po restartu

## Stav před restartem

✅ **Kompletně implementováno:**
- Backend (.NET 9) - 5 projektů, 12 API endpoints
- Frontend (Next.js 15) - 3 stránky, plně funkční UI
- PostgreSQL migrace vytvořeny a připraveny
- Docker compose konfigurace
- Všechny NuGet balíčky nainstalovány
- Všechny npm balíčky nainstalovány

✅ **Build status:**
- Backend: ✅ Úspěšný build
- Frontend: ✅ Úspěšný build (106 kB bundle)

⏳ **Co zbývá:**
- Spustit Docker Desktop (po restartu)
- Spustit celý stack

---

## Postup po restartu

### 1. Ověř instalaci Dockeru

```bash
docker --version
docker-compose --version
```

Pokud funguje, pokračuj dál.

### 2. Spusť celý stack

```bash
cd C:\projects\private\Sazkomat

# Spustit všechny služby
docker-compose up -d
```

Toto spustí:
- PostgreSQL 16 na portu 5432
- Redis na portu 6379
- pgAdmin na portu 5050
- .NET API na portu 5000
- Next.js Frontend na portu 3000

### 3. Ověř, že vše běží

```bash
# Zkontrolovat běžící kontejnery
docker-compose ps

# Sledovat logy
docker-compose logs -f sazkomat-api
```

### 4. Otevři aplikaci

**Frontend:**
- http://localhost:3000

**API:**
- http://localhost:5000
- Health check: http://localhost:5000/health

**pgAdmin:**
- http://localhost:5050
- Email: admin@sazkomat.local
- Password: admin123

---

## Alternativa: Spuštění bez Dockeru

Pokud Docker nefunguje, můžeš spustit ručně:

### Backend
```bash
cd C:\projects\private\Sazkomat\src\Sazkomat.Api
dotnet run
```
⚠️ Vyžaduje běžící PostgreSQL na localhost:5432

### Frontend
```bash
cd C:\projects\private\Sazkomat\frontend
npm run dev
```

---

## Testování aplikace

### 1. Otevři frontend
http://localhost:3000

Měl by se zobrazit dashboard se 2 kartami:
- **Konfigurace Lig** - zobrazí 5 předpřipravených lig
- **Import Dat** - umožní spustit historický import

### 2. Zobrazení lig
Klikni na "Otevřít konfiguraci"
- Mělo by se zobrazit 5 lig (Premier League, La Liga, Bundesliga, Serie A, Ligue 1)
- Všechny budou neaktivní (disabled)

### 3. Test API endpoint
```bash
# Test health check
curl http://localhost:5000/health

# Získat všechny ligy
curl http://localhost:5000/api/config/leagues

# Získat sporty
curl http://localhost:5000/api/config/sports
```

### 4. Aktivace ligy (přes API)
```bash
# Aktualizovat ligu - aktivovat Premier League
curl -X PATCH http://localhost:5000/api/config/leagues/{LEAGUE_ID} \
  -H "Content-Type: application/json" \
  -d '{"isEnabled": true}'
```

### 5. Spustit import
1. Otevři http://localhost:3000/import
2. Vyber aktivované ligy
3. Zadej sezóny (např: 2023-2024, 2022-2023)
4. Klikni "Spustit Import"

⚠️ **Pozor:** Import bude částečně selhávat, protože FootballBetExplorerScraper má pouze placeholder implementaci a potřebuje skutečný HTML parsing.

---

## Přístup do databáze

### Přes pgAdmin (v prohlížeči)
1. Otevři http://localhost:5050
2. Přihlaš se (admin@sazkomat.local / admin123)
3. Přidej server:
   - Host: postgres
   - Port: 5432
   - Database: sazkomat_db
   - Username: sazkomat
   - Password: sazkomat123

### Přes příkazovou řádku
```bash
docker exec -it sazkomat-postgres psql -U sazkomat -d sazkomat_db

# SQL příkazy:
\dt configuration.*     # Tabulky v configuration schema
\dt data_import.*       # Tabulky v data_import schema
SELECT * FROM configuration.leagues;
SELECT * FROM configuration.sports;
```

---

## Struktura projektu

```
C:\projects\private\Sazkomat\
├── docker-compose.yml              # Docker orchestrace
├── DOCKER.md                       # Docker dokumentace
├── IMPLEMENTATION_SUMMARY.md       # Detailní přehled implementace
├── RESTART_INSTRUCTIONS.md         # Tento soubor
├── Sazkomat.sln                    # .NET solution
├── src/
│   ├── Sazkomat.Core/             # Core entities & Result pattern
│   ├── Sazkomat.Configuration/    # Sport/Country/League management
│   │   └── Migrations/            # ✅ EF migrations vytvořeny
│   ├── Sazkomat.DataImport/       # Data import & scraping
│   │   └── Migrations/            # ✅ EF migrations vytvořeny
│   ├── Sazkomat.Strategy/         # Phase 2 placeholder
│   └── Sazkomat.Api/              # REST API
└── frontend/
    ├── app/                        # Next.js pages
    │   ├── page.tsx               # Home
    │   ├── leagues/page.tsx       # Leagues management
    │   └── import/page.tsx        # Data import
    ├── components/ui/              # shadcn/ui components
    ├── lib/api/                    # API client
    └── package.json                # ✅ Dependencies installed
```

---

## Co bylo vytvořeno - souhrn

### Backend (.NET 9)
- **5 projektů**: Core, Configuration, DataImport, Strategy, Api, Tests
- **12 API endpoints**: 8 pro konfiguraci, 4 pro import
- **2 PostgreSQL schémata**: configuration, data_import
- **Auto-migration** při startu aplikace
- **Seed data**: 1 sport (Football), 5 zemí, 5 top lig
- **Scraping infrastruktura**: Polly retry, anti-bot features
- **Background processing**: Fire-and-forget import jobs
- **Logging**: Serilog structured logging

### Frontend (Next.js 15)
- **3 stránky**: Home dashboard, Leagues management, Import
- **TypeScript** s type-safe API klientem
- **React Query** pro data fetching
- **Tailwind CSS** + shadcn/ui komponenty
- **Responzivní design**
- **Real-time monitoring** (připraveno)

### Databáze
- **PostgreSQL 16** s snake_case pojmenování
- **2 schémata**: `configuration`, `data_import`
- **JSONB columns** pro komplexní data (seasons, progress)
- **Indexes** pro výkon
- **Migrations** ready to apply

### Docker
- **6 služeb**: PostgreSQL, Redis, pgAdmin, API, Frontend, (Kafka ready)
- **Multi-stage builds** pro optimalizaci
- **Health checks** pro všechny služby
- **Named volumes** pro persistence

---

## Další kroky (po spuštění)

### 1. Implementovat skutečný scraping
`src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs`

Aktuálně placeholder - potřebuje:
- Analyzovat HTML strukturu BetExplorer.com
- Implementovat XPath/CSS selektory
- Parsovat Round data (matches, odds, results)

### 2. Přidat unit testy
`tests/Sazkomat.Tests/`

Struktura připravena, je třeba implementovat:
- Repository testy
- Service testy
- Scraper testy

### 3. Rozšířit frontend
- League CRUD UI (Create, Edit, Delete)
- Real-time job monitoring (WebSockets nebo polling)
- Import statistics dashboard
- Grafy a vizualizace

### 4. Phase 2 funkce
- Strategy module implementace
- Python AI service integrace
- Kafka messaging
- Redis caching
- Kubernetes deployment

---

## Troubleshooting

### Docker nespouští kontejnery
```bash
# Zkontrolovat logy
docker-compose logs

# Restartovat službu
docker-compose restart sazkomat-api

# Kompletní reset
docker-compose down
docker-compose up -d
```

### API nelze připojit k PostgreSQL
1. Ověř, že PostgreSQL běží: `docker-compose ps`
2. Zkontroluj logy: `docker-compose logs postgres`
3. Zkontroluj connection string v `appsettings.json`

### Frontend nemůže volat API
1. Ověř CORS nastavení v `Program.cs:27-36`
2. Zkontroluj `NEXT_PUBLIC_API_URL` v `.env.local`
3. Ověř, že API běží na http://localhost:5000

### Migrace selhávají
```bash
# Smazat databázi a začít znovu
docker-compose down -v
docker-compose up -d postgres
# Počkat 10s, pak spustit API
```

---

## Kontakty a dokumentace

- **Dokumentace Next.js**: https://nextjs.org/docs
- **Dokumentace .NET**: https://learn.microsoft.com/en-us/aspnet/core
- **Dokumentace PostgreSQL**: https://www.postgresql.org/docs/16/
- **BetExplorer**: https://www.betexplorer.com/

---

## Quick Start po restartu

```bash
# 1. Přejít do projektu
cd C:\projects\private\Sazkomat

# 2. Spustit vše
docker-compose up -d

# 3. Sledovat logy
docker-compose logs -f

# 4. Otevřít browser
start http://localhost:3000
```

**Hotovo!** Aplikace by měla běžet. 🎉
