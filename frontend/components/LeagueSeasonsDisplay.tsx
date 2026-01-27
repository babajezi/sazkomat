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
import Link from "next/link";
import type { LeagueSeason, LeagueProvider } from "@/lib/api/types";
import { SyncMode, ProviderType } from "@/lib/api/types";

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

interface SeasonRowProps {
  season: LeagueSeason;
  leagueId: string;
  onToggleSync: (enabled: boolean) => void;
  isToggling: boolean;
}

function SeasonRow({ season, leagueId, onToggleSync, isToggling }: SeasonRowProps) {
  const [showWarning, setShowWarning] = useState(false);

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

  return (
    <>
      <div className="border rounded-lg p-3 bg-white hover:bg-gray-50">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <Link
              href={`/rounds?leagueId=${leagueId}&season=${encodeURIComponent(season.seasonName)}`}
              className="font-medium text-blue-600 hover:underline"
            >
              {season.seasonName}
            </Link>
            {season.isCurrent && (
              <span className="text-xs bg-blue-100 text-blue-800 px-2 py-0.5 rounded">
                Aktuální
              </span>
            )}
            <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">
              {season.syncMode}
            </span>
            {season.hasData && (
              <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded">
                ✓ Data
              </span>
            )}
          </div>
          <Button
            variant={season.syncEnabled ? "default" : "outline"}
            size="sm"
            onClick={handleToggleClick}
            disabled={isToggling}
          >
            {season.syncEnabled ? "✓ Sync ON" : "○ Sync OFF"}
          </Button>
        </div>

      <div className="grid grid-cols-3 gap-2 text-xs text-gray-600">
        <div>
          <span className="font-medium">Kola:</span> {season.roundsCount || 0}
        </div>
        <div>
          <span className="font-medium">Zápasy:</span> {season.matchesCount || 0}
        </div>
        <div>
          <span className="font-medium">Kurzy:</span> {season.hasOdds ? "✓" : "—"}
        </div>
      </div>

      {season.lastDataSyncAt && (
        <div className="text-xs text-gray-500 mt-1">
          Poslední sync dat: {new Date(season.lastDataSyncAt).toLocaleString("cs-CZ")}
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
