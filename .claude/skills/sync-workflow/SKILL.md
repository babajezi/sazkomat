---
name: sync-workflow
description: Provede celý sync workflow pro ligu - detekce sezón, sync dat, report výsledků. Použij když uživatel chce synchronizovat ligu nebo sezónu.
allowed-tools: Bash, Read, Grep, WebFetch
argument-hint: "[league_slug nebo league_id]"
---

# Sync Workflow pro Sazkomat

Proveď kompletní sync workflow pro zadanou ligu: $ARGUMENTS

## Postup

### 1. Identifikace ligy
```bash
# Pokud je argument slug (obsahuje /), najdi ligu podle slugu
curl -s "http://localhost:3001/api/config/leagues" | jq '.[] | select(.betExplorerSlug | contains("ARGUMENT"))'

# Pokud je argument UUID, použij přímo
```

### 2. Zkontroluj dostupné sezóny
```bash
curl -s "http://localhost:3001/api/config/league-seasons?leagueId=LEAGUE_ID" | jq '.[] | {seasonName: .season.name, hasData, syncEnabled, noDataReason}'
```

### 3. Detekuj aktuální sezóny (pokud ještě nebyly detekovány)
```bash
curl -X POST "http://localhost:3001/api/sync/seasons/detect-current" \
  -H "Content-Type: application/json" \
  -d '{"providerId": "BETEXPLORER_PROVIDER_ID"}'
```

### 4. Sync dat pro sezóny s SyncEnabled=true
```bash
curl -X POST "http://localhost:3001/api/sync/seasons/data/LEAGUE_ID/SEASON_ID" \
  -H "Content-Type: application/json" \
  -d '{"providerId": "BETEXPLORER_PROVIDER_ID", "forceUpdate": false}'
```

### 5. Report výsledků

Zobraz přehlednou tabulku:
- Které sezóny byly synchronizovány
- Počet kol a zápasů
- Případné chyby (NoDataReason)

## Poznámky

- BetExplorer provider ID: najdi v `data_providers` tabulce kde `code = 'betexplorer'`
- Pokud sync selže, zkontroluj logy: `docker-compose logs --tail=50 api`
- Pro force update použij `forceUpdate: true`
