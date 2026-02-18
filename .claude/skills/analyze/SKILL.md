---
name: analyze
description: Vytvoří nebo upraví analytický pohled na základě popisu v přirozeném jazyce. Spustí analýzu a zobrazí výsledky.
allowed-tools: Bash, Read, Grep, Glob
---

# Analytický engine - Skill /analyze

Tento skill slouží k interaktivní práci s analytickým enginem Sazkomatu. Uživatel popíše požadovanou analýzu přirozeným jazykem a Claude Code sestaví ViewSpec, spustí ji a zobrazí výsledky.

## API

- `POST /api/analytics/execute` — spustí ad-hoc ViewSpec, vrátí AnalyticsResult
- `POST /api/analytics/views` — uloží pohled (CreateViewRequest: name, description, spec, tags)
- `PUT /api/analytics/views/{id}` — aktualizuje pohled
- `GET /api/analytics/views` — seznam uložených pohledů
- `POST /api/analytics/views/{id}/execute` — spustí uložený pohled
- `GET /api/analytics/metadata` — dostupné dimenze, metriky, sloupce

## ViewSpec formát

```json
{
  "dataSource": "matches",        // "matches" | "rounds"
  "filters": {
    "leagueIds": ["uuid"],         // filtr na ligy
    "countryIds": ["uuid"],        // filtr na země
    "seasonNames": ["2023-2024"],  // filtr na sezóny
    "dateRange": { "from": "2020-01-01", "to": "2024-12-31" },
    "results": ["H", "D", "A"],    // filtr na výsledky (jen matches)
    "hasOdds": true,               // jen zápasy s kurzy
    "oddsRange": { "column": "home_odds", "min": 1.5, "max": 3.0 },
    "minMatches": 100,             // HAVING COUNT(*) >= N
    "fieldComparisons": [          // pokročilé filtry na sloupce
      { "left": "home_wins", "operator": "=", "right": "matches_count" }
    ]
  },
  "groupBy": ["country", "league"], // dimenze
  "metrics": [
    { "type": "count", "alias": "total" },
    { "type": "resultPercentage", "result": "H", "alias": "home_pct" },
    { "type": "average", "column": "home_odds", "alias": "avg_home_odds" },
    { "type": "roi", "column": "home_odds", "result": "H", "alias": "home_roi" },
    { "type": "goalAverage", "alias": "avg_goals" }
  ],
  "sort": { "column": "total", "direction": "desc" },
  "limit": 50,
  "visualization": { "type": "barChart" }
}
```

## Dostupné dimenze (groupBy)

| Dimenze | Popis |
|---------|-------|
| league | Název ligy |
| country | Název země |
| season | Název sezóny |
| result | Výsledek zápasu (H/D/A) — jen matches |
| month | Rok-měsíc (YYYY-MM) |
| year | Rok |
| oddsRange | Rozsah domácích kurzů — jen matches |
| homeTeam | Domácí tým — jen matches |
| awayTeam | Hostující tým — jen matches |
| round | Číslo kola |
| group | Název skupiny (pro skupinové ligy) |

## Dostupné metriky

| Typ | Popis | Parametry |
|-----|-------|-----------|
| count | Počet řádků | — |
| resultPercentage | Procento výsledku | result: H/D/A |
| average | Průměr sloupce | column |
| sum | Součet sloupce | column |
| min | Minimum | column |
| max | Maximum | column |
| stddev | Směrodatná odchylka | column |
| roi | Return on Investment (flat betting) | column (odds), result |
| impliedProbability | Průměrná implikovaná pravděpodobnost | column (odds) |
| valueGap | Skutečná výhra% − implikovaná% | column (odds), result |
| goalAverage | Průměrný počet gólů | — |

## Dostupné sloupce

**Matches:** home_score, away_score, home_odds, draw_odds, away_odds
**Rounds:** round_number, matches_count, home_wins, draws, away_wins, cumulative_odds_home, cumulative_odds_draw, cumulative_odds_away

## Vizualizace

`table` (výchozí), `barChart`, `lineChart`, `pieChart`

## Workflow

1. Uživatel popíše co chce analyzovat
2. Sestav ViewSpec na základě popisu
3. Zavolej `curl -s -X POST http://localhost:3001/api/analytics/execute -H "Content-Type: application/json" -d '<spec>'`
4. Zobraz výsledky v přehledné tabulce
5. Pokud uživatel chce uložit: zavolej `POST /api/analytics/views`
6. Pokud chce upravit: iteruj na spec a znovu spusť

## Příklady

### "Jaká je úspěšnost domácích v top ligách?"
```bash
curl -s -X POST http://localhost:3001/api/analytics/execute \
  -H "Content-Type: application/json" \
  -d '{"dataSource":"matches","groupBy":["country","league"],"metrics":[{"type":"count","alias":"matches"},{"type":"resultPercentage","result":"H","alias":"home_win_pct"},{"type":"goalAverage","alias":"avg_goals"}],"sort":{"column":"matches","direction":"desc"},"limit":20,"filters":{"minMatches":1000}}'
```

### "ROI na remízy v anglické Premier League"
```bash
curl -s -X POST http://localhost:3001/api/analytics/execute \
  -H "Content-Type: application/json" \
  -d '{"dataSource":"matches","groupBy":["season"],"metrics":[{"type":"count","alias":"matches"},{"type":"resultPercentage","result":"D","alias":"draw_pct"},{"type":"roi","column":"draw_odds","result":"D","alias":"draw_roi"},{"type":"average","column":"draw_odds","alias":"avg_draw_odds"}],"sort":{"column":"season","direction":"asc"},"filters":{"hasOdds":true}}'
```

## Poznámky

- Pokud potřebuješ ID ligy/země, najdi je: `curl -s http://localhost:3001/api/config/leagues?search=Premier`
- Pro uložení vždy přidej smysluplný název a tags
- Frontend zobrazuje uložené pohledy na `/analytics`
