"use client";

import { useState, useEffect } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { recipeApi, configApi, seasonApi } from "@/lib/api/client";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2, CheckCircle2, XCircle, Clock, FileText } from "lucide-react";
import type { ScraperRecipe, TestRecipeResponse, League, LeagueSeason } from "@/lib/api/types";

interface TestRecipeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  recipe: ScraperRecipe | null;
}

export function TestRecipeDialog({
  open,
  onOpenChange,
  recipe,
}: TestRecipeDialogProps) {
  const [selectedLeagueId, setSelectedLeagueId] = useState<string>("");
  const [selectedSeason, setSelectedSeason] = useState<string>("");
  const [testResult, setTestResult] = useState<TestRecipeResponse | null>(null);

  // Fetch leagues
  const { data: leagues } = useQuery({
    queryKey: ["leagues"],
    queryFn: () => configApi.getLeagues(),
    enabled: open,
  });

  // Fetch seasons for selected league
  const { data: seasons } = useQuery({
    queryKey: ["league-seasons", selectedLeagueId],
    queryFn: () => seasonApi.getLeagueSeasons(selectedLeagueId),
    enabled: !!selectedLeagueId,
  });

  // Reset state when dialog opens/closes
  useEffect(() => {
    if (open) {
      setTestResult(null);
    }
  }, [open]);

  // Reset season when league changes
  useEffect(() => {
    setSelectedSeason("");
    setTestResult(null);
  }, [selectedLeagueId]);

  const testMutation = useMutation({
    mutationFn: () =>
      recipeApi.test(recipe!.id, {
        leagueId: selectedLeagueId,
        season: selectedSeason,
      }),
    onSuccess: (result) => {
      setTestResult(result);
    },
  });

  const handleTest = () => {
    if (selectedLeagueId && selectedSeason && recipe) {
      testMutation.mutate();
    }
  };

  const selectedLeague = leagues?.find((l) => l.id === selectedLeagueId);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Testovat recept</DialogTitle>
          <DialogDescription>
            Otestujte recept &quot;{recipe?.name}&quot; na vybrané lize a sezóně
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-4">
          {/* League Selection */}
          <div className="grid gap-2">
            <Label>Vyberte ligu</Label>
            <Select value={selectedLeagueId} onValueChange={setSelectedLeagueId}>
              <SelectTrigger>
                <SelectValue placeholder="Vyberte ligu..." />
              </SelectTrigger>
              <SelectContent>
                {leagues?.map((league) => (
                  <SelectItem key={league.id} value={league.id}>
                    {league.country?.flagEmoji} {league.displayName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Season Selection */}
          {selectedLeagueId && (
            <div className="grid gap-2">
              <Label>Vyberte sezónu</Label>
              <Select value={selectedSeason} onValueChange={setSelectedSeason}>
                <SelectTrigger>
                  <SelectValue placeholder="Vyberte sezónu..." />
                </SelectTrigger>
                <SelectContent>
                  {seasons?.map((s) => (
                    <SelectItem key={s.id} value={s.seasonName}>
                      {s.seasonName}
                      {s.isCurrent && (
                        <Badge variant="outline" className="ml-2 text-xs">
                          aktuální
                        </Badge>
                      )}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Selected Info */}
          {selectedLeague && selectedSeason && (
            <div className="bg-gray-50 p-3 rounded-md text-sm">
              <div className="font-medium">Test URL:</div>
              <code className="text-xs text-gray-600 break-all">
                https://www.betexplorer.com/football/
                {selectedLeague.country?.code?.toLowerCase()}/
                {selectedLeague.betExplorerSlug}/{selectedSeason}/results/
              </code>
            </div>
          )}

          {/* Test Results */}
          {testMutation.isPending && (
            <Alert>
              <Loader2 className="h-4 w-4 animate-spin" />
              <AlertDescription>
                Testování receptu... Toto může trvat několik sekund.
              </AlertDescription>
            </Alert>
          )}

          {testResult && (
            <div className="space-y-4">
              {/* Result Summary */}
              <Alert variant={testResult.success ? "default" : "destructive"}>
                {testResult.success ? (
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                ) : (
                  <XCircle className="h-4 w-4" />
                )}
                <AlertDescription>
                  {testResult.success
                    ? `Úspěch! Nalezeno ${testResult.roundsFound} kol a ${testResult.totalMatches} zápasů.`
                    : `Chyba: ${testResult.error}`}
                </AlertDescription>
              </Alert>

              {/* Stats */}
              <div className="grid grid-cols-4 gap-2">
                <div className="bg-gray-50 p-3 rounded-md text-center">
                  <div className="text-2xl font-bold">{testResult.roundsFound}</div>
                  <div className="text-xs text-gray-500">Kol</div>
                </div>
                <div className="bg-gray-50 p-3 rounded-md text-center">
                  <div className="text-2xl font-bold">{testResult.totalMatches}</div>
                  <div className="text-xs text-gray-500">Zápasů</div>
                </div>
                <div className="bg-gray-50 p-3 rounded-md text-center">
                  <div className="text-2xl font-bold">{testResult.durationMs}</div>
                  <div className="text-xs text-gray-500">ms</div>
                </div>
                <div className="bg-gray-50 p-3 rounded-md text-center">
                  <div className="text-2xl font-bold">
                    {Math.round(testResult.htmlLength / 1024)}
                  </div>
                  <div className="text-xs text-gray-500">KB HTML</div>
                </div>
              </div>

              {/* Rounds Sample */}
              {testResult.roundsSample.length > 0 && (
                <div>
                  <div className="text-sm font-medium mb-2">
                    Ukázka kol (prvních 5):
                  </div>
                  <div className="space-y-1">
                    {testResult.roundsSample.map((round, idx) => (
                      <div
                        key={idx}
                        className="flex items-center justify-between text-sm bg-gray-50 px-3 py-2 rounded"
                      >
                        <span>
                          {round.groupName
                            ? `${round.groupName} - Kolo ${round.roundNumber}`
                            : `Kolo ${round.roundNumber}`}
                        </span>
                        <span className="text-gray-500">
                          {round.matchesCount} zápasů • {round.summaryResult}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Logs */}
              {testResult.logs.length > 0 && (
                <div>
                  <div className="text-sm font-medium mb-2 flex items-center gap-2">
                    <FileText className="h-4 w-4" />
                    Logy ({testResult.logs.length}):
                  </div>
                  <div className="bg-gray-900 text-gray-100 p-3 rounded-md text-xs font-mono max-h-40 overflow-y-auto">
                    {testResult.logs.map((log, idx) => (
                      <div key={idx} className="opacity-80">
                        {log}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {testMutation.isError && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertDescription>
                Chyba při testování: {(testMutation.error as Error).message}
              </AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Zavřít
          </Button>
          <Button
            onClick={handleTest}
            disabled={
              !selectedLeagueId ||
              !selectedSeason ||
              testMutation.isPending
            }
          >
            {testMutation.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Testování...
              </>
            ) : (
              "Spustit test"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
