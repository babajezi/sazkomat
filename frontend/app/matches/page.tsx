"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { configApi } from "@/lib/api/client";
import type { Match, MatchFilter } from "@/lib/api/types";
import { MatchResult, MatchSortBy } from "@/lib/api/types";
import { getLeagueDisplayName } from "@/lib/utils/league";
import { useLanguage } from "@/contexts/UserContext";
import Link from "next/link";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export default function MatchesPage() {
  const { language } = useLanguage();
  const [viewMode, setViewMode] = useState<"chronological" | "grouped">("grouped");
  const [filters, setFilters] = useState<MatchFilter>({
    take: 50,
    sortBy: MatchSortBy.Round,
    sortDescending: true,
  });

  // Fetch leagues for filter
  const { data: leagues } = useQuery({
    queryKey: ["leagues"],
    queryFn: () => configApi.getLeagues(),
  });

  // Fetch imported seasons for filter
  const { data: importedSeasons } = useQuery({
    queryKey: ["importedSeasons"],
    queryFn: async () => {
      const response = await fetch(`${API_URL}/api/import/seasons/imported`);
      if (!response.ok) throw new Error("Failed to fetch seasons");
      return response.json();
    },
  });

  // Fetch available rounds for filter (depends on selected league and season)
  const { data: availableRounds } = useQuery({
    queryKey: ["availableRounds", filters.leagueId, filters.season],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filters.leagueId) params.append("leagueId", filters.leagueId);
      if (filters.season) params.append("season", filters.season);
      const response = await fetch(`${API_URL}/api/import/rounds/available?${params}`);
      if (!response.ok) throw new Error("Failed to fetch rounds");
      return response.json() as Promise<number[]>;
    },
  });

  // Fetch matches
  const { data: matchesResponse, isLoading, error } = useQuery({
    queryKey: ["matches", filters],
    queryFn: async () => {
      const params = new URLSearchParams();

      if (filters.leagueId) params.append("leagueId", filters.leagueId);
      if (filters.season) params.append("season", filters.season);
      if (filters.roundNumber) params.append("roundNumber", filters.roundNumber.toString());
      if (filters.result) params.append("result", filters.result);
      if (filters.teamName) params.append("teamName", filters.teamName);
      if (filters.skip) params.append("skip", filters.skip.toString());
      if (filters.take) params.append("take", filters.take.toString());
      if (filters.sortBy) params.append("sortBy", filters.sortBy);
      if (filters.sortDescending !== undefined) params.append("sortDescending", filters.sortDescending.toString());

      const response = await fetch(`${API_URL}/api/import/matches?${params}`);
      if (!response.ok) throw new Error("Failed to fetch matches");
      return response.json();
    },
  });

  const handleFilterChange = (key: keyof MatchFilter, value: any) => {
    setFilters((prev) => ({ ...prev, [key]: value, skip: 0 }));
  };

  const resetFilters = () => {
    setFilters({
      take: 50,
      sortBy: MatchSortBy.Round,
      sortDescending: true,
    });
  };

  // Helper function to highlight winning odds
  const getWinningOddsClass = (result: MatchResult, type: MatchResult) => {
    if (result === type) {
      return "bg-green-100 text-green-800 font-bold";
    }
    return "bg-gray-50";
  };

  // Get seasons from imported seasons endpoint (all available seasons)
  const availableSeasons: { name: string; roundsCount: number; matchesCount: number }[] =
    importedSeasons || [];

  // Get round numbers from dedicated endpoint
  const roundNumbers: number[] = availableRounds || [];

  // Group matches by round
  const groupedMatches = matchesResponse?.matches?.reduce((acc: Record<string, Match[]>, match: Match) => {
    const key = `${match.round.season}-${match.round.roundNumber}`;
    if (!acc[key]) acc[key] = [];
    acc[key].push(match);
    return acc;
  }, {});

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-destructive">Chyba</CardTitle>
          </CardHeader>
          <CardContent>
            <p>Nelze načíst data: {(error as Error).message}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const matches = matchesResponse?.matches || [];
  const totalCount = matchesResponse?.totalCount || 0;

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Importované zápasy</h1>
          <p className="text-muted-foreground">
            Celkem {totalCount} zápasů
          </p>
        </div>
        <Link href="/">
          <Button variant="outline">← Zpět na hlavní stránku</Button>
        </Link>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filtry</CardTitle>
          <CardDescription>Filtrujte zápasy podle různých kritérií</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* League Filter */}
            <div>
              <Label htmlFor="league">Liga</Label>
              <Select
                value={filters.leagueId || "all"}
                onValueChange={(value) =>
                  handleFilterChange("leagueId", value === "all" ? undefined : value)
                }
              >
                <SelectTrigger id="league">
                  <SelectValue placeholder="Všechny ligy" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Všechny ligy</SelectItem>
                  {leagues?.map((league) => (
                    <SelectItem key={league.id} value={league.id}>
                      {getLeagueDisplayName(league, language)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Season Filter */}
            <div>
              <Label htmlFor="season">Sezóna</Label>
              <Select
                value={filters.season || "all"}
                onValueChange={(value) =>
                  handleFilterChange("season", value === "all" ? undefined : value)
                }
              >
                <SelectTrigger id="season">
                  <SelectValue placeholder="Všechny sezóny" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Všechny sezóny</SelectItem>
                  {availableSeasons.map((season) => (
                    <SelectItem key={season.name} value={season.name}>
                      {season.name} ({season.roundsCount} kol, {season.matchesCount} zápasů)
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Round Filter */}
            <div>
              <Label htmlFor="round">Kolo</Label>
              <Select
                value={filters.roundNumber?.toString() || "all"}
                onValueChange={(value) =>
                  handleFilterChange("roundNumber", value === "all" ? undefined : parseInt(value))
                }
              >
                <SelectTrigger id="round">
                  <SelectValue placeholder="Všechna kola" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Všechna kola</SelectItem>
                  {roundNumbers.map((round) => (
                    <SelectItem key={round} value={round.toString()}>
                      Kolo {round}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Result Filter */}
            <div>
              <Label htmlFor="result">Výsledek</Label>
              <Select
                value={filters.result || "all"}
                onValueChange={(value) =>
                  handleFilterChange("result", value === "all" ? undefined : value)
                }
              >
                <SelectTrigger id="result">
                  <SelectValue placeholder="Všechny výsledky" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Všechny výsledky</SelectItem>
                  <SelectItem value={MatchResult.Home}>Domácí výhra</SelectItem>
                  <SelectItem value={MatchResult.Draw}>Remíza</SelectItem>
                  <SelectItem value={MatchResult.Away}>Venkovní výhra</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Team Name Search */}
            <div>
              <Label htmlFor="teamName">Tým</Label>
              <Input
                id="teamName"
                placeholder="Hledat tým..."
                value={filters.teamName || ""}
                onChange={(e) => handleFilterChange("teamName", e.target.value || undefined)}
              />
            </div>
          </div>

          <div className="flex items-center gap-4">
            <Button onClick={resetFilters} variant="outline">
              Resetovat filtry
            </Button>

            {/* View Mode Toggle */}
            <div className="flex items-center gap-2 ml-auto">
              <Label>Zobrazení:</Label>
              <Button
                variant={viewMode === "chronological" ? "default" : "outline"}
                size="sm"
                onClick={() => setViewMode("chronological")}
              >
                Chronologicky
              </Button>
              <Button
                variant={viewMode === "grouped" ? "default" : "outline"}
                size="sm"
                onClick={() => setViewMode("grouped")}
              >
                Podle kol
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Matches Display */}
      {viewMode === "chronological" ? (
        /* Chronological View */
        <Card>
          <CardHeader>
            <CardTitle>Zápasy ({matches.length})</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {matches.map((match: Match) => (
                <div
                  key={match.id}
                  className="grid grid-cols-[1fr_auto_auto] gap-4 items-center p-4 border rounded-lg hover:bg-muted/50"
                >
                  <div>
                    <div className="font-medium">
                      {match.homeTeam} vs {match.awayTeam}
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {match.league?.displayName || match.league?.name} • {match.round.season} • Kolo {match.round.roundNumber}
                    </div>
                    <div className="text-xs text-muted-foreground mt-1">
                      {match.matchDate
                        ? new Date(match.matchDate).toLocaleDateString("cs-CZ")
                        : "Datum neuvedeno"}
                    </div>
                  </div>
                  <div className="text-center w-24">
                    <div className="text-2xl font-bold">
                      {match.homeScore}:{match.awayScore}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {match.result === MatchResult.Home ? "Domácí" : match.result === MatchResult.Draw ? "Remíza" : "Venkovní"}
                    </div>
                  </div>
                  <div className="w-48">
                    {(match.homeOdds || match.drawOdds || match.awayOdds) ? (
                      <div className="flex gap-1 justify-end font-mono text-sm">
                        <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Home)}`}>
                          {match.homeOdds?.toFixed(2) || "-"}
                        </span>
                        <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Draw)}`}>
                          {match.drawOdds?.toFixed(2) || "-"}
                        </span>
                        <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Away)}`}>
                          {match.awayOdds?.toFixed(2) || "-"}
                        </span>
                      </div>
                    ) : (
                      <div className="text-sm text-muted-foreground text-right">Bez kurzů</div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ) : (
        /* Grouped by Round View */
        <div className="space-y-4">
          {groupedMatches && Object.entries(groupedMatches)
            .sort((a, b) => b[0].localeCompare(a[0]))
            .map(([key, roundMatches]) => {
              const typedRoundMatches = roundMatches as Match[];
              const firstMatch = typedRoundMatches[0];
              const homeWins = typedRoundMatches.filter(m => m.result === MatchResult.Home).length;
              const draws = typedRoundMatches.filter(m => m.result === MatchResult.Draw).length;
              const awayWins = typedRoundMatches.filter(m => m.result === MatchResult.Away).length;
              const total = homeWins + draws + awayWins;
              return (
                <Card key={key}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div>
                        <CardTitle>
                          {firstMatch.league?.displayName || firstMatch.league?.name} - {firstMatch.round.season} - Kolo {firstMatch.round.roundNumber}
                        </CardTitle>
                        <CardDescription>
                          {typedRoundMatches.length} zápasů
                        </CardDescription>
                      </div>
                      {total > 0 && (
                        <div className="text-right">
                          <div className="text-sm text-muted-foreground mb-1">Výsledky (1-X-2)</div>
                          <div className="flex gap-1 font-mono text-xl font-bold">
                            <span className={`min-w-[60px] text-center px-2 py-1 rounded ${homeWins > 0 && draws === 0 && awayWins === 0 ? "bg-green-100 text-green-800" : "bg-gray-100"}`}>
                              {homeWins}
                            </span>
                            <span className={`min-w-[60px] text-center px-2 py-1 rounded ${draws > 0 && homeWins === 0 && awayWins === 0 ? "bg-green-100 text-green-800" : "bg-gray-100"}`}>
                              {draws}
                            </span>
                            <span className={`min-w-[60px] text-center px-2 py-1 rounded ${awayWins > 0 && homeWins === 0 && draws === 0 ? "bg-green-100 text-green-800" : "bg-gray-100"}`}>
                              {awayWins}
                            </span>
                          </div>
                        </div>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {typedRoundMatches.map((match) => (
                        <div
                          key={match.id}
                          className="grid grid-cols-[1fr_auto_auto] gap-4 items-center p-3 border rounded hover:bg-muted/50"
                        >
                          <div>
                            <div className="font-medium">
                              {match.homeTeam} vs {match.awayTeam}
                            </div>
                            <div className="text-xs text-muted-foreground mt-1">
                              {match.matchDate
                                ? new Date(match.matchDate).toLocaleDateString("cs-CZ")
                                : "Datum neuvedeno"}
                            </div>
                          </div>
                          <div className="text-center w-24">
                            <div className="text-xl font-bold">
                              {match.homeScore}:{match.awayScore}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              {match.result === MatchResult.Home ? "Domácí" : match.result === MatchResult.Draw ? "Remíza" : "Venkovní"}
                            </div>
                          </div>
                          <div className="w-48">
                            {(match.homeOdds || match.drawOdds || match.awayOdds) ? (
                              <div className="flex gap-1 justify-end font-mono text-sm">
                                <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Home)}`}>
                                  {match.homeOdds?.toFixed(2) || "-"}
                                </span>
                                <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Draw)}`}>
                                  {match.drawOdds?.toFixed(2) || "-"}
                                </span>
                                <span className={`min-w-[60px] text-right px-2 py-1 rounded ${getWinningOddsClass(match.result, MatchResult.Away)}`}>
                                  {match.awayOdds?.toFixed(2) || "-"}
                                </span>
                              </div>
                            ) : (
                              <div className="text-sm text-muted-foreground text-right">Bez kurzů</div>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              );
            })}
        </div>
      )}

      {/* Pagination Info */}
      {totalCount > (filters.take || 50) && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Zobrazeno {matches.length} z {totalCount} zápasů
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  disabled={(filters.skip || 0) === 0}
                  onClick={() => handleFilterChange("skip", Math.max(0, (filters.skip || 0) - (filters.take || 50)))}
                >
                  Předchozí
                </Button>
                <Button
                  variant="outline"
                  disabled={(filters.skip || 0) + matches.length >= totalCount}
                  onClick={() => handleFilterChange("skip", (filters.skip || 0) + (filters.take || 50))}
                >
                  Další
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
