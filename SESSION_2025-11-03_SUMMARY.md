# Session Summary - 2025-11-03

## ✅ KOMPLETNĚ DOKONČENO

### 1. League Provider Mappings CRUD
**Soubory vytvořené:**
- `frontend/components/LeagueProviderDialog.tsx` - Dialog pro CRUD operace

**Soubory upravené:**
- `frontend/lib/api/types.ts` - Přidány typy:
  - `CreateLeagueProviderRequest`
  - `UpdateLeagueProviderRequest`
- `frontend/lib/api/client.ts` - Přidány API metody:
  - `createLeagueProvider()`
  - `updateLeagueProvider()`
  - `deleteLeagueProvider()`
- `frontend/app/leagues/page.tsx` - Přidáno:
  - State: `providerDialogOpen`, `editingProviderMapping`
  - Mutation: `deleteLeagueProviderMappingMutation`
  - Handlers: `handleAddLeagueProvider()`, `handleEditLeagueProviderMapping()`, `handleDeleteLeagueProviderMapping()`
  - Tlačítka: "Upravit", "Smazat", "+ Přidat Provider"
  - LeagueProviderDialog komponenta

**Funkcionalita:**
- ✅ Přidat nový League Provider mapping
- ✅ Upravit existující mapping
- ✅ Smazat mapping s potvrzením
- ✅ React Query invalidation

---

### 2. Provider Filtry a Groupování

**Soubory upravené:**
- `frontend/app/leagues/page.tsx`:
  - Přidán state: `filterProviderId`
  - Přidán filtr podle konkrétního providera
  - Groupování providerů podle typu (Scraper, API, Manual, Betting Provider)
  - Podmíněné renderování - zobrazí se jen neprázdné kategorie
  - Grid změněn z 6 na 7 sloupců
  - Změněno: `getBettingProviders()` → `getProviders()` (aby BetExplorer byl viditelný)

- `frontend/app/countries/page.tsx`:
  - Přidán groupování do filtru providerů
  - Změněno: `getBettingProviders()` → `getProviders()`

- `frontend/components/LeagueProviderDialog.tsx`:
  - Groupování providerů v selectu podle typu
  - Podmíněné renderování neprázdných kategorií

- `frontend/components/CountryProviderDialog.tsx`:
  - Groupování providerů v selectu podle typu
  - Podmíněné renderování neprázdných kategorií

**Funkcionalita:**
- ✅ Filtrování lig podle konkrétního providera
- ✅ Filtrování zemí podle konkrétního providera
- ✅ Groupování podle typu: Scraper, API, Manual, Betting Provider
- ✅ Prázdné kategorie se nezobrazují
- ✅ **BetExplorer (Scraper) je viditelný ve všech filtrech**

---

### 3. Pagination ve Všech Admin Stránkách

**Soubory vytvořené:**
- `frontend/components/PaginationControls.tsx` - Reusable pagination komponenta

**Soubory upravené:**
- `frontend/app/sports/page.tsx`:
  - State: `page`, `pageSize`
  - Client-side slice: `paginatedSports`
  - PaginationControls komponenta

- `frontend/app/countries/page.tsx`:
  - State: `page`, `pageSize`
  - Client-side slice: `paginatedCountries`
  - PaginationControls komponenta
  - Reset page při "Zrušit filtry"

- `frontend/app/leagues/page.tsx`:
  - State: `page`, `pageSize`
  - Client-side slice: `paginatedLeagues`
  - PaginationControls komponenta
  - Reset page při "Zrušit filtry"

- `frontend/app/providers/page.tsx`:
  - State: `page`, `pageSize`
  - Client-side slice: `paginatedProviders`
  - PaginationControls komponenta

**Funkcionalita:**
- ✅ Page size selector (10, 20, 50, 100)
- ✅ Počítadlo: "Stránka X z Y"
- ✅ Počítadlo: "Zobrazeno A-B z C záznamů"
- ✅ Info o celkovém počtu (když jsou aktivní filtry)
- ✅ Tlačítka Předchozí/Další s disable stavem
- ✅ Client-side pagination (bez backend změn)

---

### 4. Opravy a Vylepšení

**Soubory upravené:**
- `frontend/components/ResetDatabaseDialog.tsx`:
  - Opraven hydration error: `<DialogDescription>` už neobsahuje `<div>`, jen text
  - Změněno: `<div className="text-left">{description}</div>` → className přímo na DialogDescription

**Port Management:**
- ✅ Pravidlo: VŽDY zabít proces na obsazeném portu místo změny portu
- ✅ Frontend běží na správném portu 3000

---

## 🔄 ROZPRACOVÁNO (nedokončeno)

### Font Awesome Pro / BetExplorer SVG Flags

**Soubory částečně změněné:**
- `src/Sazkomat.Configuration/Entities/Country.cs`:
  - ✅ Přidána property: `public string IsoCode { get; set; } = string.Empty;`
  - ⚠️ Potřeba: EF migration

- `frontend/.npmrc`:
  - ✅ Vytvořen s Font Awesome tokenem
  - ❌ Token nefunguje (E401 error)

