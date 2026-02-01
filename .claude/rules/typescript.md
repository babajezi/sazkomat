# TypeScript API pravidla

## JsonStringEnumConverter - Všechny enums jsou STRINGY

Backend používá `JsonStringEnumConverter` (Program.cs) - všechny enums jsou serializovány jako **STRINGY**, ne čísla.

## Povinný workflow při přidání/změně enum

1. **Zjisti hodnoty backendu** - zavolej API endpoint, zkontroluj C# enum definici
2. **Ověř serializaci** - spusť `curl` nebo `Invoke-RestMethod` na endpoint
3. **Vytvoř TypeScript enum se STRINGOVÝMI hodnotami** odpovídajícími backendu
4. **Zkontroluj naming** - backend může mít jiný název (např. `LiveUpdate` vs `LiveSync`)
5. **Zkontroluj všechny hodnoty** - backend může mít více hodnot než očekáváš

## Příklad

```typescript
// SPRÁVNĚ - stringové hodnoty
export enum SyncJobStatus {
  Pending = "Pending",
  Running = "Running",
  Completed = "Completed",
  Failed = "Failed"
}

// ŠPATNĚ - numerické hodnoty
export enum SyncJobStatus {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Failed = 3
}
```

## Důležité upozornění

- **NIKDY NEPŘEDPOKLÁDEJ** numerické hodnoty nebo názvy
- **VŽDY OVĚŘ NA REÁLNÝCH DATECH**
- Chybné enum types způsobují tiché selhání ve filtrovacích komponentách a business logice
