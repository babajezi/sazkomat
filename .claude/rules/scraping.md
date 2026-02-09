# Pravidla pro BetExplorer scraping

## Max 20 zápasů na kolo

- Většina lig má max 18 zápasů na kolo (36 týmů, např. Mexico Liga Premier Serie A)
- Pokud scraper najde kolo s více než 20 zápasy = **chyba parsování**
- Typická příčina: nesprávné sloučení více kol dohromady
- **Při detekci se vyhodí výjimka** - sync selže a musí se opravit parser

### Proč je toto důležité
- Kumulativní kurzy = součin kurzů všech zápasů
- Např. 40 zápasů s kurzem 2.0 = 2^40 = 1 bilion → PostgreSQL NUMERIC overflow
- Chceme **detekovat** problém v parsingu, ne maskovat chybu

## BetExplorer URL - NIKDY query parametr pro sezónu

- **NIKDY** neotevírej URL typu `https://www.betexplorer.com/.../results/?season=1996-1997`
- Query parametr `?season=` **NEFUNGUJE** - stránka vždy zobrazí aktuální sezónu!
- **Sezóna se nastavuje pouze přes JavaScript** při výběru z dropdown selectu
- Recipes používají Playwright a JavaScript pro navigaci na konkrétní sezónu
- Při debugování NELZE použít WebFetch pro ověření obsahu konkrétní sezóny

## Podpora skupin v ligách (Groups)

Některé ligy mají sezónu rozdělenou na skupiny, např. Indonesia Championship:
- **2019**: "East - 1. Round", "West - 22. Round"
- **2024-2025**: "GROUP 1 - 1.ROUND", "GROUP 2 - 1.ROUND"

### Pravidla parsování
1. **Podporujeme pouze hlavičky obsahující "ROUND"**
2. **Ignorujeme**: "GROUP A", "GROUP X" (bez ROUND) → sezóna bez kol
3. **Parsujeme**: "{Skupina} - {Číslo}. Round" → extrahujeme skupinu + číslo kola

### Příklady parsování

| Input | GroupName | RoundNumber |
|-------|-----------|-------------|
| "38. Round" | `null` | 38 |
| "East - 1. Round" | "East" | 1 |
| "GROUP 1 - 15.ROUND" | "GROUP 1" | 15 |
| "West - 22. Round" | "West" | 22 |

### Klíčové soubory
- `src/Sazkomat.DataImport/Entities/Round.cs` - GroupName property
- `src/Sazkomat.DataImport/Scrapers/FootballBetExplorerScraper.cs` - ParseRoundHeader()
