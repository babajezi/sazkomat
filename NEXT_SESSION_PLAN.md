# Next Session Plan - Font Awesome Flags + BetExplorer Sync

## 🎯 Cíl
Implementovat Font Awesome Pro ikony zemí a povolit BetExplorer vytvářet nové země při synchronizaci.

## 📋 Rozpracované TODO

### ✅ Hotovo
1. `.npmrc` vytvořen s Font Awesome Pro tokenem: `28F74F03-455E-428C-B94A-641D4F840D0C`

### 🔄 Rozpracováno
2. `Country.cs` - **ČÁSTEČNĚ** (změna vrácena zpět, potřeba znovu přidat)

### ⏳ Zbývá Dokončit

#### Backend (5 úkolů):
3. **Přidat `IsoCode` property do `Country.cs`**
   ```csharp
   public string IsoCode { get; set; } = string.Empty; // ISO Alpha-2 (lowercase: "gb", "de", "cz")
   ```

4. **Vytvořit EF Migration**
   ```bash
   dotnet ef migrations add AddCountryIsoCode --project src/Sazkomat.Configuration --startup-project src/Sazkomat.Api
   ```

5. **Update seed data** v `ConfigurationSeeder.cs`
   - Přidat `IsoCode` pro všechny země v seedu
   - Příklady: England → "gb", Spain → "es", Germany → "de"

6. **Upravit `BetExplorerCountryScraper.cs`** (řádky ~83-91)
   - Z HTML najít: `<img src="https://cci.betexplorer.com/XX.svg">`
   - Parsovat "XX" (např. "de", "gb", "fr")
   - Uložit do `CountryInfo.IsoCode` (nová property)
   - Pattern: `src="https://cci.betexplorer.com/([a-z0-9]+)\.svg"`

7. **Upravit `ProviderSyncService.SyncCountriesAsync()`** (řádky ~126-137)
   - **PŘED:**
     ```csharp
     if (existingCountry == null) {
         _logger.LogWarning("Country not found - skipping");
         stats.Skipped++;
         continue;
     }
     ```
   - **PO:**
     ```csharp
     if (existingCountry == null) {
         existingCountry = new Country {
             Code = normalizedCode,
             Name = countryInfo.Name,
             FlagEmoji = countryInfo.FlagEmoji ?? "🏳️",
             IsoCode = countryInfo.IsoCode ?? "",
             IsActive = activateCountries
         };
         await _countryRepository.AddAsync(existingCountry);
         stats.Created++;
         _logger.LogInformation("Created new country: {Name} ({Code})", existingCountry.Name, existingCountry.Code);
     }
     ```

#### Frontend (4 úkoly):
8. **Instalovat Font Awesome Pro** (MŮŽE SELHAT - token issue)
   ```bash
   cd frontend
   npm install @fortawesome/fontawesome-svg-core @fortawesome/pro-solid-svg-icons @fortawesome/react-fontawesome
   ```
   - Pokud selže E401 → použít **BetExplorer SVG** místo FA

9. **Přidat `isoCode` do `Country` interface** (`frontend/lib/api/types.ts`)
   ```typescript
   export interface Country {
     id: string;
     name: string;
     code: string;
     flagEmoji: string; // deprecated
     isoCode: string;   // NEW - ISO Alpha-2 lowercase
     isActive: boolean;
     createdAt: string;
     updatedAt: string;
     countryProviders?: CountryProvider[];
   }
   ```

10. **Vytvořit `CountryFlag.tsx` komponentu**

    **Varianta A - Font Awesome (pokud instalace uspěje):**
    ```tsx
    import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
    import * as flags from '@fortawesome/pro-solid-svg-icons';

    interface CountryFlagProps {
      isoCode: string;
      className?: string;
    }

    export function CountryFlag({ isoCode, className }: CountryFlagProps) {
      const iconName = `faFlag${isoCode.charAt(0).toUpperCase()}${isoCode.charAt(1).toLowerCase()}`;
      const icon = (flags as any)[iconName];

      if (!icon) {
        return <span className={className}>🏳️</span>; // fallback
      }

      return <FontAwesomeIcon icon={icon} className={className} />;
    }
    ```

    **Varianta B - BetExplorer SVG (fallback):**
    ```tsx
    interface CountryFlagProps {
      isoCode: string;
      alt: string;
      className?: string;
    }

    export function CountryFlag({ isoCode, alt, className = "w-6 h-4" }: CountryFlagProps) {
      return (
        <img
          src={`https://cci.betexplorer.com/${isoCode}.svg`}
          alt={alt}
          className={className}
          onError={(e) => {
            // Fallback pokud SVG neexistuje
            e.currentTarget.src = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg'%3E%3C/svg%3E";
          }}
        />
      );
    }
    ```

11. **Nahradit `{country.flagEmoji}` za `<CountryFlag>` v:**
    - `frontend/app/countries/page.tsx` (řádek ~383)
    - `frontend/app/leagues/page.tsx` (řádky ~210, ~334)
    - `frontend/app/sync/page.tsx` (řádky ~354, ~450)
    - `frontend/app/import/page.tsx` (řádek ~169)
    - `frontend/app/rounds/page.tsx` (řádek ~191)
    - `frontend/components/LeagueFormDialog.tsx` (řádek ~80)
    - `frontend/components/CountryFormDialog.tsx` (řádky ~34, ~43, ~107-120)

---

## ⚠️ DŮLEŽITÉ ROZHODNUTÍ

**Font Awesome token může opět selhat (E401).** Pokud ano:

### Plán B: BetExplorer SVG (bez Font Awesome)
- Přeskočit instalaci Font Awesome
- Použít Varianta B pro CountryFlag komponentu
- Výhody: Zero dependencies, konzistentní s BetExplorer
- Nevýhody: Externí CDN dependency

---

## 🧪 Testing Checklist

Po dokončení otestovat:
1. ✅ BetExplorer country sync vytvoří nové země s `iso_code`
2. ✅ Flag ikony se zobrazí správně na všech stránkách
3. ✅ Fallback funguje pro chybějící ISO kódy
4. ✅ Existing země mají `iso_code` po migraci

---

## 📁 Soubory k Úpravě (celkem 13)

**Backend (5):**
1. `src/Sazkomat.Configuration/Entities/Country.cs`
2. `src/Sazkomat.Configuration/Migrations/XXXXXX_AddCountryIsoCode.cs` (nový)
3. `src/Sazkomat.Configuration/Data/ConfigurationSeeder.cs`
4. `src/Sazkomat.DataImport/Scrapers/BetExplorerCountryScraper.cs`
5. `src/Sazkomat.DataImport/Services/ProviderSyncService.cs`

**Frontend (8):**
6. `frontend/lib/api/types.ts`
7. `frontend/components/CountryFlag.tsx` (nový)
8. `frontend/app/countries/page.tsx`
9. `frontend/app/leagues/page.tsx`
10. `frontend/app/sync/page.tsx`
11. `frontend/app/import/page.tsx`
12. `frontend/app/rounds/page.tsx`
13. `frontend/components/LeagueFormDialog.tsx`
14. `frontend/components/CountryFormDialog.tsx`

---

**Připraveno k pokračování v nové session!**
