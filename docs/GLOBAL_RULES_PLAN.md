# Globální mapovací pravidla pro Unmatched Leagues

## Přehled
Přidání funkce pro vytvoření "globálního pravidla" (`ProviderCode = "*"`), které se automaticky aplikuje na všechny betting providery při matchování názvů lig.

**Workflow:**
1. Uživatel má namapovanou ligu (např. "1. Anglická liga" → Premier League)
2. Klikne na tlačítko Globe u namapované ligy
3. Zobrazí se dialog s náhledem: jaké pravidlo se vytvoří + které unmatched ligy budou vyřešeny
4. Po potvrzení se vytvoří globální pravidlo a vyřeší dotčené ligy

## Backend změny

### 1. Helper pro normalizaci názvů
**Nový soubor:** `src/Sazkomat.DataImport/Helpers/LeagueNameNormalizer.cs`
```csharp
public static class LeagueNameNormalizer
{
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return Regex.Replace(name.Trim(), @"\s+", " ").ToLowerInvariant();
    }

    public static bool AreEquivalent(string a, string b)
        => Normalize(a) == Normalize(b);
}
```
Řeší: "1. Anglická liga" == "1.  Anglická liga" == "1. anglická liga"

### 2. Úprava entity LeagueNameMapping
**Soubor:** `src/Sazkomat.DataImport/Entities/LeagueNameMapping.cs`
- Přidat `NormalizedProviderLeagueName` (string) - auto-computed při uložení
- Přidat konstantu `public const string GlobalProviderCode = "*";`
- Přidat computed property `public bool IsGlobal => ProviderCode == "*";`

### 3. Database migrace
**Nový soubor:** `src/Sazkomat.DataImport/Migrations/[timestamp]_AddNormalizedLeagueNameColumn.cs`
- Přidat sloupec `normalized_provider_league_name` (varchar 200)
- Přidat index `ix_league_name_mappings_normalized_lookup` na (country_code, normalized_provider_league_name, is_active)
- Update existujících záznamů

### 4. Úprava EF konfigurace
**Soubor:** `src/Sazkomat.DataImport/Data/Configurations/LeagueNameMappingConfiguration.cs`
- Přidat mapování pro `NormalizedProviderLeagueName`
- Přidat index

### 5. Rozšíření repository rozhraní
**Soubor:** `src/Sazkomat.DataImport/Repositories/ILeagueNameMappingRepository.cs`
```csharp
Task<LeagueNameMapping?> FindMappingWithFallbackAsync(
    string providerCode, string countryCode, string providerLeagueName);
```

### 6. Implementace repository
**Soubor:** `src/Sazkomat.DataImport/Repositories/LeagueNameMappingRepository.cs`
```csharp
public async Task<LeagueNameMapping?> FindMappingWithFallbackAsync(...)
{
    var normalized = LeagueNameNormalizer.Normalize(providerLeagueName);

    // 1. Provider-specific pravidlo (priorita)
    var specific = await _context.LeagueNameMappings
        .Where(m => m.ProviderCode == providerCode
                 && m.CountryCode == countryCode
                 && m.NormalizedProviderLeagueName == normalized
                 && m.IsActive)
        .OrderBy(m => m.Priority)
        .FirstOrDefaultAsync();

    if (specific != null) return specific;

    // 2. Fallback na globální pravidlo (*)
    return await _context.LeagueNameMappings
        .Where(m => m.ProviderCode == "*"
                 && m.CountryCode == countryCode
                 && m.NormalizedProviderLeagueName == normalized
                 && m.IsActive)
        .OrderBy(m => m.Priority)
        .FirstOrDefaultAsync();
}
```

### 7. Úprava BetExplorerEnrichmentService
**Soubor:** `src/Sazkomat.DataImport/Services/BetExplorerEnrichmentService.cs`
- Změnit 3x volání `FindMappingAsync` → `FindMappingWithFallbackAsync`
- Řádky cca 69, 349, 495

### 8. Nový GlobalRuleService
**Nový soubor:** `src/Sazkomat.DataImport/Services/GlobalRuleService.cs`

**Interface:**
```csharp
public interface IGlobalRuleService
{
    Task<GlobalRulePreview> GetGlobalRulePreviewAsync(Guid sourceUnmatchedLeagueId);
    Task<GlobalRuleResult> CreateGlobalRuleAsync(CreateGlobalRuleRequest request);
}
```

**Logika GetGlobalRulePreviewAsync:**
1. Načíst zdrojovou UnmatchedLeague
2. Ověřit že je `IsResolved && ResolutionType == Mapped`
3. Normalizovat název
4. Najít všechny UnmatchedLeague se stejným normalized name + country
5. Vrátit preview s affected leagues

