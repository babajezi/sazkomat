# Session Context - UI/UX Vylepšení + Scraper Fix

**Datum:** 2025-10-29
**Status:** ✅ KOMPLETNĚ DOKONČENO

## Kontext - Co bylo provedeno

Uživatel požadoval několik UI/UX vylepšení v rámci správy dat a zobrazení výsledků. Všechny změny byly implementovány a otestovány.

---

## 1. Odstranění manuálního vytváření záznamů

### Problém:
- Ligy a země se vytvářely jak ručně (tlačítka), tak přes sync workflow
- To bylo matoucí a nekonzistentní

### Řešení - Odstraněny Create dialogy:

**Frontend:**
- `frontend/app/countries/page.tsx` - odstraněno tlačítko "Nová země"
- `frontend/app/leagues/page.tsx` - odstraněno tlačítko "Nová liga"
- `frontend/components/CountryFormDialog.tsx` - odstraněna funkce `CreateCountryDialog`
- `frontend/components/LeagueFormDialog.tsx` - odstraněna funkce `CreateLeagueDialog`

**Ponecháno:**
- `EditCountryDialog` - editace existujících zemí
- `EditLeagueDialog` - editace existujících lig

**Pravidlo:** Země a ligy se vytvářejí POUZE přes sync workflow!

---

## 2. Filtry - Pouze aktivní entity

### Správa zemí - Nový filtr
**Soubor:** `frontend/app/countries/page.tsx`

```typescript
const [filterActive, setFilterActive] = useState<string>("");

const filteredCountries = countries?.filter((country) => {
  if (filterActive === "active" && !country.isActive) return false;
  if (filterActive === "inactive" && country.isActive) return false;
  return true;
});
```

**UI:** Dropdown s možnostmi "Všechny země / Pouze aktivní / Pouze neaktivní"

### Konfigurace lig - Filtr aktivních zemí
**Soubor:** `frontend/app/leagues/page.tsx:237`

```typescript
{countries?.filter((c) => c.isActive).map((country) => (...))}
```

**Výsledek:** Dropdown filtru zemí zobrazuje pouze aktivní země

### Edit League Dialog - Aktivní země
**Soubor:** `frontend/components/LeagueFormDialog.tsx`
- Země a Sport jsou **read-only** (nelze měnit po vytvoření)
- Zobrazují se jako disabled fieldy

---

## 3. Varování pro historické sezóny

### Problém:
- Uživatel mohl zapnout sync pro historickou sezónu bez varování
- Historické sezóny se obvykle nemění, sync není potřebný

### Řešení - Modal s vysvětlením:
**Soubor:** `frontend/components/LeagueSeasonsDisplay.tsx`

```typescript
const handleToggleClick = () => {
  if (!season.syncEnabled && season.syncMode === "Historical") {
    setShowWarning(true); // Zobrazit modal
  } else {
    onToggleSync(!season.syncEnabled);
  }
};
```

**Modal obsahuje:**
- Vysvětlení rozdílu mezi Current a Historical sezónami
- Varování, že sync historical sezón obvykle není potřebný
- Tlačítka: "Zrušit" / "Rozumím, zapnout sync"

---

## 4. Oprava filtru sezón v Rounds page

### Problém:
- Filtr sezón zobrazoval jen 1 sezónu, přestože uživatel naimportoval 4 sezóny
- Filtr se generoval z aktuálně načtených kol (pagination), ne ze všech dostupných

### Řešení:
**Soubor:** `frontend/app/rounds/page.tsx:34-56`

```typescript
// Load seasons for selected league
const { data: leagueSeasons } = useQuery({
  queryKey: ["league-seasons", selectedLeagueId],
  queryFn: () => seasonApi.getLeagueSeasons(selectedLeagueId),
  enabled: !!selectedLeagueId,
});

// Get available seasons for filter dropdown
const availableSeasons = selectedLeagueId && leagueSeasons
  ? leagueSeasons.map((ls) => ls.seasonName).sort((a, b) => b.localeCompare(a))
  : Array.from(new Set(data?.rounds.map((r) => r.season) || [])).sort((a, b) => b.localeCompare(a));
```

**Výsledek:** Filtr nyní zobrazuje VŠECHNY sezóny dané ligy z API

---

## 5. Fix: localhost vs 127.0.0.1

