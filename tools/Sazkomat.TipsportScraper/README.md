# Tipsport Standalone Scraper

Standalone konzolová aplikace pro scraping dat z Tipsport.cz. Běží mimo Docker, aby obešla Cloudflare ochranu.

## Proč mimo Docker?

Tipsport.cz používá agresivní Cloudflare ochranu, která blokuje headless browsery v Docker kontejnerech. Tato standalone aplikace:

1. Běží s **viditelným prohlížečem** (ne headless)
2. Umožňuje manuální řešení CAPTCHA/Cloudflare výzev
3. Po úspěšném načtení stránky zachytí API data
4. Odešle data do Sazkomat API

## Požadavky

- .NET 10 SDK
- Playwright browsery (Chromium)
- Běžící Sazkomat API (docker-compose up -d)

## Instalace

### 1. Nainstalovat Playwright browsery

**Windows (PowerShell):**
```powershell
cd tools/Sazkomat.TipsportScraper
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

**Linux/macOS:**
```bash
cd tools/Sazkomat.TipsportScraper
dotnet build
npx playwright install chromium
```

### 2. Nainstalovat systémové závislosti (pouze Linux)
```bash
npx playwright install-deps chromium
```

## Použití

### Spuštění s viditelným prohlížečem (doporučeno)
```bash
dotnet run
```

Otevře se okno prohlížeče. Pokud se zobrazí Cloudflare CAPTCHA, vyřešte ji manuálně. Po načtení stránky scraper automaticky zachytí data a odešle je do API.

### Spuštění v headless režimu (může selhat kvůli Cloudflare)
```bash
dotnet run -- --headless
```

### Vlastní API URL
```bash
dotnet run -- --api-url http://192.168.1.100:3001
```

## Jak to funguje

1. **Spuštění prohlížeče** - Playwright otevře Chromium
2. **Navigace** - Přejde na https://www.tipsport.cz/kurzy/fotbal
3. **Čekání** - Čeká na načtení stránky (a případné Cloudflare ověření)
4. **Zachycení dat** - Monitoruje síťový provoz pro API odpovědi
5. **Parsování** - Extrahuje fotbalové soutěže z JSON odpovědi
6. **Odeslání** - POST na /api/tipsport/leagues

## Výstup

Po úspěšném spuštění uvidíte:
```
===========================================
  Sazkomat Tipsport Scraper (standalone)
===========================================
Mode: Visible browser
API URL: http://localhost:3001

[1/3] Launching browser and navigating to Tipsport...
      Navigating to Tipsport...
      Captured API response: 123,456 bytes
      Page loaded successfully!

[2/3] Parsing competitions from JSON...
      Found 150 football competitions

      Sample competitions:
        - 1. anglická liga (Country: england)
        - 1. německá liga (Country: germany)
        ...

[3/3] Pushing 150 leagues to API...
      API Response: {"message":"Tipsport leagues received successfully"...}

SUCCESS: Data pushed to API successfully!
```

## API Endpoint

Scraper odesílá data na:
```
POST /api/tipsport/leagues
Content-Type: application/json

{
  "providerId": "b0000000-0000-0000-0000-000000000004",
  "leagues": [
    {
      "providerLeagueId": "123456",
      "providerLeagueName": "1. anglická liga",
      "countryCode": "england",
      "url": "/kurzy/fotbal/anglie/1-anglicka-liga",
      "matchCount": 10
    }
  ]
}
```

## Troubleshooting

### Cloudflare blokuje přístup
- Spusťte bez `--headless` a vyřešte CAPTCHA manuálně
- Ujistěte se, že máte aktuální Chromium: `npx playwright install chromium`

### API není dostupné
- Ověřte, že běží: `curl http://localhost:3001/health`
- Zkontrolujte Docker: `docker-compose ps`

### Žádná data
- Stránka se možná změnila - zkontrolujte debug-tipsport.html
- API endpoint mohl být přejmenován

## Poznámky

- Tipsport je největší česká sázková kancelář
- Data jsou ukládána do `ProviderLeagues` tabulky
- CountryCode je odvozeno z českých názvů lig (např. "anglická" → "england")
