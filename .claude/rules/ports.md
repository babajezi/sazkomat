# Kritické porty projektu

**PORTY SE NESMÍ MĚNIT BEZ EXPLICITNÍHO ODSOUHLASENÍ UŽIVATELE**

## Standardní porty

| Služba | Port | URL |
|--------|------|-----|
| Frontend | 3000 | http://localhost:3000 |
| API | 3001 | http://localhost:3001 |
| PostgreSQL | 3002 | localhost:3002 |
| Redis | 3003 | localhost:3003 |
| pgAdmin | 3004 | http://localhost:3004 |

## Pravidla

1. **VŽDY SE ZEPTEJ** uživatele před jakoukoli změnou portu
2. Nikdy neměň stávající port mappings bez souhlasu
3. Pokud potřebuješ použít nový port, konzultuj s uživatelem

## Credentials

| Služba | Credentials |
|--------|-------------|
| pgAdmin | admin@sazkomat.local / admin123 |
| PostgreSQL | sazkomat / sazkomat123 |
