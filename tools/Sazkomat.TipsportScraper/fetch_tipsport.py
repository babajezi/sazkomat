#!/usr/bin/env python3
"""
Tipsport Scraper - obchází Cloudflare pomocí TLS impersonation.

Instalace:
    pip install curl_cffi requests

Použití:
    python fetch_tipsport.py
    python fetch_tipsport.py --api-url http://192.168.1.100:3001
"""

import argparse
import json
import sys

try:
    from curl_cffi import requests as cffi_requests
except ImportError:
    print("ERROR: curl_cffi není nainstalován.")
    print("Spusť: pip install curl_cffi")
    sys.exit(1)

import requests

TIPSPORT_API = "https://www.tipsport.cz/rest/offer/v6/sports"
TIPSPORT_PROVIDER_ID = "b0000000-0000-0000-0000-000000000004"

# Mapování českých názvů na country codes
COUNTRY_MAP = {
    'anglick': 'england', 'německ': 'germany', 'španělsk': 'spain',
    'italsk': 'italy', 'francouzsk': 'france', 'česk': 'czech-republic',
    'polsk': 'poland', 'portug': 'portugal', 'nizozemsk': 'netherlands',
    'belgick': 'belgium', 'rakousk': 'austria', 'švýcarsk': 'switzerland',
    'skotsk': 'scotland', 'řeck': 'greece', 'tureck': 'turkey',
    'slovensk': 'slovakia', 'maďarsk': 'hungary', 'rumunsk': 'romania',
    'dánsk': 'denmark', 'norsk': 'norway', 'švédsk': 'sweden',
    'finsk': 'finland', 'srbsk': 'serbia', 'chorvatsk': 'croatia',
    'australsk': 'australia', 'brazilsk': 'brazil', 'argentin': 'argentina',
    'japonsk': 'japan', 'korejsk': 'south-korea', 'čínsk': 'china',
    'amerik': 'usa', 'mexick': 'mexico', 'hondurask': 'honduras',
    'indonésk': 'indonesia', 'izraelsk': 'israel', 'kuvajt': 'kuwait',
    'kypersk': 'cyprus', 'malajsijsk': 'malaysia', 'bahrajn': 'bahrain',
    'alžírsk': 'algeria', 'saudskoarabsk': 'saudi-arabia',
    'severoirsk': 'northern-ireland', 'velšsk': 'wales', 'thajsk': 'thailand',
    'sae': 'uae', 'irsk': 'ireland', 'ukrajinsk': 'ukraine', 'rusk': 'russia',
    'bulharsk': 'bulgaria', 'slovinsk': 'slovenia'
}


def derive_country(title: str) -> str | None:
    """Odvodí country code z českého názvu ligy."""
    title_lower = title.lower()
    for key, country in COUNTRY_MAP.items():
        if key in title_lower:
            return country
    return None


def fetch_tipsport_data() -> dict | None:
    """Stáhne data z Tipsport API pomocí Chrome TLS impersonation."""
    print("[1/3] Stahuji data z Tipsport API...")

    try:
        response = cffi_requests.get(
            f"{TIPSPORT_API}?fromResults=false&withLive=true",
            impersonate="chrome",
            headers={
                "Accept": "application/json",
                "Accept-Language": "cs-CZ,cs;q=0.9",
                "Referer": "https://www.tipsport.cz/kurzy/fotbal-16",
            },
            timeout=30
        )

        if response.status_code == 200:
            print(f"      Úspěch! Staženo {len(response.text):,} bytes")
            return response.json()
        else:
            print(f"      CHYBA: HTTP {response.status_code}")
            print(f"      Response: {response.text[:500]}")
            return None

    except Exception as e:
        print(f"      CHYBA: {e}")
        return None


def extract_football_leagues(data: dict) -> list[dict]:
    """Extrahuje fotbalové soutěže z Tipsport response."""
    print("[2/3] Extrahuji fotbalové ligy...")

    competitions = []

    def extract_recursive(node):
        if node.get("type") == "COMPETITION":
            competitions.append(node)
        for child in node.get("children", []):
            extract_recursive(child)

    for child in data.get("data", {}).get("children", []):
        extract_recursive(child)

    # Filtruj jen fotbal (superSportId = 16)
    football = [c for c in competitions if c.get("superSportId") == 16]

    print(f"      Nalezeno {len(football)} fotbalových lig")

    # Převeď na payload formát
    leagues = []
    for c in football:
        leagues.append({
            "providerLeagueId": str(c["id"]),
            "providerLeagueName": c.get("title", ""),
            "countryCode": derive_country(c.get("title", "")),
            "url": c.get("url", ""),
            "matchCount": c.get("count", 0)
        })

    return leagues


def push_to_api(api_url: str, leagues: list[dict]) -> bool:
    """Odešle ligy na Sazkomat API."""
    print(f"[3/3] Odesílám {len(leagues)} lig na API...")

    payload = {
        "providerId": TIPSPORT_PROVIDER_ID,
        "leagues": leagues
    }

    try:
        response = requests.post(
            f"{api_url}/api/tipsport/leagues",
            json=payload,
            timeout=60
        )

        if response.status_code == 200:
            result = response.json()
            print(f"      Úspěch!")
            print(f"      - Nových lig: {result.get('newLeagues', 0)}")
            print(f"      - Aktualizovaných: {result.get('updatedLeagues', 0)}")
            return True
        else:
            print(f"      CHYBA: HTTP {response.status_code}")
            print(f"      Response: {response.text}")
            return False

    except Exception as e:
        print(f"      CHYBA: {e}")
        return False


def main():
    parser = argparse.ArgumentParser(description="Tipsport Scraper s Cloudflare bypass")
    parser.add_argument("--api-url", default="http://localhost:3001",
                        help="URL Sazkomat API (default: http://localhost:3001)")
    parser.add_argument("--dry-run", action="store_true",
                        help="Jen stáhne data, neposílá na API")
    args = parser.parse_args()

    print("=" * 50)
    print("  Tipsport Scraper (curl_cffi)")
    print("=" * 50)
    print(f"API URL: {args.api_url}")
    print()

    # 1. Stáhni data
    data = fetch_tipsport_data()
    if not data:
        print("\nSelhalo stahování dat z Tipsport.")
        return 1

    # 2. Extrahuj ligy
    leagues = extract_football_leagues(data)
    if not leagues:
        print("\nŽádné fotbalové ligy nenalezeny.")
        return 1

    # Ukázka
    print("\n      Ukázka lig:")
    for league in leagues[:5]:
        country = league["countryCode"] or "?"
        print(f"        - {league['providerLeagueName']} ({country})")
    if len(leagues) > 5:
        print(f"        ... a dalších {len(leagues) - 5}")
    print()

    # 3. Odešli na API
    if args.dry_run:
        print("(--dry-run: Data neodeslána)")
        return 0

    if push_to_api(args.api_url, leagues):
        print("\n✓ Hotovo!")
        return 0
    else:
        print("\n✗ Odeslání selhalo.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
