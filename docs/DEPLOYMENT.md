# Production Deployment Guide

Kompletní průvodce nasazením Sazkomat na produkční server.

**URL:** `https://sazkomat.herma.cz`

## Požadavky na server

### Hardware
- **CPU:** 2+ cores
- **RAM:** 4+ GB
- **Disk:** 40+ GB SSD
- **OS:** Ubuntu 22.04 LTS (doporučeno) nebo Debian 12

### Software
- Docker 24+
- Docker Compose v2+
- Git

## 1. Instalace Docker

```bash
# Aktualizace systému
sudo apt update && sudo apt upgrade -y

# Instalace Docker
curl -fsSL https://get.docker.com | sh

# Přidání uživatele do docker skupiny
sudo usermod -aG docker $USER

# Odhlášení a přihlášení zpět (nebo logout/login)
newgrp docker

# Ověření instalace
docker --version
docker compose version
```

## 2. Klonování repozitáře

```bash
# Přepnutí do adresáře pro aplikace
cd /opt

# Klonování repozitáře
sudo git clone https://github.com/your-repo/sazkomat.git
sudo chown -R $USER:$USER sazkomat
cd sazkomat
```

## 3. Konfigurace prostředí

### 3.1 Vytvoření produkčního .env souboru

```bash
# Kopírování šablony
cp .env.prod.example .env.prod

# Editace konfigurace
nano .env.prod
```

**Povinné proměnné:**

```env
# Silné heslo pro databázi (16+ znaků)
DB_PASSWORD=VelmiSilneHeslo123!@#

# JWT secret key (64+ znaků, generujte pomocí: openssl rand -base64 64)
JWT_SECRET_KEY=zde_vložte_vygenerovaný_klíč

# Admin email
ADMIN_EMAIL=petr@herma.cz
```

**Volitelné (Google OAuth):**

```env
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
```

### 3.2 Google OAuth konfigurace

Pokud chcete povolit přihlašování přes Google:

1. Přejděte do [Google Cloud Console](https://console.cloud.google.com/)
2. Vytvořte OAuth 2.0 Client ID
3. Přidejte do **Authorized redirect URIs**:
   ```
   https://sazkomat.herma.cz
   https://sazkomat.herma.cz/api/auth/callback/google
   ```
4. Doplňte `GOOGLE_CLIENT_ID` a `GOOGLE_CLIENT_SECRET` do `.env.prod`

Viz: `docs/GOOGLE_AUTH_SETUP.md`

## 4. SSL certifikát (Let's Encrypt)

### 4.1 DNS konfigurace

Ujistěte se, že DNS záznam směřuje na IP serveru:
```
sazkomat.herma.cz.  A  YOUR_SERVER_IP
```

### 4.2 Získání certifikátu

```bash
# Inicializace SSL (vyžaduje root)
sudo ./scripts/init-ssl.sh
```

Skript:
1. Spustí dočasný nginx server
2. Získá certifikát od Let's Encrypt
3. Uloží certifikáty do `nginx/ssl/`

### 4.3 Automatická obnova certifikátu

Přidejte cron job pro automatickou obnovu:

```bash
# Editace crontab
sudo crontab -e

# Přidejte řádek (obnova každý den ve 3:00)
0 3 * * * /opt/sazkomat/scripts/renew-ssl.sh >> /var/log/ssl-renewal.log 2>&1
```

## 5. Spuštění aplikace

```bash
# Build a spuštění všech služeb
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build

# Sledování logů
docker compose -f docker-compose.prod.yml logs -f

# Kontrola stavu služeb
docker compose -f docker-compose.prod.yml ps
```

## 6. Databázové migrace

Migrace se spouštějí automaticky při startu API kontejneru.

Pokud chcete spustit migrace manuálně:

```bash
docker exec sazkomat-api dotnet ef database update
```

## 7. Ověření nasazení

### Health check
```bash
curl -k https://sazkomat.herma.cz/health
```

Očekávaná odpověď:
```json
{"status":"healthy","timestamp":"2025-11-27T12:00:00Z","version":"1.0.0"}
```

### Frontend
Otevřete v prohlížeči: `https://sazkomat.herma.cz`

### Hangfire Dashboard
Přihlaste se jako admin a navštivte: `https://sazkomat.herma.cz/hangfire`

## 8. Monitoring a údržba

### Logování

Logy jsou dostupné v:
```bash
# API logy
ls -la logs/

# Docker logy
docker compose -f docker-compose.prod.yml logs api
docker compose -f docker-compose.prod.yml logs frontend
docker compose -f docker-compose.prod.yml logs nginx
```

### Restart služeb

```bash
# Restart všech služeb
docker compose -f docker-compose.prod.yml restart

# Restart konkrétní služby
docker compose -f docker-compose.prod.yml restart api
```

### Aktualizace aplikace

```bash
# Stažení nejnovější verze
git pull origin main

# Rebuild a restart
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

### Záloha databáze

```bash
# Vytvoření zálohy
docker exec sazkomat-postgres pg_dump -U sazkomat sazkomat_db > backup_$(date +%Y%m%d).sql

# Obnovení ze zálohy
cat backup_20251127.sql | docker exec -i sazkomat-postgres psql -U sazkomat sazkomat_db
```

## 9. Troubleshooting

### Nginx nefunguje

```bash
# Kontrola konfigurace
docker exec sazkomat-nginx nginx -t

# Reload konfigurace
docker exec sazkomat-nginx nginx -s reload
```

### API nereaguje

```bash
# Kontrola logů
docker compose -f docker-compose.prod.yml logs api

# Restart API
docker compose -f docker-compose.prod.yml restart api
```

### SSL certifikát expiroval

```bash
# Manuální obnova
sudo ./scripts/renew-ssl.sh

# Restart nginx
docker compose -f docker-compose.prod.yml restart nginx
```

### Databáze je pomalá

```bash
# Kontrola připojení
docker exec sazkomat-postgres pg_isready -U sazkomat

# Velikost databáze
docker exec sazkomat-postgres psql -U sazkomat -d sazkomat_db -c "SELECT pg_size_pretty(pg_database_size('sazkomat_db'));"
```

## 10. Bezpečnostní doporučení

1. **Firewall:** Povolte pouze porty 80, 443 a 22 (SSH)
   ```bash
   sudo ufw allow 22
   sudo ufw allow 80
   sudo ufw allow 443
   sudo ufw enable
   ```

2. **SSH:** Zakažte přihlašování root uživatele a používejte SSH klíče

3. **Aktualizace:** Pravidelně aktualizujte systém a Docker images
   ```bash
   sudo apt update && sudo apt upgrade -y
   docker compose -f docker-compose.prod.yml pull
   ```

4. **Zálohy:** Nastavte automatické zálohy databáze

5. **Monitoring:** Zvažte nasazení monitoring řešení (Uptime Kuma, Grafana)

---

## Rychlý přehled příkazů

```bash
# Start
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Stop
docker compose -f docker-compose.prod.yml down

# Logy
docker compose -f docker-compose.prod.yml logs -f

# Status
docker compose -f docker-compose.prod.yml ps

# Rebuild
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build

# Záloha DB
docker exec sazkomat-postgres pg_dump -U sazkomat sazkomat_db > backup.sql
```

---

**Poslední aktualizace:** 2025-11-27