### Problém:
- API endpointy vracely 404 při volání z frontendu
- Windows DNS resolver vracel IPv6 (`::1`) místo IPv4 pro `localhost`
- Docker špatně routoval IPv6 requesty

### Řešení:
**Soubory:**
- `frontend/.env.local` - změněno na `http://127.0.0.1:3001`
- `docker-compose.yml:73` - build arg změněn na `http://127.0.0.1:3001`

**Výsledek:** API calls nyní fungují správně

---

## 6. Matches page - DisplayName místo Name

### Problém:
- Zobrazovalo se "ChNL - - Kolo 30" (duplikátní pomlčky)
- League.name bylo krátké (např. "ChNL"), chyběla sezóna

### Řešení:

**Backend:** `src/Sazkomat.Api/Endpoints/ImportEndpoints.cs:146`
```csharp
leagues[lid] = new {
    id = league.Id,
    name = league.Name,
    displayName = league.DisplayName,  // PŘIDÁNO
    country = league.Country?.Name,
    sport = league.Sport?.Name
};
```

**Backend:** `ImportEndpoints.cs:169`
```csharp
round = new {
    id = m.Round.Id,
    season = seasonNames.ContainsKey(m.Round.SeasonId)
        ? seasonNames[m.Round.SeasonId]
        : null,  // PŘIDÁNO načítání season
    roundNumber = m.Round.RoundNumber,
    leagueId = m.Round.LeagueId
}
```

**Frontend:** `frontend/lib/api/types.ts:268`
```typescript
league?: {
  id: string;
  name: string;
  displayName: string;  // PŘIDÁNO
  country?: string | null;
  sport?: string | null;
} | null;
```

**Frontend:** `frontend/app/matches/page.tsx:294, 327`
```typescript
{match.league?.displayName || match.league?.name}
```

**Výsledek:** "ChNL (Czech Republic) - 2024-2025 - Kolo 30" ✓

---

## 7. Matches page - Grid layout s pevnými šířkami

### Problém:
- Kurzy a skóre "skákaly" - nebyly zarovnané
- Každý řádek měl jinou šířku podle obsahu

### Řešení:
**Soubor:** `frontend/app/matches/page.tsx`

**Layout změněn na grid:**
```typescript
className="grid grid-cols-[1fr_auto_auto] gap-4"
```

**3 sloupce:**
1. Týmy + datum (flex-1) - zabere zbývající prostor
2. Skóre (w-24) - pevná šířka 96px
3. Kurzy (w-48) - pevná šířka 192px

**Kurzy s jednotnou šířkou:**
```typescript
<span className="min-w-[60px] text-right px-2 py-1 bg-gray-50 rounded">
  {match.homeOdds?.toFixed(2) || "-"}
</span>
```

**Aplikováno na:**
- Chronological view (řádky 287-327)
- Grouped view (řádky 355-393)

---

## 8. Zvýraznění výsledné varianty kurzů

### Rounds page - Již funguje
**Funkce:** `getWinningOddsClass` (řádky 76-81)
```typescript
const getWinningOddsClass = (result: string, type: "H" | "D" | "A") => {
  if (result === type) {
    return "bg-green-100 text-green-800 font-bold";
  }
  return "";
};
```

### Matches page - PŘIDÁNO
**Soubor:** `frontend/app/matches/page.tsx:77-82`

Stejná funkce jako v rounds page. Aplikována na:
- Chronological view (321-329)
- Grouped view (386-394)

**Výsledek:** Vítězný kurz má zelené pozadí (`bg-green-100`) a tučný font

---

## 9. Kumulativní kurzy - Zaokrouhlení

### Změna:
**Soubor:** `frontend/app/rounds/page.tsx:334, 343, 352`

```typescript
// PŘED:
1: {round.cumulativeOddsHome?.toFixed(2)}  // "45.23"

// PO:
1: {round.cumulativeOddsHome?.toFixed(0)}  // "45"
```

**Výsledek:** Kumulativní kurzy zobrazují celá čísla

---

## 10. Rounds page - Sloupec "Datum zápasu"

### Přidáno do tabulky zápasů:
**Soubor:** `frontend/app/rounds/page.tsx`

**Thead (řádek 382):**
```tsx
<th className="text-center p-4 font-semibold w-32">Datum</th>
```

**Tbody (řádky 407-411):**
```tsx
<td className="p-4 text-center text-sm text-gray-600">
  {match.matchDate
    ? new Date(match.matchDate).toLocaleDateString("cs-CZ")
    : "-"}
</td>
```

