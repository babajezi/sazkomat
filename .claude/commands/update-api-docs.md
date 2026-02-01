Aktualizuj dokumentaci API endpointů v docs/API.md.

Postup:
1. Projdi všechny soubory v `src/Sazkomat.Api/Endpoints/*.cs`
2. Extrahuj všechny endpointy (MapGet, MapPost, MapPut, MapPatch, MapDelete)
3. Pro každý endpoint zjisti:
   - HTTP metodu
   - Cestu
   - Stručný popis (z komentáře nebo názvu metody)
4. Porovnej s aktuálním docs/API.md
5. Aktualizuj docs/API.md - přidej nové endpointy, odeber smazané
6. Zachovej formátování a strukturu existujícího souboru
7. Informuj uživatele co se změnilo
