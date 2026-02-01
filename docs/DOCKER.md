# 🐳 Docker Setup - Sazkomat

## Přehled služeb

Docker Compose orchestruje následující služby:

| Služba | Port | Popis |
|--------|------|-------|
| **Frontend (Next.js)** | 3000 | Dashboard UI |
| **API (.NET 9)** | 3001 | REST API backend |
| **PostgreSQL** | 3002 | Hlavní databáze (OLTP) |
| **Redis** | 3003 | Cache (připraveno pro budoucnost) |
| **pgAdmin** | 3004 | Database management UI (optional) |

## Požadavky

- Docker Desktop 4.0+
- Docker Compose 2.0+
- Minimálně 4 GB RAM pro všechny kontejnery

## Rychlý start

### 1. Spuštění všech služeb

```bash
# Spustit všechny služby na pozadí
docker-compose up -d

# Sledovat logy všech služeb
docker-compose logs -f

# Sledovat logy konkrétní služby
docker-compose logs -f api
docker-compose logs -f frontend
```

### 2. První migrace databáze

Migrace se spustí automaticky při startu API kontejneru. Pokud ne, můžete je spustit ručně:

```bash
# Spustit migrace ručně
docker-compose exec api dotnet ef database update
```

### 3. Přístup k aplikaci

- **Frontend Dashboard**: http://localhost:3000
- **API**: http://localhost:3001
- **API Health Check**: http://localhost:3001/health
- **pgAdmin**: http://localhost:3004
  - Email: `admin@sazkomat.local`
  - Password: `admin123`

### 4. Zastavení služeb

```bash
# Zastavit všechny služby (zachovat data)
docker-compose down

# Zastavit a smazat všechna data (volumes)
docker-compose down -v
```

## Vývojářské příkazy

### Rebuild služby po změně kódu

```bash
# Rebuild a restart API
docker-compose up -d --build api

# Rebuild a restart Frontendu
docker-compose up -d --build frontend

# Rebuild všech služeb
docker-compose up -d --build
```

### Přístup do kontejneru

```bash
# Bash do API kontejneru
docker-compose exec api bash

# Bash do Frontend kontejneru
docker-compose exec frontend sh

# PostgreSQL CLI
docker-compose exec postgres psql -U sazkomat -d sazkomat_db
```

### Přístup do databáze

#### Přes pgAdmin (webové rozhraní)

1. Otevři http://localhost:3004
2. Přihlaš se (admin@sazkomat.local / admin123)
3. Přidej server:
   - **Name:** Sazkomat PostgreSQL (libovolný název)
   - **Host:** `postgres` (název služby v Docker Compose)
   - **Port:** `5432` (interní port kontejneru)
   - **Maintenance database:** `sazkomat_db`
   - **Username:** `sazkomat`
   - **Password:** `sazkomat123`

#### Přes příkazovou řádku (psql)

```bash
docker exec -it sazkomat-postgres psql -U sazkomat -d sazkomat_db

# Užitečné SQL příkazy:
\dt configuration.*     # Seznam tabulek v configuration schématu
\dt data_import.*       # Seznam tabulek v data_import schématu
SELECT * FROM configuration.leagues;
SELECT * FROM configuration.sports;
SELECT * FROM data_import.rounds;
```

### Entity Framework migrace

```bash
# Vytvořit novou migraci
docker-compose exec api dotnet ef migrations add MigrationName

# Aplikovat migrace
docker-compose exec api dotnet ef database update

# Rollback migrace
docker-compose exec api dotnet ef database update PreviousMigrationName
```

### Logy a debugging

```bash
# Real-time logy API
docker-compose logs -f api

# Real-time logy Frontendu
docker-compose logs -f frontend

# Zobrazit posledních 100 řádků
docker-compose logs --tail=100 api
```

## Konfigurace prostředí

### PostgreSQL

```yaml
POSTGRES_USER: sazkomat
POSTGRES_PASSWORD: sazkomat123
POSTGRES_DB: sazkomat_db
```

### .NET API

Connection string je automaticky nastavený v `docker-compose.yml`:

```
Host=postgres;Port=5432;Database=sazkomat_db;Username=sazkomat;Password=sazkomat123
```

### Next.js Frontend

API URL je nastavené na:

```
NEXT_PUBLIC_API_URL=http://localhost:3001
```

## Volumes (Persistentní data)

Docker Compose vytváří následující volumes pro uchování dat:

- `postgres_data` - PostgreSQL data
- `redis_data` - Redis cache
- `pgadmin_data` - pgAdmin konfigurace

## Healthchecks

Všechny služby mají nakonfigurované health checks:

```bash
# Zkontrolovat status všech kontejnerů
docker-compose ps

# Detail o health checku
docker inspect --format='{{json .State.Health}}' sazkomat-api | jq
```

## Troubleshooting

### Služba se nespustí

```bash
# Zkontrolovat logy
docker-compose logs service-name

# Restartovat službu
docker-compose restart service-name

# Force rebuild
docker-compose up -d --build --force-recreate service-name
```

### Databázové problémy

```bash
# Reset databáze (POZOR: smaže všechna data!)
docker-compose down -v
docker-compose up -d postgres
docker-compose exec postgres psql -U sazkomat -d sazkomat_db
```

### Port již používán

Všechny služby běží na portech 3000-3004. Pokud je některý port obsazený:

1. **Automatické řešení**: Použijte startup skripty, které automaticky ukončí procesy na těchto portech:
   - Windows: `.\scripts\start-dev.ps1`
   - Linux/Mac: `./scripts/start-dev.sh`

2. **Manuální řešení**: Zastavte službu používající port nebo změňte port v `docker-compose.yml`

## Production deployment

Pro produkční nasazení:

1. Změňte `ASPNETCORE_ENVIRONMENT=Production`
2. Změňte databázové hesla
3. Použijte `.env` soubor pro secrets
4. Odeberte pgAdmin službu
5. Zvažte použití orchestrátoru (Kubernetes)

## Nextup - Kubernetes

Připraveno pro ArgoCD + Kubernetes deployment (Fáze 2).
