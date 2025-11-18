"use client";

import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { importApi } from "@/lib/api/client";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import { CountryFlag } from "@/components/CountryFlag";
import type { ImportJob } from "@/lib/api/types";
import { ImportJobStatus } from "@/lib/api/types";

// Helper function to get status label from enum value
const getStatusLabel = (status: ImportJobStatus): string => {
  const statusMap: Record<ImportJobStatus, string> = {
    [ImportJobStatus.Pending]: "Čeká",
    [ImportJobStatus.Running]: "Probíhá",
    [ImportJobStatus.Completed]: "Dokončeno",
    [ImportJobStatus.Failed]: "Selhalo",
    [ImportJobStatus.PartialSuccess]: "Částečně úspěšné"
  };
  return statusMap[status] || String(status);
};

export default function ImportPage() {
  const [selectedLeagues, setSelectedLeagues] = useState<string[]>([]);
  const [seasons, setSeasons] = useState<string[]>([]);
  const [includeWithoutOdds, setIncludeWithoutOdds] = useState(false);
  const [importAllHistorical, setImportAllHistorical] = useState(false);
  const [currentJobId, setCurrentJobId] = useState<string | null>(null);

  const { data: leagues, isLoading } = useQuery({
    queryKey: ["available-leagues"],
    queryFn: () => importApi.getAvailableLeagues(),
  });

  // Poll job status every 2 seconds when a job is running
  const { data: currentJob } = useQuery({
    queryKey: ["job-status", currentJobId],
    queryFn: () => importApi.getJobStatus(currentJobId!),
    enabled: !!currentJobId,
    refetchInterval: (query) => {
      const job = query.state.data;
      // Stop polling if job is completed, failed, or has partial success
      if (job && [ImportJobStatus.Completed, ImportJobStatus.Failed, ImportJobStatus.PartialSuccess].includes(job.status)) {
        return false;
      }
      return 2000; // Poll every 2 seconds
    },
  });

  // Group leagues by country
  const leaguesByCountry = leagues?.reduce((acc, league) => {
    const countryName = league.country?.name || "Ostatní";
    if (!acc[countryName]) {
      acc[countryName] = [];
    }
    acc[countryName].push(league);
    return acc;
  }, {} as Record<string, typeof leagues>);

  // Sort countries alphabetically
  const sortedCountries = Object.entries(leaguesByCountry || {})
    .sort(([nameA], [nameB]) => nameA.localeCompare(nameB));

  const startImportMutation = useMutation({
    mutationFn: importApi.startHistoricalImport,
    onSuccess: (data) => {
      setCurrentJobId(data.jobId);
      // Alert removed - job status will be shown in the panel
    },
    onError: (error: any) => {
      alert(`Chyba při spuštění importu: ${error.response?.data?.error || error.message}`);
    },
  });

  const handleStartImport = () => {
    if (selectedLeagues.length === 0) {
      alert("Vyberte alespoň jednu ligu");
      return;
    }
    if (!importAllHistorical && seasons.length === 0) {
      alert("Zadejte alespoň jednu sezónu nebo zaškrtněte 'Importovat všechny historické sezóny'");
      return;
    }

    startImportMutation.mutate({
      leagueIds: selectedLeagues,
      seasons: importAllHistorical ? undefined : seasons,
      includeWithoutOdds,
      importAllHistorical,
    });
  };

  const toggleLeague = (leagueId: string) => {
    setSelectedLeagues((prev) =>
      prev.includes(leagueId)
        ? prev.filter((id) => id !== leagueId)
        : [...prev, leagueId]
    );
  };

  const handleSeasonsChange = (value: string) => {
    // Parse comma-separated seasons
    const parsed = value
      .split(",")
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    setSeasons(parsed);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Historický Import Dat</h1>
            <p className="text-gray-600">
              Importujte historická data z BetExplorer
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        <div className="grid gap-6 lg:grid-cols-2">
          <div>
            <Card>
              <CardHeader>
                <CardTitle>Konfigurace Importu</CardTitle>
                <CardDescription>
                  Vyberte ligy a sezóny pro import
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Dostupné ligy ({selectedLeagues.length} vybráno)
                  </label>
                  <div className="space-y-4 max-h-96 overflow-y-auto border rounded p-3">
                    {sortedCountries && sortedCountries.length === 0 && (
                      <p className="text-sm text-gray-500 text-center py-4">
                        Žádné povolené ligy k importu. Povoľte ligy v konfiguraci.
                      </p>
                    )}
                    {sortedCountries && sortedCountries.map(([countryName, countryLeagues]) => (
                      <div key={countryName}>
                        <h3 className="text-sm font-semibold text-gray-700 mb-2 pb-1 border-b flex items-center gap-2">
                          {countryLeagues[0]?.country?.isoCode && (
                            <CountryFlag isoCode={countryLeagues[0].country.isoCode} className="text-base" />
                          )}
                          <span>{countryName}</span>
                          <span className="text-xs font-normal text-gray-500">
                            ({countryLeagues.length} {countryLeagues.length === 1 ? 'liga' : 'lig'})
                          </span>
                        </h3>
                        <div className="space-y-2 ml-2">
                          {countryLeagues?.map((league) => (
                            <div
                              key={league.id}
                              className="flex items-center space-x-2"
                            >
                              <input
                                type="checkbox"
                                id={league.id}
                                checked={selectedLeagues.includes(league.id)}
                                onChange={() => toggleLeague(league.id)}
                                className="h-4 w-4"
                              />
                              <label
                                htmlFor={league.id}
                                className="text-sm cursor-pointer"
                              >
                                {league.displayName}
                              </label>
                            </div>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div>
                  <label
                    htmlFor="seasons"
                    className="text-sm font-medium mb-2 block"
                  >
                    Sezóny (oddělené čárkou, např.: 2023-2024, 2022-2023)
                    {importAllHistorical && (
                      <span className="ml-2 text-xs text-gray-500 italic">
                        (deaktivováno - importují se všechny historické)
                      </span>
                    )}
                  </label>
                  <input
                    id="seasons"
                    type="text"
                    className="w-full border rounded p-2 text-sm disabled:bg-gray-100 disabled:cursor-not-allowed disabled:text-gray-500"
                    placeholder="2023-2024, 2022-2023"
                    onChange={(e) => handleSeasonsChange(e.target.value)}
                    disabled={importAllHistorical}
                  />
                  {!importAllHistorical && seasons.length > 0 && (
                    <div className="mt-2 text-sm text-gray-600">
                      Vybrané sezóny: {seasons.join(", ")}
                    </div>
                  )}
                </div>

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="importAllHistorical"
                    checked={importAllHistorical}
                    onChange={(e) => {
                      const checked = e.target.checked;
                      setImportAllHistorical(checked);
                      if (checked) {
                        // Clear manual season input when enabling import all
                        setSeasons([]);
                      }
                    }}
                    className="h-4 w-4"
                  />
                  <label
                    htmlFor="importAllHistorical"
                    className="text-sm cursor-pointer font-medium"
                  >
                    Importovat VŠECHNY historické sezóny
                  </label>
                </div>

                {importAllHistorical && (
                  <div className="text-sm text-amber-700 bg-amber-50 p-3 rounded-lg border border-amber-200">
                    <div className="flex items-start space-x-2">
                      <span className="text-amber-600 font-bold text-lg">⚠️</span>
                      <div>
                        <p className="font-medium">Upozornění:</p>
                        <p className="mt-1">
                          Bude importováno VŠECHNO dostupné historické data z BetExplorer
                          (kromě aktuální sezóny). Import může trvat velmi dlouho.
                        </p>
                      </div>
                    </div>
                  </div>
                )}

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="includeWithoutOdds"
                    checked={includeWithoutOdds}
                    onChange={(e) => setIncludeWithoutOdds(e.target.checked)}
                    className="h-4 w-4"
                  />
                  <label
                    htmlFor="includeWithoutOdds"
                    className="text-sm cursor-pointer"
                  >
                    Zahrnout kola bez kurzů
                  </label>
                </div>

                <Button
                  className="w-full"
                  onClick={handleStartImport}
                  disabled={startImportMutation.isPending}
                >
                  {startImportMutation.isPending
                    ? "Spouštím..."
                    : "Spustit Import"}
                </Button>
              </CardContent>
            </Card>
          </div>

          <div>
            {currentJob && (
              <Card>
                <CardHeader>
                  <CardTitle>Stav Importu</CardTitle>
                  <CardDescription>Job ID: {currentJob.id}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div>
                      <span className="font-medium">Status:</span>{" "}
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          currentJob.status === ImportJobStatus.Completed
                            ? "bg-green-100 text-green-800"
                            : currentJob.status === ImportJobStatus.Failed
                            ? "bg-red-100 text-red-800"
                            : currentJob.status === ImportJobStatus.Running
                            ? "bg-blue-100 text-blue-800"
                            : currentJob.status === ImportJobStatus.PartialSuccess
                            ? "bg-yellow-100 text-yellow-800"
                            : "bg-gray-100 text-gray-800"
                        }`}
                      >
                        {getStatusLabel(currentJob.status)}
                      </span>
                    </div>
                    {currentJob.progress.currentSeason && (
                      <div>
                        <span className="font-medium">Aktuální sezóna:</span>{" "}
                        <span className="font-mono">{currentJob.progress.currentSeason}</span>
                      </div>
                    )}
                    <div>
                      <span className="font-medium">Progress:</span>{" "}
                      {Array.isArray(currentJob.progress.processedSeasons)
                        ? currentJob.progress.processedSeasons.length
                        : (currentJob.progress.processedSeasons || 0)} /{" "}
                      {currentJob.progress.totalSeasons} sezón
                    </div>
                    <div>
                      <span className="font-medium">Kola:</span>{" "}
                      {currentJob.progress.processedRounds || 0}
                    </div>
                    {currentJob.progress.errors && currentJob.progress.errors.length > 0 && (
                      <div className="text-sm">
                        <span className="font-medium">Chyby:</span>{" "}
                        <span className="text-red-600">{currentJob.progress.errors.length}</span>
                        <div className="mt-1 text-xs text-red-600 max-h-24 overflow-y-auto">
                          {currentJob.progress.errors.map((err, idx) => (
                            <div key={idx}>• {err}</div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            )}

            {!currentJob && (
              <>
                <Card>
                  <CardHeader>
                    <CardTitle>Informace</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm text-gray-600 space-y-2">
                    <p>
                      Po spuštění importu se zobrazí progress a můžete sledovat
                      stav importu.
                    </p>
                    <p>
                      Import běží na pozadí a může trvat několik minut v
                      závislosti na počtu lig a sezón.
                    </p>
                    <p>
                      Data jsou scrappována z BetExplorer.com s respektováním
                      rate limitů.
                    </p>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Mapování Názvů</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <p className="text-sm text-gray-600">
                      Pokud automatické mapování nepracuje správně, můžete vytvořit manuální mapování:
                    </p>
                    <div className="flex flex-col gap-2">
                      <Link href="/mappings">
                        <Button variant="outline" className="w-full justify-start">
                          🔗 Mapování názvů lig
                        </Button>
                      </Link>
                      <Link href="/country-mappings">
                        <Button variant="outline" className="w-full justify-start">
                          🌍 Mapování názvů zemí
                        </Button>
                      </Link>
                    </div>
                  </CardContent>
                </Card>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
