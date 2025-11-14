# Sazkomat - Quick Start Guide

## Po restartu počítače - 3 kroky

### 1️⃣ Spustit Docker Desktop
- Otevři Docker Desktop
- Počkej, až se spustí (zelená ikonka)

### 2️⃣ Spustit aplikaci
```bash
cd C:\projects\private\Sazkomat
docker-compose up -d
```

### 3️⃣ Otevřít v prohlížeči
```
http://localhost:3000
```

---

## Ověření běhu

```bash
# Zkontrolovat, že vše běží
docker-compose ps

# Mělo by běžet 5 služeb:
# - sazkomat-postgres (PostgreSQL)
# - sazkomat-redis (Redis)
# - sazkomat-pgadmin (pgAdmin)
# - sazkomat-api (.NET API)
# - sazkomat-frontend (Next.js)
```

---

## Užitečné příkazy

```bash
# Sledovat logy
docker-compose logs -f sazkomat-api

# Restartovat službu
docker-compose restart sazkomat-api

# Zastavit vše
docker-compose down

# Zastavit vše a smazat data
docker-compose down -v
```

---

## Přístupy

| Služba | URL | Přihlášení |
|--------|-----|------------|
| **Frontend** | http://localhost:3000 | - |
| **API** | http://localhost:3001 | - |
| **Health Check** | http://localhost:3001/health | - |
| **pgAdmin** | http://localhost:3004 | admin@sazkomat.local / admin123 |

---

## PostgreSQL připojení

**V pgAdminu:**
- Host: `postgres` (nebo `localhost` z hosta)
- Port: `5432` (interní) / `3002` (z hosta)
- Database: `sazkomat_db`
- Username: `sazkomat`
- Password: `sazkomat123`

**Z příkazové řádky:**
```bash
docker exec -it sazkomat-postgres psql -U sazkomat -d sazkomat_db
```

---

## Testování aplikace

### 1. Ověření Frontendu
Otevři http://localhost:3000

Měl by se zobrazit dashboard se 2 kartami:
- **Konfigurace Lig** - zobrazí předpřipravené ligy
- **Import Dat** - umožní spustit historický import

### 2. Test API Endpoints
```bash
# Health check
curl http://localhost:3001/health

# Získat všechny ligy
curl http://localhost:3001/api/config/leagues

# Získat sporty
curl http://localhost:3001/api/config/sports
```

### 3. Průzkum databáze
```bash
# Připojit se přes psql
docker exec -it sazkomat-postgres psql -U sazkomat -d sazkomat_db

# Zobrazit tabulky
\dt configuration.*
\dt data_import.*

# Dotazy
SELECT * FROM configuration.leagues;
SELECT * FROM configuration.sports;
```

---

## Co najdeš v aplikaci

### Home (localhost:3000)
- Dashboard s přehledem
- 2 hlavní karty:
  - **Konfigurace Lig** → správa sportovních lig
  - **Import Dat** → spuštění historického importu

### Leagues (localhost:3000/leagues)
- Seznam 5 předpřipravených lig:
  - 🏴󠁧󠁢󠁥󠁮󠁧󠁿 Premier League (England)
  - 🇪🇸 La Liga (Spain)
  - 🇩🇪 Bundesliga (Germany)
  - 🇮🇹 Serie A (Italy)
  - 🇫🇷 Ligue 1 (France)
- Všechny jsou defaultně neaktivní

### Import (localhost:3000/import)
- Výběr lig pro import
- Zadání sezón (např: 2023-2024, 2022-2023)
- Spuštění importu
- Monitoring progress

---

## Známé problémy

⚠️ **Scraper je placeholder**
- Import částečně selže
- FootballBetExplorerScraper potřebuje implementovat skutečný HTML parsing
- Soubor: `src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs`

---

## Pokud něco nefunguje

1. **Zkontroluj Docker Desktop** - musí běžet
2. **Restartuj služby**: `docker-compose restart`
3. **Zkontroluj logy**: `docker-compose logs -f`
4. **Kompletní reset**: `docker-compose down -v && docker-compose up -d`

---

## Další informace

📄 Kompletní dokumentace: `CLAUDE.md`
📄 Docker dokumentace: `DOCKER.md`
📄 Test výsledky: `docs/testing/PRIORITY_1_TEST_RESULTS.md`
