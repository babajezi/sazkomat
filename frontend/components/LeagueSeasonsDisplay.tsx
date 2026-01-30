"use client";

import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { seasonApi, syncApi } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ChevronDown, ChevronUp, RefreshCw, Download, Loader2, CheckCircle, XCircle } from "lucide-react";
import type { LeagueSeason, LeagueProvider } from "@/lib/api/types";
import { SyncMode, ProviderType, NoDataReason } from "@/lib/api/types";

// Czech translations for NoDataReason
const noDataReasonLabels: Record<NoDataReason, string> = {
  [NoDataReason.None]: "",
  [NoDataReason.PageNotFound]: "Stránka neexistuje",
  [NoDataReason.NoRoundsFound]: "Žádná kola",
  [NoDataReason.ParsingError]: "Chyba parsování",
  [NoDataReason.NetworkError]: "Síťová chyba",
  [NoDataReason.PartialData]: "Částečná data",
};

interface LeagueSeasonsDisplayProps {
  leagueId: string;
  leagueProviders?: LeagueProvider[];
}

type SyncStatus = "idle" | "loading" | "success" | "error";

export function LeagueSeasonsDisplay({ leagueId, leagueProviders }: LeagueSeasonsDisplayProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [syncDataStatus, setSyncDataStatus] = useState<SyncStatus>("idle");
  const [refreshListStatus, setRefreshListStatus] = useState<SyncStatus>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();

  // Check if league has betting provider mapping
  const hasBettingProvider = leagueProviders?.some(
    (lp) => lp.provider?.type === ProviderType.BettingProvider && lp.isActive
  ) ?? false;

  // Debug log
  console.log("LeagueSeasonsDisplay debug:", {
    leagueId,
    leagueProvidersCount: leagueProviders?.length,
    hasBettingProvider,
    providers: leagueProviders?.map(lp => ({
      name: lp.providerName,
      type: lp.provider?.type,
      isActive: lp.isActive
    }))
  });

  const { data: seasons, isLoading, refetch } = useQuery({
    queryKey: ["league-seasons", leagueId],
    queryFn: () => seasonApi.getLeagueSeasons(leagueId),
    enabled: isExpanded,
  });

  const toggleSyncMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      seasonApi.updateSyncEnabled(id, { enabled }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["league-seasons", leagueId] });
    },
  });

  // Sync data mutation (rounds and matches)
  const syncDataMutation = useMutation({
    mutationFn: () => syncApi.syncLeagueSeasonData(leagueId),
    onMutate: () => {
      setSyncDataStatus("loading");
      setErrorMessage(null);
    },
    onSuccess: () => {
      setSyncDataStatus("success");
      // Refresh seasons list after sync
      refetch();
      // Reset status after 3 seconds
      setTimeout(() => setSyncDataStatus("idle"), 3000);
    },
    onError: (error: Error) => {
      setSyncDataStatus("error");
      setErrorMessage(error.message || "Sync failed");
      // Reset status after 5 seconds
      setTimeout(() => {
        setSyncDataStatus("idle");
        setErrorMessage(null);
      }, 5000);
    },
  });

  // Refresh list mutation (seasons metadata only)
  const refreshListMutation = useMutation({
    mutationFn: () => syncApi.refreshLeagueSeasonsList(leagueId),
    onMutate: () => {
      setRefreshListStatus("loading");
      setErrorMessage(null);
    },
    onSuccess: () => {
      setRefreshListStatus("success");
      // Refresh seasons list
      refetch();
      // Reset status after 3 seconds
      setTimeout(() => setRefreshListStatus("idle"), 3000);
    },
    onError: (error: Error) => {
      setRefreshListStatus("error");
      setErrorMessage(error.message || "Refresh failed");
      // Reset status after 5 seconds
      setTimeout(() => {
        setRefreshListStatus("idle");
        setErrorMessage(null);
      }, 5000);
    },
  });

  const handleSyncData = () => {
    if (syncDataStatus !== "loading") {
      syncDataMutation.mutate();
    }
  };

  const handleRefreshList = () => {
    if (refreshListStatus !== "loading") {
      refreshListMutation.mutate();
    }
  };

  // Render sync button with status
  const renderSyncDataButton = () => {
    const isDisabled = syncDataStatus === "loading" || refreshListStatus === "loading";

    if (syncDataStatus === "loading") {
      return (
        <Button variant="default" size="sm" disabled>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          Synchronizuji...
        </Button>
      );
    }

    if (syncDataStatus === "success") {
      return (
        <Button variant="default" size="sm" disabled className="bg-green-600">
          <CheckCircle className="mr-2 h-4 w-4" />
          Dokončeno
        </Button>
      );
    }

    if (syncDataStatus === "error") {
      return (
        <Button variant="destructive" size="sm" onClick={handleSyncData}>
          <XCircle className="mr-2 h-4 w-4" />
          Chyba - zkusit znovu
        </Button>
      );
    }

    return (
      <Button
        variant="default"
        size="sm"
        onClick={handleSyncData}
        disabled={isDisabled}
        title="Stáhnout kola a zápasy pro všechny sezóny"
      >
        <Download className="mr-2 h-4 w-4" />
        Sync data
      </Button>
    );
  };

  // Render refresh button with status
  const renderRefreshButton = () => {
    const isDisabled = syncDataStatus === "loading" || refreshListStatus === "loading";

    if (refreshListStatus === "loading") {
      return (
        <Button variant="outline" size="sm" disabled>
          <Loader2 className="h-4 w-4 animate-spin" />
        </Button>
      );
    }

    if (refreshListStatus === "success") {
      return (
        <Button variant="outline" size="sm" disabled className="text-green-600 border-green-600">
          <CheckCircle className="h-4 w-4" />
        </Button>
      );
    }

    if (refreshListStatus === "error") {
      return (
        <Button variant="outline" size="sm" onClick={handleRefreshList} className="text-red-600 border-red-600">
          <XCircle className="h-4 w-4" />
        </Button>
      );
    }

    return (
      <Button
        variant="outline"
        size="sm"
        onClick={handleRefreshList}
        disabled={isDisabled}
        title="Obnovit seznam sezón z BetExploreru"
      >
        <RefreshCw className="h-4 w-4" />
      </Button>
    );
  };

  if (!isExpanded) {
    return (
      <div className="mt-4">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setIsExpanded(true)}
          className="w-full"
        >
          <ChevronDown className="mr-2 h-4 w-4" />
          Zobrazit sezóny
        </Button>
      </div>
    );
  }

  return (
    <div className="mt-4 border-t pt-4">
      <div className="flex justify-between items-center mb-3">
        <h4 className="font-medium">Načtené sezóny</h4>
        <div className="flex items-center gap-2">
          {/* Sync buttons - always show for testing */}
          {renderSyncDataButton()}
          {renderRefreshButton()}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsExpanded(false)}
          >
            <ChevronUp className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Error message */}
      {errorMessage && (
        <div className="mb-3 p-2 bg-red-50 border border-red-200 rounded text-sm text-red-700">
          {errorMessage}
        </div>
      )}

      {isLoading && (
        <div className="text-sm text-gray-500">Načítám sezóny...</div>
      )}

      {seasons && seasons.length === 0 && (
        <div className="text-sm text-gray-500">
          Žádné sezóny nenalezeny.
          {hasBettingProvider && (
            <span className="ml-1">
              Klikněte na <RefreshCw className="inline h-3 w-3" /> pro načtení seznamu z BetExploreru.
            </span>
          )}
        </div>
      )}

      {seasons && seasons.length > 0 && (
        <div className="space-y-2">
          {seasons.map((season) => (
            <SeasonRow
              key={season.id}
              season={season}
              leagueId={leagueId}
              onToggleSync={(enabled) =>
                toggleSyncMutation.mutate({ id: season.id, enabled })
              }
              isToggling={toggleSyncMutation.isPending}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface Round {
  id: string;
  roundNumber: number;
  groupName: string | null;  // null = liga bez skupin, e.g. "East", "West", "GROUP 1"
  matchesCount: number;
  homeWins: number;
  draws: number;
  awayWins: number;
  oddsComplete: string;
}

interface SeasonRowProps {
  season: LeagueSeason;
  leagueId: string;
  onToggleSync: (enabled: boolean) => void;
  isToggling: boolean;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

function SeasonRow({ season, leagueId, onToggleSync, isToggling }: SeasonRowProps) {
  const [showWarning, setShowWarning] = useState(false);
  const [showRounds, setShowRounds] = useState(false);
  const [selectedGroup, setSelectedGroup] = useState<string | null>(null);

  // Fetch rounds automatically for seasons with data - needed for perfect rounds stats
  const { data: roundsData, isLoading: roundsLoading } = useQuery({
    queryKey: ["season-rounds", leagueId, season.seasonName],
    queryFn: async () => {
      const response = await fetch(
        `${API_URL}/api/import/rounds?leagueId=${leagueId}&season=${encodeURIComponent(season.seasonName)}&take=100`
      );
      if (!response.ok) throw new Error("Failed to fetch rounds");
      return response.json();
    },
    enabled: season.hasData,  // Always fetch for seasons with data
    staleTime: 5 * 60 * 1000, // 5 minutes - rounds data doesn't change often
    gcTime: 10 * 60 * 1000,   // Keep in cache for 10 minutes
  });

  const handleToggleClick = () => {
    // If enabling sync for a historical season, show warning
    if (!season.syncEnabled && season.syncMode === SyncMode.Historical) {
      setShowWarning(true);
    } else {
      onToggleSync(!season.syncEnabled);
    }
  };

  const handleConfirmSync = () => {
    setShowWarning(false);
    onToggleSync(true);
  };

  const rounds: Round[] = roundsData?.rounds || [];

  // Extract unique groups from rounds
  const groups = [...new Set(rounds.map(r => r.groupName).filter(Boolean))] as string[];
  const hasGroups = groups.length > 1;

  // Filter rounds by selected group (if groups exist)
  const filteredRounds = hasGroups && selectedGroup
    ? rounds.filter(r => r.groupName === selectedGroup)
    : rounds;

  // Calculate "perfect" rounds stats (all same result) - use filtered rounds
  const perfectRounds = filteredRounds.reduce(
    (acc, round) => {
      const isAllHome = round.homeWins > 0 && round.draws === 0 && round.awayWins === 0;
      const isAllDraw = round.draws > 0 && round.homeWins === 0 && round.awayWins === 0;
      const isAllAway = round.awayWins > 0 && round.homeWins === 0 && round.draws === 0;
      return {
        home: acc.home + (isAllHome ? 1 : 0),
        draw: acc.draw + (isAllDraw ? 1 : 0),
        away: acc.away + (isAllAway ? 1 : 0),
      };
    },
    { home: 0, draw: 0, away: 0 }
  );
  const hasPerfectRounds = perfectRounds.home > 0 || perfectRounds.draw > 0 || perfectRounds.away > 0;

  return (
    <>
      <div className="border rounded-lg p-3 bg-white">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <span className="font-medium">{season.seasonName}</span>
            {season.isCurrent && (
              <span className="text-xs bg-blue-100 text-blue-800 px-2 py-0.5 rounded">
                Aktuální
              </span>
            )}
            <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">
              {season.syncMode}
            </span>
            {season.hasData && season.noDataReason !== NoDataReason.PartialData && (
              <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded">
                ✓ Data
              </span>
            )}
            {season.hasData && season.noDataReason === NoDataReason.PartialData && (
              <span className="text-xs bg-amber-100 text-amber-700 px-2 py-0.5 rounded">
                ⚠ {noDataReasonLabels[NoDataReason.PartialData]}
              </span>
            )}
            {!season.hasData && season.noDataReason && season.noDataReason !== NoDataReason.None && (
              <span className="text-xs bg-red-100 text-red-700 px-2 py-0.5 rounded">
                ✗ {noDataReasonLabels[season.noDataReason]}
              </span>
            )}
          </div>
          {/* Note for partial data or error */}
          {season.noDataNote && (
            <div className="text-xs text-gray-500 italic mt-1">
              {season.noDataNote}
            </div>
          )}
          <div className="flex flex-col gap-1 items-end">
            <Button
              variant={season.syncEnabled ? "default" : "outline"}
              size="sm"
              onClick={handleToggleClick}
              disabled={isToggling}
            >
              {season.syncEnabled ? "✓ Sync ON" : "○ Sync OFF"}
            </Button>
            {season.hasData && (
              <button
                onClick={() => setShowRounds(!showRounds)}
                className="p-1 hover:bg-gray-100 rounded transition-colors"
              >
                {showRounds ? <ChevronUp className="h-7 w-7 text-gray-600" /> : <ChevronDown className="h-7 w-7 text-gray-600" />}
              </button>
            )}
          </div>
        </div>

      <div className="flex flex-wrap gap-3 mt-2">
        <span className="inline-flex items-center px-2 py-1 rounded bg-blue-100 text-blue-800 text-sm font-medium">
          Kol: {season.roundsCount || 0}
        </span>
        <span className="inline-flex items-center px-2 py-1 rounded bg-green-100 text-green-800 text-sm font-medium">
          Zápasů: {season.matchesCount || 0}
        </span>
        <span className={`inline-flex items-center px-2 py-1 rounded text-sm font-medium ${season.hasOdds ? "bg-yellow-100 text-yellow-800" : "bg-gray-100 text-gray-500"}`}>
          Kurzy: {season.hasOdds ? "✓" : "—"}
        </span>

        {/* Perfect rounds indicators - only show when rounds are loaded */}
        {hasPerfectRounds && (
          <div className="flex gap-2 ml-2 pl-2 border-l-2 border-gray-300">
            {perfectRounds.home > 0 && (
              <span className="inline-flex items-center px-3 py-1 rounded-full bg-green-600 text-white text-sm font-bold shadow-md animate-pulse">
                {perfectRounds.home}H
              </span>
            )}
            {perfectRounds.draw > 0 && (
              <span className="inline-flex items-center px-3 py-1 rounded-full bg-yellow-500 text-white text-sm font-bold shadow-md animate-pulse">
                {perfectRounds.draw}R
              </span>
            )}
            {perfectRounds.away > 0 && (
              <span className="inline-flex items-center px-3 py-1 rounded-full bg-red-600 text-white text-sm font-bold shadow-md animate-pulse">
                {perfectRounds.away}A
              </span>
            )}
          </div>
        )}
      </div>

      {season.lastDataSyncAt && (
        <div className="text-xs text-gray-500 mt-1">
          Poslední sync dat: {new Date(season.lastDataSyncAt).toLocaleString("cs-CZ")}
        </div>
      )}

      {/* Expandable rounds list */}
      {showRounds && season.hasData && (
        <div className="mt-3 pt-3 border-t">
          {roundsLoading ? (
            <div className="text-sm text-gray-500 flex items-center gap-2">
              <Loader2 className="h-4 w-4 animate-spin" />
              Načítám kola...
            </div>
          ) : rounds.length > 0 ? (
            <div className="space-y-1">
              {hasGroups && (
                <div className="flex flex-wrap gap-1 mb-3">
                  <button
                    onClick={() => setSelectedGroup(null)}
                    className={`px-2 py-1 text-xs rounded ${!selectedGroup ? 'bg-blue-500 text-white' : 'bg-gray-100 hover:bg-gray-200'}`}
                  >
                    Všechny ({rounds.length})
                  </button>
                  {groups.map(group => (
                    <button
                      key={group}
                      onClick={() => setSelectedGroup(group)}
                      className={`px-2 py-1 text-xs rounded ${selectedGroup === group ? 'bg-blue-500 text-white' : 'bg-gray-100 hover:bg-gray-200'}`}
                    >
                      {group} ({rounds.filter(r => r.groupName === group).length})
                    </button>
                  ))}
                </div>
              )}
              <div className="text-xs font-medium text-gray-500 mb-2">Kola:</div>
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2">
                {filteredRounds.map((round) => {
                  // Only highlight if ALL results are of one type (others are 0)
                  const isAllHome = round.homeWins > 0 && round.draws === 0 && round.awayWins === 0;
                  const isAllDraw = round.draws > 0 && round.homeWins === 0 && round.awayWins === 0;
                  const isAllAway = round.awayWins > 0 && round.homeWins === 0 && round.draws === 0;

                  return (
                    <a
                      key={round.id}
                      href={`/matches?leagueId=${leagueId}&season=${encodeURIComponent(season.seasonName)}&roundNumber=${round.roundNumber}${round.groupName ? `&groupName=${encodeURIComponent(round.groupName)}` : ''}`}
                      className="flex items-center justify-between p-2 bg-gray-50 rounded hover:bg-gray-100"
                    >
                      <div className="text-xs">
                        <div className="font-medium">
                          {round.groupName ? `${round.groupName} - ` : ''}Kolo {round.roundNumber}
                        </div>
                        <div className="text-gray-500">{round.matchesCount} zápasů</div>
                      </div>
                      <div className="flex items-center gap-1 font-mono text-lg font-semibold">
                        <span className={isAllHome ? "px-1 rounded bg-green-500 text-white" : "text-gray-900"}>
                          {round.homeWins}
                        </span>
                        <span className="text-gray-400">:</span>
                        <span className={isAllDraw ? "px-1 rounded bg-yellow-500 text-white" : "text-gray-900"}>
                          {round.draws}
                        </span>
                        <span className="text-gray-400">:</span>
                        <span className={isAllAway ? "px-1 rounded bg-red-500 text-white" : "text-gray-900"}>
                          {round.awayWins}
                        </span>
                      </div>
                    </a>
                  );
                })}
              </div>
            </div>
          ) : (
            <div className="text-sm text-gray-500">Žádná kola nenalezena</div>
          )}
        </div>
      )}
    </div>

    <Dialog open={showWarning} onOpenChange={setShowWarning}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Varování: Historická sezóna</DialogTitle>
          <DialogDescription>
            Pokoušíte se zapnout synchronizaci pro historickou sezónu {season.seasonName}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
            <h4 className="font-semibold text-amber-900 mb-2">Co je historická sezóna?</h4>
            <p className="text-sm text-amber-800">
              Historická sezóna je již ukončená sezóna, kde se data již nemění.
              Synchronizace takových sezón obvykle není potřebná, protože všechna data již byla importována.
            </p>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h4 className="font-semibold text-blue-900 mb-2">Aktuální sezóna vs. Historická sezóna</h4>
            <ul className="text-sm text-blue-800 space-y-2">
              <li>
                <strong>Current:</strong> Probíhající sezóna s průběžně aktualizovanými daty (zápasy, výsledky, kurzy)
              </li>
              <li>
                <strong>Historical:</strong> Ukončená sezóna s kompletními historickými daty
              </li>
            </ul>
          </div>

          <p className="text-sm text-gray-600">
            Pokud chcete přesto zapnout synchronizaci pro tuto historickou sezónu,
            klikněte na tlačítko níže. Systém bude pravidelně kontrolovat případné změny dat.
          </p>
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => setShowWarning(false)}
          >
            Zrušit
          </Button>
          <Button
            type="button"
            onClick={handleConfirmSync}
          >
            Rozumím, zapnout sync
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
    </>
  );
}
