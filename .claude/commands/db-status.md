Zobraz počty záznamů v hlavních databázových tabulkách:

Spusť tento SQL přes psql:

```bash
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "
SELECT 'sports' as table_name, COUNT(*) as count FROM configuration.sports
UNION ALL SELECT 'countries', COUNT(*) FROM configuration.countries
UNION ALL SELECT 'leagues', COUNT(*) FROM configuration.leagues
UNION ALL SELECT 'seasons', COUNT(*) FROM configuration.seasons
UNION ALL SELECT 'league_seasons', COUNT(*) FROM configuration.league_seasons
UNION ALL SELECT 'data_providers', COUNT(*) FROM configuration.data_providers
UNION ALL SELECT 'league_providers', COUNT(*) FROM configuration.league_providers
UNION ALL SELECT 'country_providers', COUNT(*) FROM configuration.country_providers
UNION ALL SELECT 'rounds', COUNT(*) FROM data_import.rounds
UNION ALL SELECT 'matches', COUNT(*) FROM data_import.matches
UNION ALL SELECT 'sync_jobs', COUNT(*) FROM data_import.sync_jobs
UNION ALL SELECT 'unmatched_leagues', COUNT(*) FROM data_import.unmatched_leagues
UNION ALL SELECT 'scraper_recipes', COUNT(*) FROM data_import.scraper_recipes
ORDER BY table_name;
"
```

Zobraz výsledek v přehledné tabulce.
