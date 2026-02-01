# Kritická architektura - Zdroje dat

**TOTO JE NEJDŮLEŽITĚJŠÍ PRAVIDLO CELÉHO PROJEKTU**

## BetExplorer = Jediný zdroj pravdy

**BetExplorer.com** je **JEDINÝ** zdroj pro:
- Země (countries)
- Ligy (leagues)
- Sezóny (seasons)
- Kola a zápasy (rounds, matches)
- Výsledky a kurzy

## Betting Providers = Pouze mapování

Betting providers (Betano, Fortuna, Tipsport, Chance, Kingsbet):
- **NEVYTVÁŘÍ** nová data o ligách/zemích
- Pouze zjišťujeme **které existující ligy podporují**
- Vytváříme pouze **vazební záznamy**:
  - `LeagueProvider` - vazba liga ↔ betting provider
  - `CountryProvider` - vazba země ↔ betting provider

## Typy providerů (DataProviderType)

| Typ | Hodnota | Příklady | Účel |
|-----|---------|----------|------|
| Reference | 1 | BetExplorer, Oddsportal | Zdroj pravdy - vytváří data |
| Betting | 4 | Betano, Fortuna, Tipsport, Chance, Kingsbet | Pouze mapování |

## Praktické důsledky

1. **Scan zemí/lig z betting providera** = hledání shody s existujícími BetExplorer daty
2. **NIKDY** nevytvářet nové ligy/země z betting providera
3. **ProviderLeagues** pro betting providery = dočasná cache pro mapování, ne nová data
4. Pokud liga z betting providera nemá shodu v BetExploreru = nelze importovat

**NIKDY TOTO PRAVIDLO NEPORUŠUJ PŘI IMPLEMENTACI NOVÝCH PROVIDERŮ!**
