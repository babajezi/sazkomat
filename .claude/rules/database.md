# Databázová pravidla

## VŽDY SE ZEPTEJ před manipulací s daty

**Před DELETE nebo UPDATE** na produkčních datech se **VŽDY ZEPTEJ** uživatele.

Platí pro:
- Mazání kol, zápasů, sezón, lig, league_seasons
- Jakékoli UPDATE na existujících datech
- Hromadné operace

**Důvod**: Při vývoji mám tendenci řešit nesouvisející věci, což může rozbít datovou konzistenci.

**Výjimka**: Pouze pokud je mazání/úprava explicitně součástí schváleného plánu.

## Příklad správného postupu

```
Claude: "Našel jsem 88 kol s chybnými daty pro Scotland League Two 1994-1998.
         Mám je smazat?"
Uživatel: "Ano, smaž je."
Claude: [provede DELETE]
```

## Příklad ŠPATNÉHO postupu

```
Claude: [bez ptaní provede DELETE na datech, která "vypadají špatně"]
```

## Databázové konvence

- **snake_case** pro názvy tabulek a sloupců
- **JSONB** pro komplexní data (metadata, progress, raw provider data)
- **UUID** jako primární klíče
- **timestamptz** pro všechny datetime sloupce (UTC)
- Auto timestamps: `created_at`, `updated_at` na všech entitách
