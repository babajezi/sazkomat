"use client";

import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { CheckCircle2, Circle, XCircle, Loader2, AlertCircle, ArrowLeft } from "lucide-react";
import { syncApi } from "@/lib/api/client";
import type { SyncWorkflowState, SyncResponse, Country, League } from "@/lib/api/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";
const BET_EXPLORER_PROVIDER_ID = "a0000000-0000-0000-0000-000000000001";

export default function SyncPage() {
  const queryClient = useQueryClient();
  const [selectedCountries, setSelectedCountries] = useState<Set<string>>(new Set());
  const [selectedLeagues, setSelectedLeagues] = useState<Set<string>>(new Set());
  const [activateCountries, setActivateCountries] = useState(false);

  // Fetch workflow state
  const { data: workflowState, isLoading: loadingState } = useQuery<SyncWorkflowState>({
    queryKey: ["workflow-state"],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/sync/workflow/state`);
      if (!res.ok) throw new Error("Failed to fetch workflow state");
      return res.json();
    },
    refetchInterval: 5000, // Poll every 5 seconds
  });

  // Fetch all countries (show always, even before sync)
  const { data: countries = [] } = useQuery<Country[]>({
    queryKey: ["countries"],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/countries`);
      if (!res.ok) throw new Error("Failed to fetch countries");
      return res.json();
    },
    enabled: true, // Always load countries to show them even before sync
  });

  // Fetch leagues (only show after sync)
  const { data: leagues = [] } = useQuery<League[]>({
    queryKey: ["leagues"],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/leagues`);
      if (!res.ok) throw new Error("Failed to fetch leagues");
      return res.json();
    },
    enabled: workflowState?.leaguesSynced || false,
  });

  // Sync countries mutation
  const syncCountries = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_URL}/api/sync/countries`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          providerId: BET_EXPLORER_PROVIDER_ID,
          activateCountries: activateCountries
        }),
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Sync failed");
      }
      return res.json() as Promise<SyncResponse>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
      queryClient.invalidateQueries({ queryKey: ["countries"] });
      setActivateCountries(false); // Reset checkbox after sync
    },
  });

  // Confirm countries mutation
  const confirmCountries = useMutation({
    mutationFn: async () => {
      // First update selected countries
      await Promise.all(
        Array.from(selectedCountries).map((countryId) =>
          fetch(`${API_URL}/api/config/countries/${countryId}`, {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ isActive: true }),
          })
        )
      );

      // Then confirm
      const res = await fetch(`${API_URL}/api/sync/workflow/confirm-countries`, {
        method: "POST",
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Confirmation failed");
      }
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
      queryClient.invalidateQueries({ queryKey: ["countries"] });
      setSelectedCountries(new Set());
    },
  });

  // Sync leagues mutation
  const syncLeagues = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_URL}/api/sync/leagues`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ providerId: BET_EXPLORER_PROVIDER_ID }),
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Sync failed");
      }
      return res.json() as Promise<SyncResponse>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  // Confirm leagues mutation
  const confirmLeagues = useMutation({
    mutationFn: async () => {
      // First update selected leagues - set both isActive AND isSyncEnabled
      await Promise.all(
        Array.from(selectedLeagues).map((leagueId) =>
          fetch(`${API_URL}/api/config/leagues/${leagueId}`, {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ isActive: true, isSyncEnabled: true }),
          })
        )
      );

      // Then confirm
      const res = await fetch(`${API_URL}/api/sync/workflow/confirm-leagues`, {
        method: "POST",
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Confirmation failed");
      }
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
      setSelectedLeagues(new Set());
    },
  });

  // Sync seasons mutation
  const syncSeasons = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_URL}/api/sync/seasons`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ providerId: BET_EXPLORER_PROVIDER_ID }),
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Sync failed");
      }
      return res.json() as Promise<SyncResponse>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
    },
  });

  // Detect current seasons mutation
  const detectCurrentSeasons = useMutation({
    mutationFn: () => syncApi.detectCurrentSeasons({
      providerId: BET_EXPLORER_PROVIDER_ID,
      type: "Seasons",
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["league-seasons"] });
    },
  });

  // Sync all marked seasons data mutation
  const syncAllMarkedData = useMutation({
    mutationFn: () => syncApi.syncAllMarkedSeasonsData({
      providerId: BET_EXPLORER_PROVIDER_ID,
      type: "Seasons",
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["league-seasons"] });
    },
  });

  // Reset workflow mutation
  const resetWorkflow = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_URL}/api/sync/workflow/reset`, {
        method: "POST",
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Reset failed");
      }
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
      setSelectedCountries(new Set());
      setSelectedLeagues(new Set());
    },
  });

  const toggleCountry = (countryId: string) => {
    const newSet = new Set(selectedCountries);
    if (newSet.has(countryId)) {
      newSet.delete(countryId);
    } else {
      newSet.add(countryId);
    }
    setSelectedCountries(newSet);
  };

  const toggleLeague = (leagueId: string) => {
    const newSet = new Set(selectedLeagues);
    if (newSet.has(leagueId)) {
      newSet.delete(leagueId);
    } else {
      newSet.add(leagueId);
    }
    setSelectedLeagues(newSet);
  };

  if (loadingState) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 space-y-6">
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
          <h1 className="text-3xl font-bold">Synchronization Workflow</h1>
          <p className="text-muted-foreground">
            Step-by-step data synchronization from BetExplorer
          </p>
        </div>
        <Button
          variant="outline"
          onClick={() => resetWorkflow.mutate()}
          disabled={resetWorkflow.isPending}
        >
          {resetWorkflow.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Reset Workflow
        </Button>
      </div>

      {/* Step 1: Countries */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {workflowState?.countriesConfirmed ? (
                <CheckCircle2 className="h-6 w-6 text-green-600" />
              ) : workflowState?.countriesSynced ? (
                <AlertCircle className="h-6 w-6 text-yellow-600" />
              ) : (
                <Circle className="h-6 w-6 text-gray-400" />
              )}
              <div>
                <CardTitle>Step 1: Countries</CardTitle>
                <CardDescription>Synchronize and activate countries</CardDescription>
              </div>
            </div>
            <div className="flex gap-2">
              {workflowState?.countriesConfirmed && (
                <Badge variant="secondary">Confirmed</Badge>
              )}
              {workflowState?.countriesSynced && !workflowState?.countriesConfirmed && (
                <Badge variant="outline">Synced</Badge>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {!workflowState?.countriesSynced && (
            <div className="space-y-4">
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="activateCountries"
                  checked={activateCountries}
                  onChange={(e) => setActivateCountries(e.target.checked)}
                  className="h-4 w-4"
                />
                <label htmlFor="activateCountries" className="text-sm font-medium cursor-pointer">
                  Aktivovat země během synchronizace
                </label>
              </div>

              {activateCountries && (
                <div className="p-3 bg-blue-50 border border-blue-200 rounded text-sm text-blue-900">
                  ℹ️ Země nalezené během synchronizace budou automaticky aktivovány
                </div>
              )}

              <Button
                onClick={() => syncCountries.mutate()}
                disabled={syncCountries.isPending}
              >
                {syncCountries.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Sync Countries
              </Button>
              {syncCountries.error && (
                <Alert variant="destructive" className="mt-4">
                  <AlertDescription>{syncCountries.error.message}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {workflowState?.countriesSynced && !workflowState?.countriesConfirmed && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Select countries you want to track ({selectedCountries.size} selected):
              </p>
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2 max-h-96 overflow-y-auto">
                {countries.map((country) => (
                  <Button
                    key={country.id}
                    variant={selectedCountries.has(country.id) ? "default" : "outline"}
                    size="sm"
                    onClick={() => toggleCountry(country.id)}
                    className="justify-start"
                  >
                    {country.flagEmoji} {country.name}
                  </Button>
                ))}
              </div>
              <Button
                onClick={() => confirmCountries.mutate()}
                disabled={confirmCountries.isPending || selectedCountries.size === 0}
              >
                {confirmCountries.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Confirm Countries ({selectedCountries.size})
              </Button>
              {confirmCountries.error && (
                <Alert variant="destructive">
                  <AlertDescription>{confirmCountries.error.message}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {workflowState?.countriesConfirmed && (
            <p className="text-sm text-green-600">
              ✓ Země potvrzeny. {countries.filter((c) => c.isActive).length} aktivních / {countries.length} celkem.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Step 2: Leagues */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {workflowState?.leaguesConfirmed ? (
                <CheckCircle2 className="h-6 w-6 text-green-600" />
              ) : workflowState?.leaguesSynced ? (
                <AlertCircle className="h-6 w-6 text-yellow-600" />
              ) : (
                <Circle className="h-6 w-6 text-gray-400" />
              )}
              <div>
                <CardTitle>Step 2: Leagues</CardTitle>
                <CardDescription>Synchronize and activate leagues</CardDescription>
              </div>
            </div>
            <div className="flex gap-2">
              {workflowState?.leaguesConfirmed && (
                <Badge variant="secondary">Confirmed</Badge>
              )}
              {workflowState?.leaguesSynced && !workflowState?.leaguesConfirmed && (
                <Badge variant="outline">Synced</Badge>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {!workflowState?.countriesConfirmed && (
            <p className="text-sm text-muted-foreground">
              Complete Step 1 first (confirm countries)
            </p>
          )}

          {workflowState?.countriesConfirmed && !workflowState?.leaguesSynced && (
            <div>
              <Button
                onClick={() => syncLeagues.mutate()}
                disabled={syncLeagues.isPending}
              >
                {syncLeagues.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Sync Leagues
              </Button>
              {syncLeagues.error && (
                <Alert variant="destructive" className="mt-4">
                  <AlertDescription>{syncLeagues.error.message}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {workflowState?.leaguesSynced && !workflowState?.leaguesConfirmed && (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Select leagues you want to track ({selectedLeagues.size} selected):
              </p>
              <div className="space-y-4 max-h-96 overflow-y-auto">
                {Object.entries(
                  leagues.reduce<Record<string, typeof leagues>>((acc, league) => {
                    const countryName = league.country?.name || "Unknown";
                    if (!acc[countryName]) acc[countryName] = [];
                    acc[countryName].push(league);
                    return acc;
                  }, {})
                )
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([countryName, countryLeagues]) => (
                    <div key={countryName} className="space-y-2">
                      <h3 className="font-semibold text-sm text-muted-foreground flex items-center gap-2">
                        {countryLeagues[0].country?.flagEmoji} {countryName}
                        <span className="text-xs">({countryLeagues.length})</span>
                      </h3>
                      <div className="space-y-1 pl-4">
                        {countryLeagues.map((league) => (
                          <Button
                            key={league.id}
                            variant={selectedLeagues.has(league.id) ? "default" : "outline"}
                            size="sm"
                            onClick={() => toggleLeague(league.id)}
                            className="w-full justify-start"
                          >
                            {league.displayName}
                          </Button>
                        ))}
                      </div>
                    </div>
                  ))}
              </div>
              <Button
                onClick={() => confirmLeagues.mutate()}
                disabled={confirmLeagues.isPending || selectedLeagues.size === 0}
              >
                {confirmLeagues.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Confirm Leagues ({selectedLeagues.size})
              </Button>
              {confirmLeagues.error && (
                <Alert variant="destructive">
                  <AlertDescription>{confirmLeagues.error.message}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {workflowState?.leaguesConfirmed && (
            <p className="text-sm text-green-600">
              ✓ Ligy potvrzeny. {leagues.filter((l) => l.isSyncEnabled).length} aktivních / {leagues.length} celkem.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Step 3: Seasons */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {workflowState?.seasonsSynced ? (
                <CheckCircle2 className="h-6 w-6 text-green-600" />
              ) : (
                <Circle className="h-6 w-6 text-gray-400" />
              )}
              <div>
                <CardTitle>Step 3: Seasons</CardTitle>
                <CardDescription>
                  Synchronize seasons (last 3 years)
                </CardDescription>
              </div>
            </div>
            {workflowState?.seasonsSynced && (
              <Badge variant="secondary">Completed</Badge>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {!workflowState?.leaguesConfirmed && (
            <p className="text-sm text-muted-foreground">
              Complete Step 2 first (confirm leagues)
            </p>
          )}

          {workflowState?.leaguesConfirmed && !workflowState?.seasonsSynced && (
            <div>
              <Button
                onClick={() => syncSeasons.mutate()}
                disabled={syncSeasons.isPending}
              >
                {syncSeasons.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Sync Seasons
              </Button>
              {syncSeasons.error && (
                <Alert variant="destructive" className="mt-4">
                  <AlertDescription>{syncSeasons.error.message}</AlertDescription>
                </Alert>
              )}
            </div>
          )}

          {workflowState?.seasonsSynced && (
            <p className="text-sm text-green-600">
              ✓ Seasons synchronized successfully!
            </p>
          )}
        </CardContent>
      </Card>

      {/* Step 4: Data Synchronization Modes */}
      {workflowState?.seasonsSynced && (
        <div className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold mb-2">Step 4: Data Synchronization</h2>
            <p className="text-muted-foreground">
              Choose between historical data import or current season tracking
            </p>
          </div>

          {/* Mode A: Historical Data Import */}
          <Card className="border-blue-200 bg-blue-50/50">
            <CardHeader>
              <div className="flex items-center gap-2">
                <div className="p-2 bg-blue-100 rounded-lg">
                  <svg className="h-5 w-5 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div>
                  <CardTitle className="text-blue-900">Režim A: Historický import</CardTitle>
                  <CardDescription className="text-blue-700">
                    Jednorázový import kompletních dat z minulých sezón
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="bg-white p-4 rounded-lg border border-blue-100">
                <h4 className="font-semibold mb-2 text-sm">Co tento režim dělá:</h4>
                <ul className="text-sm space-y-1 text-muted-foreground list-disc list-inside">
                  <li>Importuje kompletní data z vybraných historických sezón</li>
                  <li>Ideální pro analýzu trendů a zpětné testování strategií</li>
                  <li>Importuje pouze finální výsledky (bez průběžných aktualizací)</li>
                  <li>Vhodné pro sezóny 2020-2021 až 2023-2024</li>
                </ul>
              </div>

              <Alert className="bg-blue-50 border-blue-200">
                <AlertCircle className="h-4 w-4 text-blue-600" />
                <AlertDescription className="text-blue-900">
                  Pro historický import použijte stránku <Link href="/import" className="font-semibold underline">Import</Link>,
                  kde můžete vybrat konkrétní ligy a sezóny k importu.
                </AlertDescription>
              </Alert>

              <div className="flex gap-2">
                <Link href="/import">
                  <Button variant="default" className="bg-blue-600 hover:bg-blue-700">
                    Přejít na historický import
                  </Button>
                </Link>
              </div>
            </CardContent>
          </Card>

          {/* Mode B: Current Season Tracking */}
          <Card className="border-green-200 bg-green-50/50">
            <CardHeader>
              <div className="flex items-center gap-2">
                <div className="p-2 bg-green-100 rounded-lg">
                  <svg className="h-5 w-5 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                  </svg>
                </div>
                <div>
                  <CardTitle className="text-green-900">Režim B: Sledování aktuálních sezón</CardTitle>
                  <CardDescription className="text-green-700">
                    Průběžná synchronizace dat z probíhajících sezón
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="bg-white p-4 rounded-lg border border-green-100">
                <h4 className="font-semibold mb-2 text-sm">Co tento režim dělá:</h4>
                <ul className="text-sm space-y-1 text-muted-foreground list-disc list-inside">
                  <li>Automaticky detekuje aktuálně probíhající sezóny (např. 2024-2025)</li>
                  <li>Pravidelně synchronizuje nové výsledky zápasů</li>
                  <li>Ideální pro live analýzy a predikce nadcházejících zápasů</li>
                  <li>Data se aktualizují průběžně během sezóny</li>
                </ul>
              </div>

              <div className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  <Button
                    variant="outline"
                    onClick={() => detectCurrentSeasons.mutate()}
                    disabled={detectCurrentSeasons.isPending}
                    className="border-green-300 hover:bg-green-50"
                  >
                    {detectCurrentSeasons.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                    1. Detekovat aktuální sezóny
                  </Button>
                  <Button
                    variant="default"
                    onClick={() => syncAllMarkedData.mutate()}
                    disabled={syncAllMarkedData.isPending}
                    className="bg-green-600 hover:bg-green-700"
                  >
                    {syncAllMarkedData.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                    2. Synchronizovat data aktuálních sezón
                  </Button>
                </div>

                {detectCurrentSeasons.isSuccess && (
                  <Alert className="bg-green-50 border-green-200">
                    <CheckCircle2 className="h-4 w-4 text-green-600" />
                    <AlertDescription className="text-green-900">
                      Aktuální sezóny detekovány úspěšně! Nyní můžete synchronizovat jejich data.
                    </AlertDescription>
                  </Alert>
                )}

                {detectCurrentSeasons.error && (
                  <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                      {detectCurrentSeasons.error.message}
                    </AlertDescription>
                  </Alert>
                )}

                {syncAllMarkedData.isSuccess && syncAllMarkedData.data && (
                  <Alert className="bg-green-50 border-green-200">
                    <CheckCircle2 className="h-4 w-4 text-green-600" />
                    <AlertDescription className="text-green-900">
                      {syncAllMarkedData.data.message}
                      <div className="mt-2 text-xs">
                        Vytvořeno: {syncAllMarkedData.data.statistics.created} |
                        Aktualizováno: {syncAllMarkedData.data.statistics.updated} |
                        Přeskočeno: {syncAllMarkedData.data.statistics.skipped}
                      </div>
                    </AlertDescription>
                  </Alert>
                )}

                {syncAllMarkedData.error && (
                  <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                      {syncAllMarkedData.error.message}
                    </AlertDescription>
                  </Alert>
                )}
              </div>

              <p className="text-sm text-muted-foreground">
                Spravujte synchronizaci sezón na stránce <Link href="/leagues" className="text-green-600 font-semibold hover:underline">Ligy</Link>.
              </p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Summary */}
      {workflowState?.seasonsSynced && (
        <Alert>
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription>
            Synchronization workflow completed! You can now import match data.
          </AlertDescription>
        </Alert>
      )}
    </div>
  );
}
