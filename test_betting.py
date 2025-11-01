import requests
import json

# Get all leagues
response = requests.get("http://localhost:3001/api/config/leagues")
leagues = response.json()

# Find Prva liga
prva_liga = next((l for l in leagues if l['name'] == 'Prva liga'), None)

if prva_liga:
    print(f"Prva liga ID: {prva_liga['id']}")

    # Get betting availability
    bet_response = requests.get(f"http://localhost:3001/api/config/leagues/{prva_liga['id']}/betting-availability")
    betting_providers = bet_response.json()

    print(f"\nBetting Providers for Prva liga:")
    print(json.dumps(betting_providers, indent=2))
else:
    print("Prva liga not found")
