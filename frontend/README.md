# Sazkomat Frontend

Next.js 15 frontend pro platformu Sazkomat - import a analýza historických sázkových dat.

## Technologie

- **Next.js 15** - App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **shadcn/ui** - UI komponenty
- **React Query** - Data fetching a cache
- **Axios** - HTTP klient

## Vývoj

```bash
# Instalace závislostí
npm install

# Vývoj server (http://localhost:3000)
npm run dev

# Build pro produkci
npm build

# Spuštění produkční verze
npm start
```

## Proměnné prostředí

Vytvořte `.env.local` soubor:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Struktura

```
frontend/
├── app/              # Next.js App Router stránky
│   ├── leagues/      # Správa lig
│   ├── import/       # Import dat
│   └── layout.tsx    # Root layout
├── components/
│   └── ui/           # shadcn/ui komponenty
├── lib/
│   ├── api/          # API klient a typy
│   ├── providers.tsx # React Query provider
│   └── utils.ts      # Utility funkce
└── public/           # Statické soubory
```

## Funkce

- **Konfigurace lig** - Zobrazení a správa sportovních lig
- **Historický import** - Spuštění importu dat z BetExplorer
- **Monitoring importu** - Sledování progress běžícího importu
- **Responzivní design** - Optimalizováno pro desktop i mobil
