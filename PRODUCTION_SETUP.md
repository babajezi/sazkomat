# Production Setup - sazkomat.herma.cz

Rychlý návod pro nasazení Sazkomat na produkční server.

## Vytvořené soubory

### Backend
| Soubor | Popis |
|--------|-------|
| `src/Sazkomat.Api/appsettings.Production.json` | Produkční konfigurace API |
| `src/Sazkomat.Api/Middleware/HangfireAuthorizationFilter.cs` | Autentizace Hangfire dashboardu |

### Frontend
| Soubor | Popis |
|--------|-------|
| `frontend/.env.production` | Produkční environment variables |

### Docker & Nginx
| Soubor | Popis |
|--------|-------|
| `docker-compose.prod.yml` | Produkční Docker Compose |
| `.env.prod.example` | Šablona pro environment variables |
| `nginx/nginx.conf` | Nginx reverse proxy konfigurace |

### SSL skripty
| Soubor | Popis |
|--------|-------|
| `scripts/init-ssl.sh` | Inicializace Let's Encrypt certifikátu |
| `scripts/renew-ssl.sh` | Automatická obnova certifikátu |

---

## Kroky pro nasazení

### 1. Příprava serveru

```bash
# Naklonovat repozitář
git clone https://github.com/your-repo/sazkomat.git /opt/sazkomat
cd /opt/sazkomat

# Vytvořit .env.prod ze šablony
cp .env.prod.example .env.prod
nano .env.prod
```

### 2. Nastavit environment variables

Editovat `.env.prod`:

```env
# POVINNÉ
DB_PASSWORD=silne_heslo_min_16_znaku
JWT_SECRET_KEY=openssl_rand_base64_64_output
ADMIN_EMAIL=petr@herma.cz

# VOLITELNÉ (Google OAuth)
GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=xxx
```

**Generování JWT klíče:**
```bash
openssl rand -base64 64
```

### 3. Získat SSL certifikát

```bash
# Ujisti se, že DNS směřuje na server
# sazkomat.herma.cz -> IP_SERVERU

# Spustit inicializaci SSL
sudo ./scripts/init-ssl.sh
```

### 4. Spustit aplikaci

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

### 5. Ověřit nasazení

```bash
# Health check
curl https://sazkomat.herma.cz/health

# Logy
docker compose -f docker-compose.prod.yml logs -f
```

### 6. Nastavit automatickou obnovu SSL

```bash
sudo crontab -e

# Přidat:
0 3 * * * /opt/sazkomat/scripts/renew-ssl.sh >> /var/log/ssl-renewal.log 2>&1
```

---

## Užitečné příkazy

```bash
# Start
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Stop
docker compose -f docker-compose.prod.yml down

# Restart
docker compose -f docker-compose.prod.yml restart

# Logy
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f frontend
docker compose -f docker-compose.prod.yml logs -f nginx

# Rebuild po změnách
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build

# Záloha databáze
docker exec sazkomat-postgres pg_dump -U sazkomat sazkomat_db > backup_$(date +%Y%m%d).sql
```

---

## Konfigurace Google OAuth (volitelné)

1. [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials
2. Vytvořit OAuth 2.0 Client ID
3. Přidat **Authorized JavaScript origins**:
   ```
   https://sazkomat.herma.cz
   ```
4. Přidat **Authorized redirect URIs**:
   ```
   https://sazkomat.herma.cz
   https://sazkomat.herma.cz/api/auth/callback/google
   ```
5. Doplnit `GOOGLE_CLIENT_ID` a `GOOGLE_CLIENT_SECRET` do `.env.prod`
6. Rebuild frontend: `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build frontend`

---

## Přístupové body

| URL | Popis |
|-----|-------|
| https://sazkomat.herma.cz | Frontend |
| https://sazkomat.herma.cz/api | Backend API |
| https://sazkomat.herma.cz/health | Health check |
| https://sazkomat.herma.cz/hangfire | Job monitoring (vyžaduje admin login) |

---

## Detailní dokumentace

- `docs/DEPLOYMENT.md` - Kompletní deployment guide
- `docs/GOOGLE_AUTH_SETUP.md` - Nastavení Google OAuth

---

**Vytvořeno:** 2025-12-01
