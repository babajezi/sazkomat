Otestuj scraping recipe na konkrétní ligu a sezónu.

Argumenty: $ARGUMENTS (očekává se ve formátu "league_slug season" např. "england/premier-league 2023-2024")

Postup:
1. Parsuj argumenty - první je slug ligy, druhý je sezóna
2. Najdi ID ligy podle slugu: `curl -s "http://localhost:3001/api/config/leagues" | jq '.[] | select(.betExplorerSlug | contains("SLUG"))'`
3. Najdi aktivní recepty: `curl -s "http://localhost:3001/api/recipes"`
4. Pro každý aktivní recept zavolej test endpoint:
   ```
   curl -X POST "http://localhost:3001/api/recipes/{recipeId}/test" \
     -H "Content-Type: application/json" \
     -d '{"leagueId": "LEAGUE_ID", "season": "SEASON"}'
   ```
5. Zobraz výsledky - který recept fungoval, kolik kol/zápasů našel
