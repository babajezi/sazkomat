# Pravidlo: Aktualizace DATABASE_SCHEMA.md

Při změnách v databázovém schématu **VŽDY aktualizuj** `docs/DATABASE_SCHEMA.md`:

## Kdy aktualizovat

1. **Přidání migrace** - nová tabulka, sloupec, index, FK
2. **Změna entity** - nová property, změna typu, přejmenování
3. **Změna indexů nebo FK** - přidání, odebrání, změna
4. **Změna enum hodnot** - nové hodnoty, přejmenování

## Co aktualizovat

- Definici tabulky (sloupce, typy, constraints)
- Indexy
- Foreign keys
- Enum hodnoty (jako stringy)
- JSONB field dokumentaci

## Umístění

`docs/DATABASE_SCHEMA.md`
