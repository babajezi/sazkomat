"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { seasonApi } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ChevronDown, ChevronUp } from "lucide-react";
import Link from "next/link";
import type { LeagueSeason } from "@/lib/api/types";
import { SyncMode } from "@/lib/api/types";

interface LeagueSeasonsDisplayProps {
  leagueId: string;
}

export function LeagueSeasonsDisplay({ leagueId }: LeagueSeasonsDisplayProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const queryClient = useQueryClient();

  const { data: seasons, isLoading } = useQuery({
    queryKey: ["league-seasons", leagueId],
    queryFn: () => seasonApi.getLeagueSeasons(leagueId),
    enabled: isExpanded, // Only fetch when expanded
  });

  const toggleSyncMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      seasonApi.updateSyncEnabled(id, { enabled }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["league-seasons", leagueId] });
    },
  });

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
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setIsExpanded(false)}
        >
          <ChevronUp className="h-4 w-4" />
        </Button>
      </div>

      {isLoading && (
        <div className="text-sm text-gray-500">Načítám sezóny...</div>
      )}

      {seasons && seasons.length === 0 && (
        <div className="text-sm text-gray-500">Žádné sezóny nenalezeny</div>
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
