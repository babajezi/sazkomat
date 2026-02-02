---
name: analyze-failures
description: Analyzuje selhané scrapingy, najde vzory v chybách, navrhne nové recipes nebo opravy. Použij po hromadném sync když některé ligy selhaly.
allowed-tools: Bash, Read, Grep, Glob
---

# Analýza selhaných scrapingů

Analyzuj selhané scrapingy a navrhni řešení.

## Postup

### 1. Najdi sezóny s chybami
```bash
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "
SELECT
  l.name as league,
  s.name as season,
  ls.no_data_reason,
  ls.no_data_note,
  ls.last_scraped_at
FROM configuration.league_seasons ls
JOIN configuration.leagues l ON l.id = ls.league_id
JOIN configuration.seasons s ON s.id = ls.season_id
WHERE ls.no_data_reason IS NOT NULL
  AND ls.no_data_reason != 'None'
ORDER BY ls.last_scraped_at DESC
LIMIT 50;
"
```

### 2. Seskup chyby podle typu
```bash
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "
SELECT
  no_data_reason,
  COUNT(*) as count
FROM configuration.league_seasons
WHERE no_data_reason IS NOT NULL AND no_data_reason != 'None'
GROUP BY no_data_reason
ORDER BY count DESC;
"
```

### 3. Zkontroluj recipes statistiky
```bash
curl -s "http://localhost:3001/api/recipes/stats" | jq '.[] | {name, totalAttempts, successfulAttempts, successRate}'
```

### 4. Analyzuj vzory

Pro každý typ chyby:

**PageNotFound:**
- Liga možná neexistuje na BetExploreru
- Zkontroluj URL: `https://www.betexplorer.com/soccer/{country}/{league}/results/`

**NoRoundsFound:**
- Stránka existuje ale parser nenašel kola
- Možná jiná HTML struktura → potřeba nový recipe

**ParsingError:**
- Recipe selhalo při parsování
- Zkontroluj selektory v recipe

**NoRecipeFound:**
- Žádný recipe nefunguje
- Potřeba vytvořit nový recipe

### 5. Navrhni řešení

Pro každou skupinu chyb navrhni:
1. Konkrétní opravu (nový recipe, úprava selektoru)
2. Jak otestovat opravu
3. Které ligy budou opraveny

## Výstup

Vytvoř report s:
- Shrnutí problémů
- Navržená řešení seřazená podle dopadu (kolik lig opraví)
- Konkrétní kroky k implementaci