**Logika CreateGlobalRuleAsync:**
1. Vytvořit LeagueNameMapping s `ProviderCode = "*"`
2. Pro každou affected unmatched league: ResolveAsMapped
3. Vrátit výsledek

### 9. Nové API endpointy
**Soubor:** `src/Sazkomat.Api/Endpoints/UnmatchedLeagueEndpoints.cs`

```
GET  /api/unmatched-leagues/{id}/global-rule/preview
POST /api/unmatched-leagues/{id}/global-rule/create
```

### 10. DI registrace
**Soubor:** `src/Sazkomat.Api/Program.cs`
```csharp
builder.Services.AddScoped<IGlobalRuleService, GlobalRuleService>();
```

## Frontend změny

### 1. TypeScript typy
**Soubor:** `frontend/lib/api/types.ts`
```typescript
export interface GlobalRulePreview {
  normalizedLeagueName: string;
  countryCode: string;
  betExplorerSlug: string;
  sourceLeagueId: string | null;
  sourceLeagueName: string;
  affectedLeagues: AffectedUnmatchedLeague[];
  canCreateGlobalRule: boolean;
  validationMessage?: string;
}

export interface AffectedUnmatchedLeague {
  id: string;
  providerName: string;
  providerLeagueName: string;
  isResolved: boolean;
  resolutionType?: string;
}

export interface GlobalRuleResult {
  globalRuleId: string;
  resolvedCount: number;
}
```

### 2. API client metody
**Soubor:** `frontend/lib/api/client.ts`
```typescript
// Do unmatchedLeagueApi:
getGlobalRulePreview: (id: string) => Promise<GlobalRulePreview>
createGlobalRule: (id: string, request?: { notes?: string }) => Promise<GlobalRuleResult>
```

### 3. GlobalRuleDialog komponenta
**Nový soubor:** `frontend/components/GlobalRuleDialog.tsx`
- Dialog s náhledem pravidla
- Tabulka affected leagues
- Checkbox "Automaticky vyřešit X nevyřešených lig"
- Tlačítko "Vytvořit pravidlo"

### 4. Úprava stránky unmatched-leagues
**Soubor:** `frontend/app/unmatched-leagues/page.tsx`
- Přidat import `Globe` ikony a `GlobalRuleDialog`
- Přidat state pro dialog
- U namapovaných lig přidat tlačítko Globe
- Přidat `<GlobalRuleDialog />` na konec

## Kritické soubory (v pořadí implementace)

1. `src/Sazkomat.DataImport/Helpers/LeagueNameNormalizer.cs` (NEW)
2. `src/Sazkomat.DataImport/Entities/LeagueNameMapping.cs` (MODIFY)
3. `src/Sazkomat.DataImport/Data/Configurations/LeagueNameMappingConfiguration.cs` (MODIFY)
4. `src/Sazkomat.DataImport/Migrations/[timestamp]_AddNormalizedLeagueNameColumn.cs` (NEW)
5. `src/Sazkomat.DataImport/Repositories/ILeagueNameMappingRepository.cs` (MODIFY)
6. `src/Sazkomat.DataImport/Repositories/LeagueNameMappingRepository.cs` (MODIFY)
7. `src/Sazkomat.DataImport/Services/BetExplorerEnrichmentService.cs` (MODIFY)
8. `src/Sazkomat.DataImport/Services/GlobalRuleService.cs` (NEW)
9. `src/Sazkomat.Api/Endpoints/UnmatchedLeagueEndpoints.cs` (MODIFY)
10. `src/Sazkomat.Api/Program.cs` (MODIFY)
11. `frontend/lib/api/types.ts` (MODIFY)
12. `frontend/lib/api/client.ts` (MODIFY)
13. `frontend/components/GlobalRuleDialog.tsx` (NEW)
14. `frontend/app/unmatched-leagues/page.tsx` (MODIFY)

## Testování

### Unit testy
- `LeagueNameNormalizerTests` - normalizace názvů
- `LeagueNameMappingRepositoryTests` - fallback logika
- `GlobalRuleServiceTests` - preview a vytvoření pravidla

### Manuální test
1. Mít namapovanou ligu v unmatched-leagues
2. Mít další ligy se stejným názvem (jiné providery)
3. Kliknout Globe → ověřit preview
4. Potvrdit → ověřit že se ligy vyřešily
5. Spustit nový scan → ověřit že se automaticky matchují