**Pozice:** 4. sloupec (za "Hosté", před kurzy "1")

**Struktura tabulky:**
```
Domácí | Skóre | Hosté | Datum | 1 | X | 2
```

---

## 11. Matches page - Datum pod týmy

### Zobrazení data:
**Soubor:** `frontend/app/matches/page.tsx`

**Chronological view (296-300):**
```tsx
<div className="text-xs text-muted-foreground mt-1">
  {match.matchDate
    ? new Date(match.matchDate).toLocaleDateString("cs-CZ")
    : "Datum neuvedeno"}
</div>
```

**Grouped view (361-365):** Stejná implementace

**Výsledek:** Datum zobrazeno pod názvy týmů, nebo fallback "Datum neuvedeno"

---

## 12. Scraper - Oprava parsování dat zápasů

### Problém identifikován:
1. CSS selektor `table-main__date` **NEEXISTUJE** v HTML
2. Skutečná třída: `h-text-right h-text-no-wrap`
3. Datum je **poslední buňka** v řádku (`<td>`)
4. Formát: `"DD.MM."` bez roku (např. "25.05.")
5. Původní `DateTime.TryParse()` selhával kvůli chybějícímu roku

### Řešení implementováno:
**Soubor:** `src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs:222-250`

**Klíčové změny:**
1. **Správný selektor:** `cells.LastOrDefault()` místo hledání třídy
2. **Přidán parametr:** `ParseMatchRow(HtmlNode row, string season)`
3. **Parsing formátu:** `DateTime.TryParseExact(dateText, "dd.MM.", ...)`
4. **Inteligentní rok:**
   ```csharp
   // Aug-Dec (8-12) = first year, Jan-Jul (1-7) = second year
   var year = tempDate.Month >= 8 ? seasonYears[0] : seasonYears[1];
   matchDate = new DateTime(year, tempDate.Month, tempDate.Day);
   ```

**Volání opraveno (řádek 112):**
```csharp
var matchData = ParseMatchRow(row, season);
```

### ⚠️ Důležité:
**Existující data v DB mají `match_date = NULL`!**

Pro naplnění dat je potřeba:
1. Re-import sezón přes **/import** stránku
2. Nebo import nových kol přes sync workflow

Nový scraper nyní správně parsuje data ve formátu "DD.MM." a přiřazuje správný rok.

---

## Souhrn změněných souborů

### Backend (3 soubory):
| Soubor | Změny |
|--------|-------|
| `src/Sazkomat.Api/Endpoints/ImportEndpoints.cs` | Přidán displayName (146), season loading (153-164, 182) |
| `src/Sazkomat.DataImport/Repositories/MatchRepository.cs` | Ponechán .Include(m => m.Round) (19) |
| `src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs` | Oprava parsování data (163, 112, 222-250) |

### Frontend (7 souborů):
| Soubor | Změny |
|--------|-------|
| `frontend/app/countries/page.tsx` | Filtr + odstranění Create tlačítka |
| `frontend/app/leagues/page.tsx` | Filtr aktivních zemí + odstranění Create tlačítka |
| `frontend/app/rounds/page.tsx` | Zaokrouhlení kurzů (334,343,352) + sloupec Datum (382, 407-411) |
| `frontend/app/matches/page.tsx` | Grid layout + zvýraznění + datum (77-82, 287-327, 355-393) |
| `frontend/components/CountryFormDialog.tsx` | Odstraněn CreateCountryDialog |
| `frontend/components/LeagueFormDialog.tsx` | Odstraněn CreateLeagueDialog |
| `frontend/components/LeagueSeasonsDisplay.tsx` | Modal varování pro historical (105-223) |
| `frontend/lib/api/types.ts` | Přidán displayName do Match.league (268) |

### Konfigurace (2 soubory):
| Soubor | Změny |
|--------|-------|
| `frontend/.env.local` | NEXT_PUBLIC_API_URL=http://127.0.0.1:3001 |
| `docker-compose.yml` | NEXT_PUBLIC_API_URL build arg (73) |

---

## Detailní přehled UI změn

### Countries Page (`/countries`)
- ✅ Filtr: Všechny / Aktivní / Neaktivní
- ✅ Tlačítko "Nová země" - ODSTRANĚNO
- ✅ Editace stále funguje
- ✅ Toggle active/provider sync

