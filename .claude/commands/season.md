Sync a analýza výsledků pro konkrétní ligu a sezónu.

Argumenty: $ARGUMENTS (formát: "country league_slug season", např. "france ligue-1 2023-2024")

## Postup

### 1. Parsuj argumenty
- `$1` = country (např. "france", "fr")
- `$2` = league slug (např. "ligue-1", "premier-league")
- `$3` = season (např. "2023-2024", "2024")

### 2. Najdi ligu v databázi
```bash
# Hledej podle BetExplorer slugu (obsahuje country/league)
curl -s "http://localhost:3001/api/config/leagues" | jq --arg country "$1" --arg league "$2" '.[] | select(.betExplorerSlug | test($country + ".*" + $league; "i"))'
```

Zapiš si `leagueId` a `betExplorerSlug`.

### 3. Najdi sezónu
```bash
curl -s "http://localhost:3001/api/config/league-seasons?leagueId=LEAGUE_ID" | jq --arg season "$3" '.[] | select(.season.name == $season)'
```

Zapiš si `leagueSeasonId` a `seasonId`.

### 4. Najdi BetExplorer provider ID
```bash
curl -s "http://localhost:3001/api/config/providers" | jq '.[] | select(.code == "betexplorer") | .id'
```

### 5. Spusť sync pro sezónu
```bash
curl -X POST "http://localhost:3001/api/sync/seasons/data/LEAGUE_ID/SEASON_ID" \
  -H "Content-Type: application/json" \
  -d '{"providerId": "PROVIDER_ID", "forceUpdate": true}'
```

### 6. Počkej na dokončení a zkontroluj výsledek
```bash
# Počkej 10-30 sekund podle velikosti ligy
sleep 15

# Zkontroluj stav league_season
curl -s "http://localhost:3001/api/config/league-seasons?leagueId=LEAGUE_ID" | jq --arg season "$3" '.[] | select(.season.name == $season) | {
  season: .season.name,
  hasData: .hasData,
  hasOdds: .hasOdds,
  roundsCount: .roundsCount,
  matchesCount: .matchesCount,
  noDataReason: .noDataReason,
  noDataNote: .noDataNote,
  lastScrapedAt: .lastScrapedAt
}'
```

### 7. Zobraz importovaná kola
```bash
curl -s "http://localhost:3001/api/import/rounds?leagueId=LEAGUE_ID&season=$3" | jq '.items | .[:5] | .[] | {
  roundNumber: .roundNumber,
  groupName: .groupName,
  matchesCount: .matchesCount,
  homeWins: .homeWins,
  draws: .draws,
  awayWins: .awayWins,
  oddsComplete: .oddsComplete
}'
```

### 8. Analýza výsledků

Zobraz shrnutí:
- ✅/❌ Status syncu
- Počet kol a zápasů
- Rozložení výsledků (H/D/A)
- Kompletnost kurzů
- Případné chyby (noDataReason, noDataNote)

Pokud sync selhal:
- Zkontroluj logy: `docker-compose logs --tail=30 api | grep -i "error\|exception"`
- Navrhni možnou příčinu a řešení
