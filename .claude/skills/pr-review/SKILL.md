---
name: pr-review
description: Code review pull requestu s kontextem Sazkomat projektu - kontroluje architektonická pravidla, TypeScript typy, databázové operace. Použij před mergem PR.
allowed-tools: Bash, Read, Grep, Glob
argument-hint: "[PR číslo nebo branch]"
---

# Code Review pro Sazkomat

Proveď code review pro: $ARGUMENTS

## Postup

### 1. Získej změny

```bash
# Pokud je argument číslo PR
gh pr diff $ARGUMENTS

# Pokud je argument branch
git diff main...$ARGUMENTS
```

### 2. Zkontroluj podle pravidel projektu

#### Architektura (viz .claude/rules/architecture.md)
- [ ] BetExplorer je jediný zdroj pravdy pro data
- [ ] Betting providers pouze vytváří mapování (LeagueProvider, CountryProvider)
- [ ] Žádné nové ligy/země z betting providerů

#### Databáze (viz .claude/rules/database.md)
- [ ] DELETE/UPDATE operace jsou okomentovány nebo schváleny
- [ ] Správné použití snake_case pro sloupce
- [ ] JSONB pro komplexní data
- [ ] Migrace jsou přítomny pro DB změny

#### TypeScript (viz .claude/rules/typescript.md)
- [ ] Enum hodnoty jsou STRINGY (ne čísla)
- [ ] Typy odpovídají backendu
- [ ] Nové typy přidány do types.ts

#### Scraping (viz .claude/rules/scraping.md)
- [ ] Max 15 zápasů na kolo validace
- [ ] Žádné ?season= query parametry v URL
- [ ] Podpora skupin (GroupName) pokud relevantní

#### Workflow (viz .claude/rules/workflow.md)
- [ ] Správné použití 4-step workflow
- [ ] SyncMode (Historical/Current) správně nastaveno

### 3. Kontrola kódu

#### Backend (.cs)
- [ ] Repository pattern dodržen
- [ ] Result pattern pro error handling
- [ ] Async/await správně použito
- [ ] Logování přes Serilog

#### Frontend (.tsx, .ts)
- [ ] React Query pro data fetching
- [ ] Správné typy z types.ts
- [ ] Žádné hardcoded URL/porty

### 4. Testy
```bash
# Zkontroluj jestli jsou testy
ls tests/Sazkomat.Tests/**/*$FEATURE*Tests.cs 2>/dev/null

# Spusť relevantní testy
dotnet test --filter "FullyQualifiedName~$FEATURE"
```

### 5. Dokumentace
- [ ] docs/API.md aktualizováno pro nové endpointy
- [ ] docs/DATABASE_SCHEMA.md aktualizováno pro DB změny
- [ ] CLAUDE.md nepotřebuje update (je minimální)

## Výstup

Vytvoř review s:
1. **Summary** - Co PR dělá
2. **Positives** - Co je dobře
3. **Issues** - Problémy k opravě (blocker/suggestion)
4. **Questions** - Nejasnosti k diskuzi
5. **Verdict** - Approve / Request Changes / Comment
