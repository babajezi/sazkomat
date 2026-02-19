# Pravidlo: Ověření změn před předáním uživateli

Po provedení změn, které uživatel bude testovat, **VŽDY ověř že aplikace funguje**.

## Kdy ověřovat

1. **Změny v backendu** (.cs soubory)
   - Restartovat API: `docker-compose restart api` nebo `dotnet run`
   - Ověřit že API běží: `curl http://localhost:3001/health`

2. **Změny ve frontendu** (.tsx, .ts soubory)
   - **Dev mode** (výchozí `docker-compose up`): volume mount + hot reload funguje automaticky
     - Změny v .tsx/.ts souborech se projeví okamžitě bez rebuildu
     - Stačí ověřit: `curl -s -o /dev/null -w "%{http_code}" http://localhost:3000`
   - **Production mode** (`docker-compose -f docker-compose.yml up`): soubory jsou v buildu
     - Proto: `docker-compose -f docker-compose.yml build frontend && docker-compose -f docker-compose.yml up -d frontend`
     - Počkat ~5s na start
     - Pouhý `restart` NESTAČÍ — musí se rebuildit image

3. **Změny v migraci/DB schématu**
   - Ověřit že migrace proběhla: zkontrolovat logy API
   - Nebo ručně: `dotnet ef database update`

4. **Změny v docker-compose.yml**
   - Restartovat služby: `docker-compose up -d`

5. **Změny v konfiguraci** (appsettings.json, .env)
   - Restartovat příslušnou službu

## Co ověřit

- [ ] Služba běží (health check, logy bez chyb)
- [ ] Změna je viditelná/funkční
- [ ] Žádné regrese v souvisejících částech

## Příklad správného postupu

```
Claude: [provede změny v API]
Claude: [restartuje API]
Claude: [ověří health check]
Claude: "Změny jsou hotové a API běží. Můžeš otestovat na http://localhost:3001/..."
```

## Příklad ŠPATNÉHO postupu

```
Claude: [provede změny v API]
Claude: "Hotovo, můžeš otestovat."
Uživatel: [testuje, ale vidí starou verzi protože API nebylo restartováno]
```
