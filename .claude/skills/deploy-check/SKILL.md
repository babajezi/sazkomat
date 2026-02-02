---
name: deploy-check
description: Ověří že vše je připraveno pro deploy - testy projdou, migrace jsou aplikovány, build funguje, git je čistý. Použij před deployem do produkce.
allowed-tools: Bash, Read
---

# Deploy Check pro Sazkomat

Ověř připravenost pro deploy.

## Checklist

### 1. Git stav
```bash
# Čistý working tree?
git status --short

# Jsme na main branch?
git branch --show-current

# Máme všechno pushnuté?
git log origin/main..HEAD --oneline
```

### 2. Testy
```bash
cd tests/Sazkomat.Tests
dotnet test --logger "console;verbosity=minimal"
```

Všechny testy musí projít. Pokud některé selhávají, STOP - nelze deployovat.

### 3. Build
```bash
# Backend build
cd src/Sazkomat.Api
dotnet build --configuration Release

# Frontend build
cd frontend
npm run build
```

Oba buildy musí projít bez chyb.

### 4. Migrace
```bash
# Zkontroluj pending migrace
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "
SELECT migration_id, product_version
FROM __EFMigrationsHistory
ORDER BY migration_id DESC
LIMIT 5;
"

# Porovnej s migracemi v kódu
ls -la src/Sazkomat.Configuration/Migrations/*.cs | tail -5
ls -la src/Sazkomat.DataImport/Migrations/*.cs | tail -5
```

### 5. Environment
```bash
# Docker services běží?
docker-compose ps

# API health check
curl -s http://localhost:3001/health

# Žádné kritické chyby v logu?
docker-compose logs --tail=20 api | grep -i "error\|exception\|failed"
```

### 6. Databázová integrita
```bash
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "
-- Orphaned records check
SELECT 'orphaned rounds' as check, COUNT(*) as count
FROM data_import.rounds r
LEFT JOIN configuration.leagues l ON l.id = r.league_id
WHERE l.id IS NULL
UNION ALL
SELECT 'orphaned matches', COUNT(*)
FROM data_import.matches m
LEFT JOIN data_import.rounds r ON r.id = m.round_id
WHERE r.id IS NULL;
"
```

## Výstup

### Deploy Ready ✅
Pokud vše prošlo:
```
✅ Git: čistý, na main, vše pushnuté
✅ Testy: X/X prošlo
✅ Build: backend OK, frontend OK
✅ Migrace: synchronizované
✅ Services: běží, healthy
✅ DB: bez orphaned records

READY TO DEPLOY
```

### Deploy Blocked ❌
Pokud něco selhalo:
```
❌ [Konkrétní problém]
   Řešení: [Jak opravit]

DEPLOY BLOCKED - oprav problémy a spusť znovu
```
