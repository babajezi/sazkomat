"use client";

import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ArrowLeft, Database, ScanLine, Download, Activity, Info, RefreshCw } from "lucide-react";
import { ScanDialog } from "@/components/ScanDialog";
import { CacheTablesView } from "@/components/CacheTablesView";
import { JobsPanel } from "@/components/JobsPanel";
import { ProviderLogo } from "@/components/ProviderLogo";
import { SyncEntityType, parseScanCapabilities, DataProvider } from "@/lib/api/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";
const BETANO_PROVIDER_ID = "b0000000-0000-0000-0000-000000000001";

export default function SyncPage() {
  const [selectedProviderId, setSelectedProviderId] = useState<string>(BETANO_PROVIDER_ID);
  const [isBackfilling, setIsBackfilling] = useState(false);
  const [isBackfillingLP, setIsBackfillingLP] = useState(false);
  const queryClient = useQueryClient();

  // Fetch all providers
  const { data: providers = [] } = useQuery({
    queryKey: ["providers"],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/providers`);
      if (!res.ok) throw new Error("Failed to fetch providers");
      return res.json();
    },
  });

  const activeProviders = providers.filter((p: DataProvider) => p.isActive);
  const selectedProvider = providers.find((p: DataProvider) => p.id === selectedProviderId);
  const scanCapabilities = parseScanCapabilities(selectedProvider?.scanCapabilities);

  const handleBackfillProviderLeagues = async () => {
    setIsBackfilling(true);
    try {
      const response = await fetch(`${API_URL}/api/scan/backfill-provider-leagues`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ providerId: selectedProviderId }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Failed to backfill");
      }

      const result = await response.json();
      alert(`Backfill provider_leagues dokončen: ${result.created} vytvořeno, ${result.updated} aktualizováno`);

      // Refresh the cache tables view
      queryClient.invalidateQueries({ queryKey: ["provider-cache"] });
    } catch (error) {
      console.error("Backfill error:", error);
      alert(`Chyba: ${error instanceof Error ? error.message : "Unknown error"}`);
    } finally {
      setIsBackfilling(false);
    }
  };

  const handleBackfillLeagueProviders = async () => {
    setIsBackfillingLP(true);
    try {
      const response = await fetch(`${API_URL}/api/scan/backfill-league-providers`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ providerId: selectedProviderId }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Failed to backfill");
      }

      const result = await response.json();
      alert(`Backfill LeagueProvider dokončen: ${result.created} vytvořeno, ${result.skipped} již existovalo`);

      // Refresh the leagues view
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    } catch (error) {
      console.error("Backfill LP error:", error);
      alert(`Chyba: ${error instanceof Error ? error.message : "Unknown error"}`);
    } finally {
      setIsBackfillingLP(false);
    }
  };

  return (
    <div className="container mx-auto py-8 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-4 mb-2">
            <Link href="/">
              <Button variant="ghost" size="sm">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Zpět na úvodní stránku
              </Button>
            </Link>
          </div>
          <h1 className="text-3xl font-bold">Provider Synchronization</h1>
          <p className="text-muted-foreground">
            3-step workflow: Scan → Preview → Import
          </p>
        </div>
        <div className="flex gap-2">
          <Link href="/country-mappings">
            <Button variant="outline">
              <Database className="mr-2 h-4 w-4" />
              Mapování Zemí
            </Button>
          </Link>
          <Link href="/unmatched-leagues">
            <Button variant="outline">
              <Database className="mr-2 h-4 w-4" />
              Nespárované Ligy
            </Button>
          </Link>
          <Link href="/jobs">
            <Button variant="outline">
              <Activity className="mr-2 h-4 w-4" />
              Monitor Jobs
            </Button>
          </Link>
        </div>
      </div>

      {/* Provider Selector */}
      <Card>
        <CardHeader>
          <CardTitle>Vybrat Provider</CardTitle>
          <CardDescription>
            Vyber který provider chceš scanovat a synchronizovat
          </CardDescription>
        </CardHeader>
        <CardContent>
          {activeProviders.length > 0 ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {activeProviders.map((provider: any) => (
                <Button
                  key={provider.id}
                  type="button"
                  variant={selectedProviderId === provider.id ? "default" : "outline"}
                  className="h-auto py-4 px-4 justify-start"
                  onClick={() => setSelectedProviderId(provider.id)}
                >
                  <ProviderLogo provider={provider} size="sm" className="mr-3" />
                  <div className="text-left flex-1">
                    <div className="font-medium">{provider.name}</div>
                    <div className="text-xs opacity-70">{provider.code}</div>
                  </div>
                </Button>
              ))}
            </div>
          ) : (
            <div className="text-sm text-muted-foreground">
              Žádní aktivní providers nejsou k dispozici.
            </div>
          )}
        </CardContent>
      </Card>

      {/* Jobs Panel - Live status */}
      <JobsPanel providerId={selectedProviderId} maxJobs={5} refreshInterval={2000} />

      {/* Workflow Info */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          <div className="space-y-2">
            <p className="font-semibold">Jak synchronizace funguje:</p>
            <ol className="list-decimal list-inside space-y-1 text-sm">
              <li>
                <strong>SCAN</strong> - Načte data z providera do cache tabulek
              </li>
              <li>
                <strong>PREVIEW</strong> - Zkontroluj data před importem (v tabulkách níže)
              </li>
              <li>
                <strong>IMPORT</strong> - Vyber položky a importuj je do hlavní databáze
              </li>
            </ol>
          </div>
        </AlertDescription>
      </Alert>

      {/* Step 1: SCAN */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <ScanLine className="h-5 w-5" />
            <div>
              <CardTitle>Krok 1: Scan Provider Data</CardTitle>
              <CardDescription>
                Načte data z BetExplorer do dočasných cache tabulek
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Spusť scan pro jednotlivé typy dat. Data se uloží do cache a můžeš je
              zkontrolovat před importem.
            </p>

            <div className="flex flex-wrap gap-3">
              {scanCapabilities.canScanCountries && (
                <ScanDialog
                  entityType={SyncEntityType.Countries}
                  providerId={selectedProviderId}
                  trigger={
                    <Button variant="default">
                      <ScanLine className="mr-2 h-4 w-4" />
                      Scan Countries
                    </Button>
                  }
                />
              )}

              {scanCapabilities.canScanLeagues && (
                <ScanDialog
                  entityType={SyncEntityType.Leagues}
                  providerId={selectedProviderId}
                  trigger={
                    <Button variant="default">
                      <ScanLine className="mr-2 h-4 w-4" />
                      Scan Leagues
                    </Button>
                  }
                />
              )}

              {scanCapabilities.canScanSeasons && (
                <ScanDialog
                  entityType={SyncEntityType.Seasons}
                  providerId={selectedProviderId}
                  trigger={
                    <Button variant="default">
                      <ScanLine className="mr-2 h-4 w-4" />
                      Scan Seasons
                    </Button>
                  }
                />
              )}

              {/* Backfill resolved leagues buttons */}
              <Button
                variant="secondary"
                onClick={handleBackfillProviderLeagues}
                disabled={isBackfilling}
                title="Doplní provider_leagues záznamy z vyřešených unmatched_leagues"
              >
                <RefreshCw className={`mr-2 h-4 w-4 ${isBackfilling ? "animate-spin" : ""}`} />
                {isBackfilling ? "Backfilling..." : "Backfill Cache"}
              </Button>
              <Button
                variant="secondary"
                onClick={handleBackfillLeagueProviders}
                disabled={isBackfillingLP}
                title="Doplní LeagueProvider mapování z vyřešených unmatched_leagues (pro filtrování lig)"
              >
                <RefreshCw className={`mr-2 h-4 w-4 ${isBackfillingLP ? "animate-spin" : ""}`} />
                {isBackfillingLP ? "Backfilling..." : "Backfill Mappings"}
              </Button>
            </div>

            <Alert className="bg-blue-50 border-blue-200">
              <Database className="h-4 w-4 text-blue-600" />
              <AlertDescription className="text-blue-900 text-sm">
                Scan operace běží na pozadí. Průběh vidíš v panelu &quot;Průběh jobů&quot; výše.
              </AlertDescription>
            </Alert>
          </div>
        </CardContent>
      </Card>

      {/* Step 2 & 3: PREVIEW + IMPORT */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Download className="h-5 w-5" />
            <div>
              <CardTitle>Krok 2 & 3: Preview a Import</CardTitle>
              <CardDescription>
                Zkontroluj nascanovaná data a vyber co chceš importovat
                {selectedProvider && ` (${selectedProvider.name})`}
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <CacheTablesView providerId={selectedProviderId} />
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Links</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Link href="/jobs">
              <Button variant="outline" className="w-full">
                <Activity className="mr-2 h-4 w-4" />
                Job Monitoring
              </Button>
            </Link>
            <Link href="/countries">
              <Button variant="outline" className="w-full">
                Manage Countries
              </Button>
            </Link>
            <Link href="/leagues">
              <Button variant="outline" className="w-full">
                Manage Leagues
              </Button>
            </Link>
          </div>
        </CardContent>
      </Card>

      {/* Hangfire Dashboard Link */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription className="text-sm">
          Pro pokročilý monitoring background jobů můžeš použít{" "}
          <a
            href={`${process.env.NEXT_PUBLIC_API_URL?.replace('/api', '') || ''}/hangfire`}
            target="_blank"
            rel="noopener noreferrer"
            className="font-semibold underline"
          >
            Hangfire Dashboard
          </a>
          .
        </AlertDescription>
      </Alert>
    </div>
  );
}
