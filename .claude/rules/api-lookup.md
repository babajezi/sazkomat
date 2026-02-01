# Pravidlo: API lookup

## Před voláním API endpointu

**VŽDY nejprve zkontroluj** `docs/API.md` pro:
- Správnou cestu endpointu
- HTTP metodu
- Request/response typy
- Query parametry

## Při změnách API

**Aktualizuj docs/API.md** když:
1. Přidáš nový endpoint
2. Změníš existující endpoint (cesta, metoda, typy)
3. Odebereš endpoint
4. Změníš request/response strukturu

## Umístění

`docs/API.md`

## Proč

- docs/API.md obsahuje 126+ endpointů
- Prevence chyb z nesprávných cest nebo typů
- Centrální dokumentace pro frontend i backend vývoj
