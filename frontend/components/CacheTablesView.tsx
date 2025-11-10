"use client";

import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Loader2, Download, CheckCircle2, AlertCircle, Database, Trash2 } from "lucide-react";
import type { ProviderCountry, ProviderLeague, ProviderSeason } from "@/lib/api/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";
const BET_EXPLORER_PROVIDER_ID = "a0000000-0000-0000-0000-000000000001";

interface CacheTablesViewProps {
  providerId?: string;
}

export function CacheTablesView({ providerId = BET_EXPLORER_PROVIDER_ID }: CacheTablesViewProps) {
  const queryClient = useQueryClient();
  const [selectedCountries, setSelectedCountries] = useState<Set<string>>(new Set());
  const [selectedLeagues, setSelectedLeagues] = useState<Set<string>>(new Set());
  const [selectedSeasons, setSelectedSeasons] = useState<Set<string>>(new Set());

  // Fetch cached countries
  const { data: rawCountries = [], isLoading: loadingCountries } = useQuery<ProviderCountry[]>({
    queryKey: ["provider-countries", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/provider-cache/countries?providerId=${providerId}`);
      if (!res.ok) {
        if (res.status === 404) return [];
        throw new Error("Failed to fetch cached countries");
      }
      return res.json();
    },
  });

  // Fetch cached leagues
  const { data: rawLeagues = [], isLoading: loadingLeagues } = useQuery<ProviderLeague[]>({
    queryKey: ["provider-leagues", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/provider-cache/leagues?providerId=${providerId}`);
      if (!res.ok) {
        if (res.status === 404) return [];
        throw new Error("Failed to fetch cached leagues");
      }
      return res.json();
    },
  });

  // Fetch cached seasons
  const { data: rawSeasons = [], isLoading: loadingSeasons } = useQuery<ProviderSeason[]>({
    queryKey: ["provider-seasons", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/provider-cache/seasons?providerId=${providerId}`);
      if (!res.ok) {
        if (res.status === 404) return [];
        throw new Error("Failed to fetch cached seasons");
      }
      return res.json();
    },
  });

  // Sort data alphabetically
  const cachedCountries = [...rawCountries].sort((a, b) =>
    a.providerName.localeCompare(b.providerName, "cs")
  );

  // Group leagues by country code, then sort alphabetically within each group
  const leaguesByCountry = rawLeagues.reduce((acc, league) => {
    const countryCode = league.countryCode || "unknown";
    if (!acc[countryCode]) {
      acc[countryCode] = [];
    }
    acc[countryCode].push(league);
    return acc;
  }, {} as Record<string, typeof rawLeagues>);

  // Sort countries alphabetically and sort leagues within each country
  const sortedCountryCodes = Object.keys(leaguesByCountry).sort((a, b) =>
    a.localeCompare(b, "cs")
  );

  sortedCountryCodes.forEach((countryCode) => {
    leaguesByCountry[countryCode].sort((a, b) =>
      (a.displayName || a.providerName).localeCompare(
        b.displayName || b.providerName,
        "cs"
      )
    );
  });

  const cachedLeagues = rawLeagues.sort((a, b) =>
    (a.displayName || a.providerName).localeCompare(b.displayName || b.providerName, "cs")
  );

  const cachedSeasons = [...rawSeasons].sort((a, b) =>
    a.seasonName.localeCompare(b.seasonName)
  );

  // Import mutation
  const importMutation = useMutation({
    mutationFn: async (entityType: "countries" | "leagues" | "seasons") => {
      let endpoint = "";
      let body: any = { providerId };

      switch (entityType) {
        case "countries":
          endpoint = "/api/import/countries";
          body.providerCountryIds = Array.from(selectedCountries);
          break;
        case "leagues":
          endpoint = "/api/import/leagues";
          body.providerLeagueIds = Array.from(selectedLeagues);
          break;
        case "seasons":
          endpoint = "/api/import/seasons";
          body.providerSeasonIds = Array.from(selectedSeasons);
          break;
      }

      const res = await fetch(`${API_URL}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Import failed");
      }

      return res.json();
    },
    onSuccess: (_, entityType) => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      queryClient.invalidateQueries({ queryKey: ["seasons"] });

      // Clear selections
      switch (entityType) {
        case "countries":
          setSelectedCountries(new Set());
          break;
        case "leagues":
          setSelectedLeagues(new Set());
          break;
        case "seasons":
          setSelectedSeasons(new Set());
          break;
      }
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: async (entityType: "countries" | "leagues" | "seasons") => {
      let ids: string[] = [];
      let endpoint = "";

      switch (entityType) {
        case "countries":
          ids = Array.from(selectedCountries);
          endpoint = "/api/provider-cache/countries";
          break;
        case "leagues":
          ids = Array.from(selectedLeagues);
          endpoint = "/api/provider-cache/leagues";
          break;
        case "seasons":
          ids = Array.from(selectedSeasons);
          endpoint = "/api/provider-cache/seasons";
          break;
      }

      const res = await fetch(`${API_URL}${endpoint}`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ids }),
      });

      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Delete failed");
      }

      return res.json();
    },
    onSuccess: (_, entityType) => {
      queryClient.invalidateQueries({ queryKey: ["provider-countries", providerId] });
      queryClient.invalidateQueries({ queryKey: ["provider-leagues", providerId] });
      queryClient.invalidateQueries({ queryKey: ["provider-seasons", providerId] });

      // Clear selections
      switch (entityType) {
        case "countries":
          setSelectedCountries(new Set());
          break;
        case "leagues":
          setSelectedLeagues(new Set());
          break;
        case "seasons":
          setSelectedSeasons(new Set());
          break;
      }
    },
  });

  const toggleSelection = (
    id: string,
    type: "countries" | "leagues" | "seasons"
  ) => {
    let setter;
    let currentSet;

    switch (type) {
      case "countries":
        setter = setSelectedCountries;
        currentSet = selectedCountries;
        break;
      case "leagues":
        setter = setSelectedLeagues;
        currentSet = selectedLeagues;
        break;
      case "seasons":
        setter = setSelectedSeasons;
        currentSet = selectedSeasons;
        break;
    }

    const newSet = new Set(currentSet);
    if (newSet.has(id)) {
      newSet.delete(id);
    } else {
      newSet.add(id);
    }
    setter(newSet);
  };

  const toggleSelectAll = (
    type: "countries" | "leagues" | "seasons"
  ) => {
    let setter;
    let currentSet;
    let allIds: string[];

    switch (type) {
      case "countries":
        setter = setSelectedCountries;
        currentSet = selectedCountries;
        allIds = cachedCountries.map((c) => c.id);
        break;
      case "leagues":
        setter = setSelectedLeagues;
        currentSet = selectedLeagues;
        allIds = cachedLeagues.map((l) => l.id);
        break;
      case "seasons":
        setter = setSelectedSeasons;
        currentSet = selectedSeasons;
        allIds = cachedSeasons.map((s) => s.id);
        break;
    }

    // If all are selected, deselect all; otherwise select all
    if (currentSet.size === allIds.length) {
      setter(new Set());
    } else {
      setter(new Set(allIds));
    }
  };

  const toggleCountryLeagues = (countryCode: string) => {
    const countryLeagues = leaguesByCountry[countryCode] || [];
    const countryLeagueIds = countryLeagues.map((l) => l.id);
    const newSet = new Set(selectedLeagues);

    // Check if all leagues from this country are selected
    const allSelected = countryLeagueIds.every((id) => newSet.has(id));

    if (allSelected) {
      // Deselect all leagues from this country
      countryLeagueIds.forEach((id) => newSet.delete(id));
    } else {
      // Select all leagues from this country
      countryLeagueIds.forEach((id) => newSet.add(id));
    }

    setSelectedLeagues(newSet);
  };

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString("cs-CZ", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Database className="h-5 w-5" />
          <div>
            <CardTitle>Provider Cache</CardTitle>
            <CardDescription>
              Náhled dat načtených z providera před importem do hlavní databáze
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="countries">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="countries">
              Země ({cachedCountries.length})
            </TabsTrigger>
            <TabsTrigger value="leagues">
              Ligy ({cachedLeagues.length})
            </TabsTrigger>
            <TabsTrigger value="seasons">
              Sezóny ({cachedSeasons.length})
            </TabsTrigger>
          </TabsList>

          <TabsContent value="countries" className="space-y-4">
            {loadingCountries ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin" />
              </div>
            ) : cachedCountries.length === 0 ? (
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Žádná cachovaná data. Spusťte scan zemí.
                </AlertDescription>
              </Alert>
            ) : (
              <>
                <div className="flex items-center justify-between">
                  <p className="text-sm text-muted-foreground">
                    {selectedCountries.size} vybraných
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => deleteMutation.mutate("countries")}
                      disabled={
                        selectedCountries.size === 0 || deleteMutation.isPending
                      }
                    >
                      {deleteMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Trash2 className="mr-2 h-4 w-4" />
                      Smazat vybrané
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => importMutation.mutate("countries")}
                      disabled={
                        selectedCountries.size === 0 || importMutation.isPending
                      }
                    >
                      {importMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Download className="mr-2 h-4 w-4" />
                      Importovat vybrané
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-12">
                          <input
                            type="checkbox"
                            checked={selectedCountries.size === cachedCountries.length && cachedCountries.length > 0}
                            onChange={() => toggleSelectAll("countries")}
                            className="h-4 w-4"
                            title="Vybrat vše"
                          />
                        </TableHead>
                        <TableHead>Kód</TableHead>
                        <TableHead>Název</TableHead>
                        <TableHead>Skenováno</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {cachedCountries.map((country) => (
                        <TableRow key={country.id}>
                          <TableCell>
                            <input
                              type="checkbox"
                              checked={selectedCountries.has(country.id)}
                              onChange={() =>
                                toggleSelection(country.id, "countries")
                              }
                              className="h-4 w-4"
                            />
                          </TableCell>
                          <TableCell className="font-medium">
                            {country.providerCode}
                          </TableCell>
                          <TableCell>{country.providerName}</TableCell>
                          <TableCell className="text-sm text-muted-foreground">
                            {formatTimestamp(country.scannedAt)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </>
            )}
          </TabsContent>

          <TabsContent value="leagues" className="space-y-4">
            {loadingLeagues ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin" />
              </div>
            ) : cachedLeagues.length === 0 ? (
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Žádná cachovaná data. Spusťte scan lig.
                </AlertDescription>
              </Alert>
            ) : (
              <>
                <div className="flex items-center justify-between">
                  <p className="text-sm text-muted-foreground">
                    {selectedLeagues.size} vybraných
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => deleteMutation.mutate("leagues")}
                      disabled={
                        selectedLeagues.size === 0 || deleteMutation.isPending
                      }
                    >
                      {deleteMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Trash2 className="mr-2 h-4 w-4" />
                      Smazat vybrané
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => importMutation.mutate("leagues")}
                      disabled={
                        selectedLeagues.size === 0 || importMutation.isPending
                      }
                    >
                      {importMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Download className="mr-2 h-4 w-4" />
                      Importovat vybrané
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-12">
                          <input
                            type="checkbox"
                            checked={selectedLeagues.size === cachedLeagues.length && cachedLeagues.length > 0}
                            onChange={() => toggleSelectAll("leagues")}
                            className="h-4 w-4"
                            title="Vybrat vše"
                          />
                        </TableHead>
                        <TableHead>Země</TableHead>
                        <TableHead>Název</TableHead>
                        <TableHead>Sport</TableHead>
                        <TableHead>Skenováno</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {sortedCountryCodes.map((countryCode) => {
                        const countryLeagues = leaguesByCountry[countryCode];
                        const countryLeagueIds = countryLeagues.map((l) => l.id);
                        const allCountryLeaguesSelected = countryLeagueIds.every((id) =>
                          selectedLeagues.has(id)
                        );

                        return (
                          <React.Fragment key={countryCode}>
                            {/* Country header row */}
                            <TableRow className="bg-muted/50 hover:bg-muted/70">
                              <TableCell>
                                <input
                                  type="checkbox"
                                  checked={allCountryLeaguesSelected && countryLeagueIds.length > 0}
                                  onChange={() => toggleCountryLeagues(countryCode)}
                                  className="h-4 w-4"
                                  title={`Vybrat všechny ligy ${countryCode}`}
                                />
                              </TableCell>
                              <TableCell colSpan={4} className="font-semibold">
                                <div className="flex items-center gap-2">
                                  <Badge variant="outline">{countryCode.toUpperCase()}</Badge>
                                  <span className="text-sm text-muted-foreground">
                                    ({countryLeagues.length} {countryLeagues.length === 1 ? "liga" : "lig"})
                                  </span>
                                </div>
                              </TableCell>
                            </TableRow>
                            {/* League rows for this country */}
                            {countryLeagues.map((league) => (
                              <TableRow key={league.id}>
                                <TableCell className="pl-8">
                                  <input
                                    type="checkbox"
                                    checked={selectedLeagues.has(league.id)}
                                    onChange={() =>
                                      toggleSelection(league.id, "leagues")
                                    }
                                    className="h-4 w-4"
                                  />
                                </TableCell>
                                <TableCell>
                                  <Badge variant="outline">{league.countryCode}</Badge>
                                </TableCell>
                                <TableCell className="font-medium">
                                  {league.displayName || league.providerName}
                                </TableCell>
                                <TableCell>
                                  <Badge variant="secondary">{league.sportCode}</Badge>
                                </TableCell>
                                <TableCell className="text-sm text-muted-foreground">
                                  {formatTimestamp(league.scannedAt)}
                                </TableCell>
                              </TableRow>
                            ))}
                          </React.Fragment>
                        );
                      })}
                    </TableBody>
                  </Table>
                </div>
              </>
            )}
          </TabsContent>

          <TabsContent value="seasons" className="space-y-4">
            {loadingSeasons ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin" />
              </div>
            ) : cachedSeasons.length === 0 ? (
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Žádná cachovaná data. Spusťte scan sezón.
                </AlertDescription>
              </Alert>
            ) : (
              <>
                <div className="flex items-center justify-between">
                  <p className="text-sm text-muted-foreground">
                    {selectedSeasons.size} vybraných
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => deleteMutation.mutate("seasons")}
                      disabled={
                        selectedSeasons.size === 0 || deleteMutation.isPending
                      }
                    >
                      {deleteMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Trash2 className="mr-2 h-4 w-4" />
                      Smazat vybrané
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => importMutation.mutate("seasons")}
                      disabled={
                        selectedSeasons.size === 0 || importMutation.isPending
                      }
                    >
                      {importMutation.isPending && (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      )}
                      <Download className="mr-2 h-4 w-4" />
                      Importovat vybrané
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-12">
                          <input
                            type="checkbox"
                            checked={selectedSeasons.size === cachedSeasons.length && cachedSeasons.length > 0}
                            onChange={() => toggleSelectAll("seasons")}
                            className="h-4 w-4"
                            title="Vybrat vše"
                          />
                        </TableHead>
                        <TableHead>Liga</TableHead>
                        <TableHead>Sezóna</TableHead>
                        <TableHead>Skenováno</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {cachedSeasons.map((season) => (
                        <TableRow key={season.id}>
                          <TableCell>
                            <input
                              type="checkbox"
                              checked={selectedSeasons.has(season.id)}
                              onChange={() =>
                                toggleSelection(season.id, "seasons")
                              }
                              className="h-4 w-4"
                            />
                          </TableCell>
                          <TableCell className="font-medium">
                            {season.providerLeagueSlug}
                          </TableCell>
                          <TableCell>
                            <Badge>{season.seasonName}</Badge>
                          </TableCell>
                          <TableCell className="text-sm text-muted-foreground">
                            {formatTimestamp(season.scannedAt)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </>
            )}
          </TabsContent>
        </Tabs>

        {importMutation.isSuccess && (
          <Alert className="mt-4 bg-green-50 border-green-200">
            <CheckCircle2 className="h-4 w-4 text-green-600" />
            <AlertDescription className="text-green-900">
              Import úspěšně dokončen!
            </AlertDescription>
          </Alert>
        )}

        {importMutation.isError && (
          <Alert variant="destructive" className="mt-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{importMutation.error.message}</AlertDescription>
          </Alert>
        )}
      </CardContent>
    </Card>
  );
}
