# WSL2 Migration Plan - Sazkomat

**Datum:** 31. října 2025
**Důvod:** Docker Desktop na Windows je nestabilní

---

## 🎯 Proč WSL2?

### Problémy na Windows:
- ❌ Docker Desktop padá
- ❌ Networking issues (port conflicts)
- ❌ Playwright browser installation komplikovaná
- ❌ Pomalá file I/O

### Výhody WSL2:
- ✅ Native Linux Docker support
- ✅ Stabilní performance
- ✅ Rychlejší file operations
- ✅ Jednodušší Playwright setup
- ✅ Lepší developer experience

---

## 📋 Migration Checklist

### 1. Instalace WSL2

```powershell
# Zapnout WSL2
wsl --install

# Nebo pokud už máš WSL1:
wsl --set-default-version 2
```

### 2. Instalace Ubuntu

```powershell
# Nainstalovat Ubuntu 24.04 LTS
wsl --install -d Ubuntu-24.04

# Nebo z Microsoft Store
# - Otevřít Microsoft Store
# - Vyhledat "Ubuntu 24.04"
# - Kliknout Install
```

### 3. První Spuštění Ubuntu

```bash
# WSL otevře terminál a požádá o:
# - Username (např.: petr)
# - Password

# Update systému
sudo apt update && sudo apt upgrade -y
```

### 4. Instalace .NET 9 SDK

```bash
# Přidat Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Instalovat .NET SDK
sudo apt update
sudo apt install -y dotnet-sdk-9.0

# Ověřit instalaci
dotnet --version
# Očekáváno: 9.0.x
```

### 5. Instalace Node.js 20

```bash
# Instalovat nvm (Node Version Manager)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Reload shell
source ~/.bashrc

# Instalovat Node.js 20
nvm install 20
nvm use 20

# Ověřit instalaci
node --version  # v20.x.x
npm --version   # 10.x.x
```

### 6. Instalace Docker

```bash
# Docker je už součástí Docker Desktop WSL2 backendu
# Stačí v Docker Desktop Settings povolit WSL2 integration

# Ověřit
docker --version
docker-compose --version
```

**NEBO instalovat Docker přímo v WSL:**

```bash
# Instalovat Docker Engine
sudo apt install -y docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker

# Přidat uživatele do docker group
sudo usermod -aG docker $USER

# Logout/login pro aplikaci změn
```

### 7. Instalace Git

```bash
sudo apt install -y git

# Nastavit git config
git config --global user.name "Tvoje Jméno"
git config --global user.email "tvuj@email.com"
```

### 8. Klonování/Kopírování Projektu

**Možnost A: Kopírovat z Windows**

```bash
# V WSL terminál:
# Windows cesty jsou dostupné přes /mnt/c/

# Vytvořit workspace
mkdir -p ~/projects
cd ~/projects

# Kopírovat projekt
cp -r /mnt/c/projects/private/Sazkomat ./

# NEBO symbolický link (nedoporučuji - pomalé)
ln -s /mnt/c/projects/private/Sazkomat ~/projects/Sazkomat
```

**Možnost B: Git Clone (DOPORUČENO)**

```bash
cd ~/projects
git clone <tvoje-repo-url> Sazkomat
cd Sazkomat
```

### 9. Instalace Playwright

```bash
cd ~/projects/Sazkomat/src/Sazkomat.Api

# Instalovat Playwright browsers
pwsh bin/Debug/net9.0/playwright.ps1 install chromium --with-deps

# Nebo
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium --with-deps
```

### 10. Spuštění Projektu

```bash
cd ~/projects/Sazkomat

# Spustit Docker služby
docker-compose up -d

# Počkat 10s na PostgreSQL
sleep 10

# Otevřít v prohlížeči (z Windows)
# http://localhost:3000
```

**NEBO lokální development:**

```bash
# Terminal 1: API
cd ~/projects/Sazkomat/src/Sazkomat.Api
dotnet run

# Terminal 2: Frontend
cd ~/projects/Sazkomat/frontend
npm install  # První spuštění
npm run dev
```

---

## 🔧 Configuration Changes Needed

### Update .env.local (pokud potřeba)

Frontend už má správnou konfiguraci:
```env
NEXT_PUBLIC_API_URL=http://localhost:3001
```

### Docker Compose

Neměnit nic - `docker-compose.yml` funguje na WSL2 stejně.

---

## ✅ Verifikace

### Kontrola že vše funguje:

```bash
# 1. PostgreSQL
docker ps | grep postgres
# Očekáváno: sazkomat-postgres ... Up X seconds (healthy)

# 2. API
curl http://localhost:3001/health
# Očekáváno: {"status":"healthy",...}

# 3. Frontend
curl -I http://localhost:3000
# Očekáváno: HTTP/1.1 200 OK

# 4. Test Playwright scraping
curl -X POST http://localhost:3001/api/import/leagues/92edb3c0-eb08-4af7-91fb-d0efc6929005/seasons/available
# Očekáváno: {"seasons": [...27 items...], "currentSeason": "2026-2027"}
```

---

## 📝 Poznámky Pro Zítra

1. **Backup Windows projektu:**
   ```powershell
   # Před migrací
   cd C:\projects\private
   tar -czf Sazkomat-backup-2025-10-30.tar.gz Sazkomat
   ```

2. **WSL2 File System Performance:**
   - Projekty ukládat do WSL filesystem (`~/projects/`)
   - NE do Windows filesystem (`/mnt/c/projects/`)
   - WSL filesystem je 10x rychlejší!

3. **VS Code WSL Extension:**
   - Nainstalovat "Remote - WSL" extension
   - Otevřít projekt: `code ~/projects/Sazkomat`
   - VS Code se automaticky připojí k WSL

4. **Docker Desktop Settings:**
   - General → Use WSL 2 based engine ✓
   - Resources → WSL Integration → Enable Ubuntu-24.04 ✓

---

## 🎉 Očekávané Výsledky

Po migraci na WSL2:
- ✅ Docker stabilní (bez pádů)
- ✅ Playwright funguje out-of-the-box
- ✅ Rychlejší build times
- ✅ Lepší development experience
- ✅ Žádné port conflicts

---

**Připraven na zítřejší migraci!** 🚀
