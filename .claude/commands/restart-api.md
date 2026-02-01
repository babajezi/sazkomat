Restartuj API službu a ověř že běží:

1. Spusť `docker-compose restart api`
2. Počkej 5 sekund
3. Ověř health check: `curl -s http://localhost:3001/health`
4. Pokud health check selže, zobraz logy: `docker-compose logs --tail=30 api`
5. Informuj uživatele o výsledku
