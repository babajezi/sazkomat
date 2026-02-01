Aktualizuj dokumentaci databázového schématu v docs/DATABASE_SCHEMA.md.

Postup:
1. Projdi entity v:
   - `src/Sazkomat.Configuration/Entities/*.cs`
   - `src/Sazkomat.DataImport/Entities/*.cs`
2. Projdi EF konfigurace v:
   - `src/Sazkomat.Configuration/Data/Configurations/*.cs`
   - `src/Sazkomat.DataImport/Data/Configurations/*.cs`
3. Zkontroluj nejnovější migrace pro případné změny
4. Porovnej s aktuálním docs/DATABASE_SCHEMA.md
5. Aktualizuj:
   - Přidej nové tabulky/sloupce
   - Odeber smazané
   - Aktualizuj typy a constraints
   - Aktualizuj enum hodnoty
6. Zachovej formátování a strukturu existujícího souboru
7. Informuj uživatele co se změnilo
