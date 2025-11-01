"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";

interface SyncStatus {
  isRunning: boolean;
  currentOperation?: string | null;
  startedAt?: string | null;
  lastCompletedAt?: string | null;
  lastResult?: {
    success: boolean;
    message: string;
    statistics?: {
      totalProcessed: number;
      created: number;
      updated: number;
      skipped: number;
      errors: number;
    };
  } | null;
}

export default function QueuesPage() {
  const queryClient = useQueryClient();

  const { data: syncStatus, isLoading } = useQuery<SyncStatus>({
    queryKey: ["sync-status"],
    queryFn: async () => {
      const response = await fetch(`${API_URL}/api/sync/status`);
      if (!response.ok) throw new Error("Failed to fetch sync status");
      return response.json();
    },
    refetchInterval: 5000, // Poll every 5 seconds
  });

  const resetWorkflowMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch(`${API_URL}/api/sync/workflow/reset`, {
        method: "POST",
      });
      if (!response.ok) throw new Error("Failed to reset workflow");
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sync-status"] });
      queryClient.invalidateQueries({ queryKey: ["workflow-state"] });
    },
  });

  const handleResetWorkflow = () => {
    if (
      window.confirm(
        "Opravdu chcete resetovat workflow? Toto vymaže všechny Redis locks a reset workflow state."
      )
    ) {
      resetWorkflowMutation.mutate();
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  const formatDuration = (startedAt: string | null | undefined) => {
    if (!startedAt) return "N/A";
    const start = new Date(startedAt);
    const now = new Date();
    const diff = now.getTime() - start.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Fronty Synchronizace</h1>
            <p className="text-gray-600">
              Monitoring a správa synchronizačních operací
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        {/* Current Sync Status */}
        <Card className="mb-6">
          <CardHeader>
            <div className="flex justify-between items-start">
              <div>
                <CardTitle>Aktuální Stav Synchronizace</CardTitle>
                <CardDescription>
                  Real-time monitoring běžících operací
                </CardDescription>
              </div>
              {syncStatus?.isRunning && (
                <Badge className="bg-blue-100 text-blue-800">
                  Running
                </Badge>
              )}
              {!syncStatus?.isRunning && (
                <Badge className="bg-gray-100 text-gray-600">
                  Idle
                </Badge>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {/* Running Operation */}
              {syncStatus?.isRunning && (
                <div className="p-4 bg-blue-50 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <div className="font-semibold text-blue-900">
                      {syncStatus.currentOperation || "Probíhá synchronizace..."}
                    </div>
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={handleResetWorkflow}
                      disabled={resetWorkflowMutation.isPending}
                    >
                      {resetWorkflowMutation.isPending ? "Ruším..." : "🛑 Přerušit"}
                    </Button>
                  </div>
                  <div className="text-sm text-blue-700">
                    <div>Začátek: {syncStatus.startedAt ? new Date(syncStatus.startedAt).toLocaleString("cs-CZ") : "N/A"}</div>
                    <div>Trvání: {formatDuration(syncStatus.startedAt)}</div>
                  </div>
                  <div className="mt-3">
                    <div className="h-2 bg-blue-200 rounded-full overflow-hidden">
                      <div className="h-full bg-blue-600 rounded-full animate-pulse" style={{ width: "50%" }} />
                    </div>
                    <p className="text-xs text-blue-600 mt-1">
                      Synchronizace probíhá...
                    </p>
                  </div>
                </div>
              )}

              {/* Last Completed */}
              {!syncStatus?.isRunning && syncStatus?.lastCompletedAt && (
                <div>
                  <div className="text-sm font-semibold mb-2">Poslední dokončená operace:</div>
                  <div className="p-4 bg-gray-50 rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      {syncStatus.lastResult?.success ? (
                        <Badge className="bg-green-100 text-green-800">
                          ✓ Úspěch
                        </Badge>
                      ) : (
                        <Badge className="bg-red-100 text-red-800">
                          ✗ Chyba
                        </Badge>
                      )}
                      <span className="text-sm text-gray-600">
                        {new Date(syncStatus.lastCompletedAt).toLocaleString("cs-CZ")}
                      </span>
                    </div>
                    <div className="text-sm text-gray-700">
                      {syncStatus.lastResult?.message || "Bez zprávy"}
                    </div>
                    {syncStatus.lastResult?.statistics && (
                      <div className="mt-3 grid grid-cols-5 gap-2 text-xs">
                        <div className="text-center p-2 bg-white rounded">
                          <div className="font-semibold">{syncStatus.lastResult.statistics.totalProcessed}</div>
                          <div className="text-gray-500">Celkem</div>
                        </div>
                        <div className="text-center p-2 bg-white rounded">
                          <div className="font-semibold text-green-600">{syncStatus.lastResult.statistics.created}</div>
                          <div className="text-gray-500">Vytvořeno</div>
                        </div>
                        <div className="text-center p-2 bg-white rounded">
                          <div className="font-semibold text-blue-600">{syncStatus.lastResult.statistics.updated}</div>
                          <div className="text-gray-500">Aktualizováno</div>
                        </div>
                        <div className="text-center p-2 bg-white rounded">
                          <div className="font-semibold text-gray-600">{syncStatus.lastResult.statistics.skipped}</div>
                          <div className="text-gray-500">Přeskočeno</div>
                        </div>
                        <div className="text-center p-2 bg-white rounded">
                          <div className="font-semibold text-red-600">{syncStatus.lastResult.statistics.errors}</div>
                          <div className="text-gray-500">Chyby</div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* No Activity */}
              {!syncStatus?.isRunning && !syncStatus?.lastCompletedAt && (
                <div className="text-center py-8 text-gray-500">
                  <div className="text-4xl mb-2">💤</div>
                  <p>Žádná synchronizace zatím neproběhla</p>
                  <Link href="/sync">
                    <Button className="mt-4" variant="outline">
                      Přejít na Sync Workflow
                    </Button>
                  </Link>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Info Card - Next Steps */}
        <Card className="border-blue-200 bg-blue-50">
          <CardHeader>
            <CardTitle className="text-blue-900">📋 Poznámka k implementaci</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-blue-800">
            <p className="mb-2">
              Toto je <strong>MVP verze</strong> stránky pro monitoring front.
              Zobrazuje aktuální stav synchronizace z in-memory storage.
            </p>
            <p className="font-semibold mt-4 mb-2">Pro plnou funkcionalitu je potřeba dokončit:</p>
            <ul className="list-disc list-inside space-y-1 ml-2">
              <li>DB entita <code>SyncQueueItem</code> pro perzistentní historii</li>
              <li>Repository pattern a migrace</li>
              <li>API endpointy pro CRUD operace</li>
              <li>Queue tracking v ProviderSyncService</li>
              <li>Progress tracking během sync operací</li>
              <li>Cancel/Retry akce pro jednotlivé fronty</li>
            </ul>
            <p className="mt-4 text-xs">
              Odhadovaný čas pro kompletní implementaci: ~2-3 hodiny
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
