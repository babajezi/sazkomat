"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { importApi, configApi, seasonApi } from "@/lib/api/client";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ChevronDown, ChevronRight, Sparkles, X } from "lucide-react";
import Link from "next/link";
import type { Round } from "@/lib/api/types";

export default function RoundsPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [expandedRounds, setExpandedRounds] = useState<Set<string>>(new Set());

  // Cascade filters: Country → League → Season
  const [selectedCountryId, setSelectedCountryId] = useState<string>("");
  const [selectedLeagueId, setSelectedLeagueId] = useState<string>("");
  const [selectedSeason, setSelectedSeason] = useState<string>("");

  // Load all leagues for filter
  const { data: allLeagues } = useQuery({
    queryKey: ["leagues"],
    queryFn: () => configApi.getLeagues(),
  });

  // Get unique countries from leagues
  const countries = allLeagues
    ? Array.from(
        new Map(
          allLeagues
            .filter((l) => l.country)
            .map((l) => [l.country!.id, l.country!])
        ).values()
      ).sort((a, b) => a.name.localeCompare(b.name))
    : [];

  // Filter leagues by selected country
  const filteredLeagues = selectedCountryId
    ? allLeagues?.filter((l) => l.country?.id === selectedCountryId) || []
    : allLeagues || [];

  // Load seasons for selected league
  const { data: leagueSeasons } = useQuery({
    queryKey: ["league-seasons", selectedLeagueId],
    queryFn: () => seasonApi.getLeagueSeasons(selectedLeagueId),
    enabled: !!selectedLeagueId,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["rounds", page, pageSize, selectedLeagueId, selectedSeason],
    queryFn: () =>
      importApi.getRounds({
        skip: page * pageSize,
        take: pageSize,
        sortDescending: true,
        leagueId: selectedLeagueId || undefined,
        season: selectedSeason || undefined,
      }),
  });

  const totalCount = data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  // Get available seasons for filter dropdown
  // If league is selected, use seasons from API; otherwise use seasons from current data
  const availableSeasons = selectedLeagueId && leagueSeasons
    ? leagueSeasons.map((ls) => ls.seasonName).sort((a, b) => b.localeCompare(a))
    : Array.from(new Set(data?.rounds.map((r) => r.season) || [])).sort((a, b) => b.localeCompare(a));

  const toggleRound = (roundId: string) => {
    setExpandedRounds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(roundId)) {
        newSet.delete(roundId);
      } else {
        newSet.add(roundId);
      }
      return newSet;
    });
  };

  const resetFilters = () => {
    setSelectedCountryId("");
    setSelectedLeagueId("");
    setSelectedSeason("");
    setPage(0);
  };

  const handleCountryChange = (countryId: string) => {
    setSelectedCountryId(countryId);
    setSelectedLeagueId(""); // Reset league when country changes
    setSelectedSeason(""); // Reset season
    setPage(0);
  };

  const handleLeagueChange = (leagueId: string) => {
    setSelectedLeagueId(leagueId);
    setSelectedSeason(""); // Reset season when league changes
    setPage(0);
  };

  const getWinningOddsClass = (result: string, type: "H" | "D" | "A") => {
    if (result === type) {
      return "bg-green-100 text-green-800 font-bold";
    }
    return "";
  };

  // Helper functions to detect special rounds
  const isAllHomeWins = (round: Round) =>
    round.homeWins === round.matchesCount && round.homeWins > 0;
  const isAllDraws = (round: Round) =>
    round.draws === round.matchesCount && round.draws > 0;
  const isAllAwayWins = (round: Round) =>
    round.awayWins === round.matchesCount && round.awayWins > 0;
  const isSpecialRound = (round: Round) =>
    isAllHomeWins(round) || isAllDraws(round) || isAllAwayWins(round);

  const getSpecialRoundInfo = (round: Round) => {
    if (isAllHomeWins(round))
      return { type: "Všechny domácí", color: "border-green-500 bg-green-50" };
    if (isAllDraws(round))
      return { type: "Všechny remízy", color: "border-yellow-500 bg-yellow-50" };
    if (isAllAwayWins(round))
      return { type: "Všechny hosté", color: "border-red-500 bg-red-50" };
    return null;
  };

  // Format summary result: "2-3-5" → "2 3 5"
  const formatSummaryResult = (summaryResult: string) => {
    return summaryResult.replace(/-/g, " ");
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-4xl font-bold mb-2 bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
              Přehled kol
            </h1>
            <p className="text-gray-600 text-lg">
              {data?.totalCount || 0} kol celkem
            </p>
          </div>
          <Link href="/">
            <Button variant="outline" size="lg">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        {/* Filters */}
        <Card className="mb-6 shadow-md">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <span>🔍</span>
              Filtry
            </CardTitle>
            <CardDescription>
              Filtrujte kola podle ligy a sezóny
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex gap-4 items-end flex-wrap">
              <div className="flex-1 min-w-[200px]">
                <label className="text-sm font-medium block mb-2">Země</label>
                <select
                  value={selectedCountryId}
                  onChange={(e) => handleCountryChange(e.target.value)}
                  className="w-full border rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Všechny země</option>
                  {countries.map((country) => (
                    <option key={country.id} value={country.id}>
                      {country.flagEmoji} {country.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex-1 min-w-[200px]">
                <label className="text-sm font-medium block mb-2">Liga</label>
                <select
                  value={selectedLeagueId}
                  onChange={(e) => handleLeagueChange(e.target.value)}
                  className="w-full border rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  disabled={!!(selectedCountryId && filteredLeagues.length === 0)}
                >
                  <option value="">Všechny ligy</option>
                  {filteredLeagues.map((league) => (
                    <option key={league.id} value={league.id}>
                      {league.displayName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex-1 min-w-[200px]">
                <label className="text-sm font-medium block mb-2">Sezóna</label>
                <select
                  value={selectedSeason}
                  onChange={(e) => {
                    setSelectedSeason(e.target.value);
                    setPage(0);
                  }}
                  className="w-full border rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Všechny sezóny</option>
                  {availableSeasons.map((season) => (
                    <option key={season} value={season}>
                      {season}
                    </option>
                  ))}
                </select>
              </div>

              {(selectedLeagueId || selectedSeason) && (
                <Button
                  variant="outline"
                  onClick={resetFilters}
                  className="flex items-center gap-2"
                >
                  <X className="h-4 w-4" />
                  Vymazat filtry
                </Button>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Pagination controls */}
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <label className="text-sm font-medium">Na stránku:</label>
                  <select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value));
                      setPage(0);
                    }}
                    className="border rounded px-3 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="10">10</option>
                    <option value="20">20</option>
                    <option value="50">50</option>
                    <option value="100">100</option>
                  </select>
                </div>
                <div className="text-sm text-gray-600 font-medium">
                  Stránka {page + 1} z {totalPages}
                </div>
                <div className="text-sm text-gray-500">
                  (Zobrazeno {data?.rounds.length || 0} z {totalCount} kol)
                </div>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(0, p - 1))}
                  disabled={page === 0}
                >
                  ← Předchozí
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page >= totalPages - 1}
                >
                  Další →
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Legend */}
        <Card className="mb-6 bg-blue-50 border-blue-200">
          <CardContent className="pt-6">
            <div className="flex gap-8 text-sm flex-wrap">
              <div className="flex items-center gap-2">
                <span className="font-bold">1</span>
                <span className="text-gray-600">= Domácí</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="font-bold">X</span>
                <span className="text-gray-600">= Remíza</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="font-bold">2</span>
                <span className="text-gray-600">= Hosté</span>
              </div>
              <div className="flex items-center gap-2">
                <Sparkles className="h-4 w-4 text-yellow-600" />
                <span className="text-gray-600">= Speciální kolo (všechny zápasy stejný výsledek)</span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Rounds list */}
        <div className="space-y-5">
          {data?.rounds.map((round) => {
            const specialRoundInfo = getSpecialRoundInfo(round);
            const isSpecial = isSpecialRound(round);

            return (
              <Card
                key={round.id}
                className={`shadow-md hover:shadow-lg transition-all ${
                  isSpecial
                    ? `border-4 ${specialRoundInfo?.color}`
                    : "border-2 border-gray-200"
                }`}
              >
                <CardHeader
                  className="cursor-pointer hover:bg-gray-50 transition-colors"
                  onClick={() => toggleRound(round.id)}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4 flex-1">
                      {expandedRounds.has(round.id) ? (
                        <ChevronDown className="h-6 w-6 text-gray-500 mt-1" />
                      ) : (
                        <ChevronRight className="h-6 w-6 text-gray-500 mt-1" />
                      )}
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-1">
                          <CardTitle className="text-2xl">
                            Kolo {round.roundNumber}
                          </CardTitle>
                          {isSpecial && (
                            <span className="flex items-center gap-1 px-3 py-1 bg-gradient-to-r from-yellow-400 to-orange-400 text-white text-xs font-bold rounded-full shadow-md">
                              <Sparkles className="h-3 w-3" />
                              {specialRoundInfo?.type}
                            </span>
                          )}
                        </div>
                        <CardDescription className="text-base">
                          {round.league?.countryFlagEmoji} {round.league?.displayName} • {round.season} • {round.matchesCount} zápasů
                        </CardDescription>
                      </div>
                    </div>

                    <div className="flex items-center gap-8 text-sm">
                      <div className="text-center">
                        <div className="text-gray-500 text-xs mb-1">Výsledky (1-X-2)</div>
                        <div className="font-mono font-bold text-xl">
                          {formatSummaryResult(round.summaryResult)}
                        </div>
                      </div>

                      <div className="text-center">
                        <div className="text-gray-500 text-xs mb-2">Kumulativní kurzy</div>
                        <div className="flex gap-2 font-mono text-sm">
                          <span
                            className={`px-3 py-1.5 rounded-md transition-all ${
                              isAllHomeWins(round)
                                ? "bg-green-500 text-white font-bold shadow-md scale-110"
                                : "bg-gray-100 text-gray-700"
                            }`}
                          >
                            1: {round.cumulativeOddsHome?.toFixed(0)}
                          </span>
                          <span
                            className={`px-3 py-1.5 rounded-md transition-all ${
                              isAllDraws(round)
                                ? "bg-yellow-500 text-white font-bold shadow-md scale-110"
                                : "bg-gray-100 text-gray-700"
                            }`}
                          >
                            X: {round.cumulativeOddsDraw?.toFixed(0)}
                          </span>
                          <span
                            className={`px-3 py-1.5 rounded-md transition-all ${
                              isAllAwayWins(round)
                                ? "bg-red-500 text-white font-bold shadow-md scale-110"
                                : "bg-gray-100 text-gray-700"
                            }`}
                          >
                            2: {round.cumulativeOddsAway?.toFixed(0)}
                          </span>
                        </div>
                      </div>

                      <div className="text-center">
                        <div className="text-gray-500 text-xs mb-1">Kurzy</div>
                        <div
                          className={`px-3 py-1.5 rounded-md text-sm font-medium ${
                            round.oddsComplete === "Yes"
                              ? "bg-green-100 text-green-800"
                              : "bg-yellow-100 text-yellow-800"
                          }`}
                        >
                          {round.oddsComplete}
                        </div>
                      </div>
                    </div>
                  </div>
                </CardHeader>

                {expandedRounds.has(round.id) && (
                  <CardContent className="bg-gray-50">
                    <div className="bg-white rounded-lg overflow-hidden shadow-inner border-2 border-gray-100">
                      <table className="w-full text-sm">
                        <thead className="bg-gradient-to-r from-gray-700 to-gray-800 text-white">
                          <tr>
                            <th className="text-left p-4 font-semibold">Domácí</th>
                            <th className="text-center p-4 font-semibold w-28">Skóre</th>
                            <th className="text-left p-4 font-semibold">Hosté</th>
                            <th className="text-center p-4 font-semibold w-32">Datum</th>
                            <th className="text-center p-4 font-semibold w-24">1</th>
                            <th className="text-center p-4 font-semibold w-24">X</th>
                            <th className="text-center p-4 font-semibold w-24">2</th>
                          </tr>
                        </thead>
                        <tbody>
                          {round.matches.map((match, idx) => (
                            <tr
                              key={match.id}
                              className={`${
                                idx % 2 === 0 ? "bg-white" : "bg-gray-50"
                              } hover:bg-blue-50 transition-colors`}
                            >
                              <td className="p-4 font-medium text-gray-800">
                                {match.homeTeam}
                              </td>
                              <td className="p-4 text-center">
                                <span className="font-mono font-bold text-lg bg-gray-100 px-3 py-1 rounded">
                                  {match.homeScore} : {match.awayScore}
                                </span>
                              </td>
                              <td className="p-4 font-medium text-gray-800">
                                {match.awayTeam}
                              </td>
                              <td className="p-4 text-center text-sm text-gray-600">
                                {match.matchDate
                                  ? new Date(match.matchDate).toLocaleDateString("cs-CZ")
                                  : "-"}
                              </td>
                              <td
                                className={`p-4 text-center font-mono font-semibold ${getWinningOddsClass(
                                  match.result,
                                  "H"
                                )}`}
                              >
                                {match.homeOdds?.toFixed(2) || "-"}
                              </td>
                              <td
                                className={`p-4 text-center font-mono font-semibold ${getWinningOddsClass(
                                  match.result,
                                  "D"
                                )}`}
                              >
                                {match.drawOdds?.toFixed(2) || "-"}
                              </td>
                              <td
                                className={`p-4 text-center font-mono font-semibold ${getWinningOddsClass(
                                  match.result,
                                  "A"
                                )}`}
                              >
                                {match.awayOdds?.toFixed(2) || "-"}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </CardContent>
                )}
              </Card>
            );
          })}
        </div>

        {/* Bottom pagination */}
        {totalPages > 1 && (
          <div className="mt-6 flex justify-center">
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setPage((p) => Math.max(0, p - 1))}
                disabled={page === 0}
              >
                ← Předchozí
              </Button>
              <div className="flex items-center px-4 text-sm">
                Stránka {page + 1} z {totalPages}
              </div>
              <Button
                variant="outline"
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= totalPages - 1}
              >
                Další →
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