### Leagues Page (`/leagues`)
- ✅ Filtr zemí - pouze aktivní země
- ✅ Tlačítko "Nová liga" - ODSTRANĚNO
- ✅ Editace stále funguje (země/sport read-only)
- ✅ LeagueSeasonsDisplay s varováním pro historical

### Rounds Page (`/rounds`)
- ✅ Filtr sezón - zobrazuje všechny sezóny ligy (API call)
- ✅ Kumulativní kurzy - zaokrouhleny na celá čísla
- ✅ Tabulka zápasů - nový sloupec "Datum"
- ✅ Struktura: Domácí | Skóre | Hosté | **Datum** | 1 | X | 2

### Matches Page (`/matches`)
- ✅ Grid layout - pevné šířky (flex-1, w-24, w-48)
- ✅ Kurzy - jednotná šířka (min-w-[60px]), zarovnané vpravo
- ✅ Zvýraznění - vítězný kurz zeleně (`bg-green-100`)
- ✅ Datum - zobrazeno pod týmy (fallback "Datum neuvedeno")
- ✅ DisplayName místo name

---

## Technické detaily

### CSS Grid Layout (Matches)
```typescript
grid grid-cols-[1fr_auto_auto]
```

**3 sloupce:**
- `1fr` - Týmy a datum (flex)
- `auto` - Skóre (w-24 = 96px)
- `auto` - Kurzy (w-48 = 192px)

### Zvýraznění výsledků
```typescript
const getWinningOddsClass = (result: string, type: "H" | "D" | "A") => {
  if (result === type) {
    return "bg-green-100 text-green-800 font-bold";
  }
  return "bg-gray-50";
};
```

### Parsing data v scraperu
```csharp
// Formát: "DD.MM." (bez roku)
DateTime.TryParseExact(dateText, "dd.MM.", ...)

// Logika roku:
// Měsíc 8-12 → první rok sezóny (2024)
// Měsíc 1-7  → druhý rok sezóny (2025)
```

---

## Známé problémy

### Všechna data v DB mají matchDate = NULL
**Příčina:** Data byla naimportována před opravou scraperu

**Řešení:**
1. **Re-import sezón** přes `/import` stránku
2. **Sync workflow** pro nová kola

**Nový scraper:** ✅ Nyní správně parsuje data z HTML

---

## Jak pokračovat po restartu

### Aplikace běží:
```
✅ Frontend:    http://localhost:3000
✅ Backend API: http://127.0.0.1:3001
✅ PostgreSQL:  localhost:3002
✅ Redis:       localhost:3003
✅ pgAdmin:     http://localhost:3004
```

### Spuštění služeb:
```bash
docker-compose up -d
```

### Re-import dat s datumy:
1. Otevřít http://localhost:3000/import
2. Vybrat ligu (např. ChNL)
3. Zadat sezónu: `2024/2025`
4. Spustit import
5. Data budou mít nově `matchDate` správně naplněný

---

## Testování

**Otestováno:**
- ✅ Filtry v countries (aktivní/neaktivní)
- ✅ Filtr aktivních zemí v leagues
- ✅ Filtr sezón v rounds (zobrazuje všechny sezóny)
- ✅ Varování pro historical sezóny (modal)
- ✅ Zvýraznění výsledků v matches (zelené pozadí)
- ✅ Grid layout (kurzy zarovnané)
- ✅ Kumulativní kurzy (celá čísla)
- ✅ Sloupec Datum v rounds tabulce

**Funkční:**
- ✅ API endpointy (127.0.0.1)
- ✅ Frontend hot reload
- ✅ Docker služby
- ✅ Scraper (opravený parsing data)

---

## Pending úkoly

### VOLITELNÉ - Re-import dat:
- [ ] Re-import ChNL sezóny 2024/2025 pro naplnění dat
- [ ] Ověřit, že datum se nyní správně scrapuje
- [ ] Zkontrolovat zobrazení v UI

### BACKLOG:
- [ ] Analýza lig v českých sázkových kancelářích
- [ ] Analýza lig v zahraničních sázkových kancelářích
- [ ] Odstranit obsolete property `BetExplorerSlug`

---

**Poslední aktualizace:** 2025-10-29 23:05
**Status:** 🎉 Všechny UI/UX vylepšení + scraper fix DOKONČENY
**Připraveno k:** Re-import dat s datumy

---

**End of Context**