**Co zbývá dokončit v další session:**
1. Vytvořit EF migration pro `iso_code` sloupec
2. Upravit `BetExplorerCountryScraper.cs` - extrahovat ISO kód z `<img src="https://cci.betexplorer.com/XX.svg">`
3. Upravit `ProviderSyncService.SyncCountriesAsync()` - povolit vytváření nových zemí (změnit skip → create)
4. Update seed data s `iso_code` hodnotami
5. Přidat `isoCode` do Country TypeScript interface (`frontend/lib/api/types.ts`)
6. Vytvořit `CountryFlag.tsx` komponentu (BetExplorer SVG nebo jiné řešení)
7. Nahradit `{country.flagEmoji}` za `<CountryFlag>` v:
   - countries/page.tsx
   - leagues/page.tsx
   - sync/page.tsx
   - CountryFormDialog.tsx
   - LeagueFormDialog.tsx
   - import/page.tsx
   - rounds/page.tsx

---

## 📝 POZNÁMKY PRO DALŠÍ SESSION

### BetExplorer jako Primární Zdroj Dat
- ✅ BetExplorer sync MUSÍ moci vytvářet Country a League entity
- ✅ Betano provider NESMÍ vytvářet základní entity (pouze mappings)
- ✅ Současný stav: `BettingProviderOrchestrator.cs` je správně - POUZE matchuje, nevytváří
- ❌ Současný stav: `ProviderSyncService.cs` - Country sync přeskakuje neexistující země

### Flag Ikony - Rozhodnutí
**Možnosti:**
1. **BetExplorer SVG CDN** - `https://cci.betexplorer.com/{code}.svg` (DOPORUČENO)
   - Zero dependencies
   - Konzistentní s BetExplorer
   - Parsovat ISO kód přímo z jejich HTML

2. **Font Awesome Pro** - nefunkční tokeny
   - Token 1: `FAPS-KENW-JQPA-MGKR-9009` - E401 error
   - Token 2: `28F74F03-455E-428C-B94A-641D4F840D0C` - nepotestován

3. **country-flag-icons (npm)** - free alternativa
   - React komponenty
   - Tree-shakeable
   - MIT licence

### Neplatná Data k Vyčištění
- Liga: "Esoccer eAdriatic League - Liga mistrů - Zápas 2x5 minuty (Esoccer)"
  - Zjistit jak byla vytvořena
  - Pravděpodobně ručně přes frontend nebo je validní z BetExplorer
  - Smazat pokud není validní

---

## 🔧 TECHNICKÉ DETAILY

### API Endpoints Vytvořené/Upravené
- ✅ `POST /api/config/providers/league-mappings` - create
- ✅ `PATCH /api/config/providers/league-mappings/{id}` - update
- ✅ `DELETE /api/config/providers/league-mappings/{id}` - delete
- ✅ `GET /api/config/providers` - vrací všechny providery (ne jen betting)

### Komponenty Vytvořené
- `LeagueProviderDialog.tsx` - 228 řádků
- `PaginationControls.tsx` - 98 řádků

### Soubory s Významými Změnami
- `leagues/page.tsx` - +100 řádků (provider dialog, filtry, pagination)
- `countries/page.tsx` - +50 řádků (filtry, pagination)
- `sports/page.tsx` - +35 řádků (pagination)
- `providers/page.tsx` - +35 řádků (pagination)
- `types.ts` - +15 řádků (League Provider types)
- `client.ts` - +30 řádků (API metody)

### Provider Types (reminder)
```
1 = Scraper (BetExplorer, Oddsportal)
2 = API (připraveno pro budoucí použití)
3 = Manual (připraveno pro edge cases)
4 = BettingProvider (Betano, Chance, Fortuna, Tipsport, Kingsbet)
```

---

## 🎯 PRIORITA PRO DALŠÍ SESSION

### HIGH Priority
1. **BetExplorer Sync - Vytváření Zemí**
   - Upravit `ProviderSyncService.SyncCountriesAsync()`
   - Změnit skip → create new Country
   - Testovat na /sync stránce

2. **Flag Ikony**
   - Rozhodnout: BetExplorer SVG vs country-flag-icons
   - Vytvořit migration pro `iso_code`
   - Implementovat CountryFlag komponentu
   - Nahradit napříč frontendem

### MEDIUM Priority
3. **Data Cleanup**
   - Najít a smazat neplatnou "Esoccer" ligu
   - Ověřit že všechny ligy mají validní BetExplorer slug

### LOW Priority
4. **Code Cleanup**
   - Smazat deprecated `.npmrc` pokud Font Awesome nebude použit
   - Smazat `FlagEmoji` property po migraci na `IsoCode`

---

## 🚀 FRONTEND BĚŽÍ

- Port: **3000** (správný!)
- API: **http://localhost:3001** (jeden z běžících API procesů)
- Bez chyb kompilace
- Hydration warning opravena

## 📦 BACKGROUND PROCESSES

Mnoho duplicitních procesů běží - před další session doporučuji:
```bash
# Zabít všechny staré procesy
# Spustit čistě jen:
cd src/Sazkomat.Api && dotnet run
cd frontend && npm run dev
```

---

**Připraveno pro commit a novou session!**
