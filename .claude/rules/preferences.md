# Osobní preference

## Subscription model
Uživatel má Claude subscription (neplatí za tokeny).

**VŽDY preferuj:**
- Architektonicky čistší řešení před rychlým hackem
- Správné abstrakce a design patterns
- Rozšiřitelný a udržovatelný kód
- Proper separation of concerns

**NIKDY:**
- Nenavrhuj jednodušší řešení kvůli úspoře tokenů/času
- Neříkej "pro jednoduchost" jako důvod pro horší architekturu

## Analýza před implementací

Před implementací změny **VŽDY zvaž:**
- Vedlejší efekty a konflikty s existujícím kódem
- Pokud přidáváš nový mechanismus (converter, middleware, decorator, atd.), zkontroluj jestli nekoliduje s existujícím
- Zda existující atributy/konfigurace nebudou v konfliktu s novým kódem
- Jak změna ovlivní ostatní části systému

**Cíl:** Vyhnout se zbytečným iteracím přes chyby, které bylo možné předvídat.
