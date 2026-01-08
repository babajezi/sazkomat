# Nastavení Google OAuth pro Sazkomat

Tento návod popisuje jak získat Google OAuth Client ID a Client Secret pro přihlašování přes Google.

## 1. Vytvoření projektu v Google Cloud Console

1. Přejděte na [Google Cloud Console](https://console.cloud.google.com/)
2. Klikněte na **Select a project** (vlevo nahoře) → **New Project**
3. Zadejte název projektu (např. "Sazkomat")
4. Klikněte **Create**

## 2. Povolení Google+ API (volitelné, ale doporučené)

1. V levém menu vyberte **APIs & Services** → **Library**
2. Vyhledejte "Google+ API" nebo "Google People API"
3. Klikněte na něj a pak **Enable**

## 3. Konfigurace OAuth Consent Screen

1. V levém menu vyberte **APIs & Services** → **OAuth consent screen**
2. Vyberte **User Type**:
   - **Internal** - pouze pro uživatele vaší Google Workspace organizace
   - **External** - pro všechny uživatele s Google účtem
3. Klikněte **Create**

### Vyplnění formuláře:

**OAuth consent screen:**
- **App name**: Sazkomat
- **User support email**: váš email
- **App logo**: (volitelné)
- **App domain**: (volitelné pro development)
- **Developer contact information**: váš email

Klikněte **Save and Continue**

**Scopes:**
- Klikněte **Add or Remove Scopes**
- Vyberte:
  - `email` - zobrazení emailové adresy
  - `profile` - zobrazení základních informací o profilu
  - `openid` - OpenID Connect
- Klikněte **Update** a pak **Save and Continue**

**Test users:** (pouze pro External typ v testovacím režimu)
- Přidejte emaily uživatelů, kteří mohou aplikaci testovat
- Klikněte **Save and Continue**

## 4. Vytvoření OAuth Client ID

1. V levém menu vyberte **APIs & Services** → **Credentials**
2. Klikněte **+ Create Credentials** → **OAuth client ID**
3. Vyberte **Application type**: **Web application**
4. Zadejte **Name**: např. "Sazkomat Web Client"

### Authorized JavaScript origins:
Pro development přidejte:
```
http://localhost:3000
```

Pro production (sazkomat.herma.cz):
```
https://sazkomat.herma.cz
```

### Authorized redirect URIs:
Pro development:
```
http://localhost:3000
http://localhost:3000/api/auth/callback/google
```

Pro production (sazkomat.herma.cz):
```
https://sazkomat.herma.cz
https://sazkomat.herma.cz/api/auth/callback/google
```

5. Klikněte **Create**

## 5. Získání credentials

Po vytvoření se zobrazí dialog s:
- **Client ID** - dlouhý řetězec končící na `.apps.googleusercontent.com`
- **Client Secret** - kratší tajný klíč

**Uložte si obě hodnoty!**

## 6. Konfigurace v Sazkomat

### Frontend (.env.local)

Vytvořte nebo upravte soubor `frontend/.env.local`:

```env
NEXT_PUBLIC_GOOGLE_CLIENT_ID=vase-client-id.apps.googleusercontent.com
```

### Backend (appsettings.json)

Upravte `src/Sazkomat.Api/appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "vase-client-id.apps.googleusercontent.com",
      "ClientSecret": "vas-client-secret"
    }
  }
}
```

**Poznámka:** Pro produkční nasazení použijte environment proměnné nebo secret manager místo uložení credentials přímo v appsettings.json.

## 7. Restartování aplikace

Po změně konfigurace restartujte backend i frontend:

```bash
# Backend
cd src/Sazkomat.Api
dotnet run

# Frontend
cd frontend
npm run dev
```

## Troubleshooting

### "Error 400: redirect_uri_mismatch"
- Zkontrolujte, že URL v **Authorized redirect URIs** přesně odpovídá URL vaší aplikace
- Pozor na trailing slash (`/`) - musí být konzistentní

### "Error 403: access_denied"
- Pro External typ v testovacím režimu: přidejte svůj email do **Test users**
- Nebo publikujte aplikaci (vyžaduje ověření od Google)

### Google tlačítko se nezobrazuje
- Zkontrolujte, že `NEXT_PUBLIC_GOOGLE_CLIENT_ID` je správně nastaveno
- Otevřete Developer Tools → Console a hledejte chyby

### "Invalid client" error
- Ověřte, že Client ID a Client Secret jsou správné
- Zkontrolujte, že jste neomylem nezkopírovali mezery

## Publikování aplikace (pro production)

Pro External typ aplikace v produkci:

1. Vraťte se do **OAuth consent screen**
2. Klikněte **Publish App**
3. Google může vyžadovat ověření aplikace, pokud:
   - Požadujete citlivé scopes
   - Máte více než 100 uživatelů

Pro základní scopes (email, profile, openid) obvykle není potřeba plné ověření.

---

**Poslední aktualizace:** 2025-11-27
