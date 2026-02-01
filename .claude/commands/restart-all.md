Restartuj všechny Docker služby:

1. Spusť `docker-compose restart`
2. Počkej 10 sekund
3. Zobraz status všech služeb: `docker-compose ps`
4. Ověř API health check: `curl -s http://localhost:3001/health`
5. Informuj uživatele o výsledku
