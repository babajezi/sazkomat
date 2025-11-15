"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { configApi } from "@/lib/api/client";
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
import { EditProviderDialog } from "@/components/EditProviderDialog";
import { ProviderType, type DataProvider } from "@/lib/api/types";

export default function BettingProvidersPage() {
  const queryClient = useQueryClient();
  const [syncingProvider, setSyncingProvider] = useState<string | null>(null);
  const [editingProvider, setEditingProvider] = useState<DataProvider | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  const { data: providers, isLoading, error } = useQuery({
    queryKey: ["betting-providers"],
    queryFn: () => configApi.getBettingProviders(),
  });

  // Poll sync status for syncing provider
  const { data: syncStatus } = useQuery({
    queryKey: ["sync-status", syncingProvider],
    queryFn: () => syncingProvider
      ? configApi.getProviderSyncStatus(syncingProvider, "football")
      : Promise.resolve(null),
    enabled: syncingProvider !== null,
    refetchInterval: syncingProvider ? 2000 : false,
  });

  const syncMutation = useMutation({
    mutationFn: ({ providerId, sportCode }: { providerId: string; sportCode: string }) =>
      configApi.syncBettingProviderLeagues(providerId, { sportCode }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      setSyncingProvider(null);
    },
    onError: () => {
      setSyncingProvider(null);
    },
  });

  const autoEnableMutation = useMutation({
    mutationFn: () => configApi.autoEnableBetExplorerSync(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const handleSync = async (provider: DataProvider) => {
    setSyncingProvider(provider.id);
    syncMutation.mutate({ providerId: provider.id, sportCode: "football" });
  };

  const handleEdit = (provider: DataProvider) => {
    setEditingProvider(provider);
    setEditDialogOpen(true);
  };

  // Check if provider is currently syncing
  const isProviderSyncing = (providerId: string) => {
    if (syncingProvider !== providerId) return false;
    return syncStatus?.status === "running";
  };

  const handleAutoEnable = () => {
    if (window.confirm("Automaticky povolit BetExplorer synchronizaci pro všechny ligy s betting supportem?")) {
      autoEnableMutation.mutate();
    }
  };

  const getProviderTypeName = (type: ProviderType) => {
    switch (type) {
      case ProviderType.Scraper: return "Scraper";
      case ProviderType.API: return "API";
      case ProviderType.Manual: return "Manual";
      case ProviderType.BettingProvider: return "Betting Provider";
      default: return "Unknown";
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto p-6">
        <p>Načítám betting providery...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mx-auto p-6">
        <p className="text-red-500">Chyba: {String(error)}</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Betting Providers</h1>
          <p className="text-gray-600">
            Správa českých sázkových kanceláří
          </p>
        </div>
        <Button
          onClick={handleAutoEnable}
          disabled={autoEnableMutation.isPending}
          variant="default"
        >
          {autoEnableMutation.isPending ? "Zpracovávám..." : "Auto-Enable BetExplorer"}
        </Button>
      </div>

      {/* Info Card */}
      <Card>
        <CardHeader>
          <CardTitle>O Betting Providers</CardTitle>
          <CardDescription>
            Integrace s českými sázkovými kancelářemi pro získávání seznamu sázitelných lig a aktuálních kurzů
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-2 text-sm">
            <p><strong>Účel:</strong> Identifikovat všechny sportovní ligy, na které lze v ČR sázet</p>
            <p><strong>Funkce:</strong> Synchronizace seznamu lig z jednotlivých sázkových kanceláří</p>
            <p><strong>Auto-Enable:</strong> Automaticky povolí BetExplorer synchronizaci pro ligy podporované alespoň jedním betting providerem</p>
          </div>
        </CardContent>
      </Card>

      {/* Providers List */}
      <div className="grid gap-4">
        {providers?.map((provider) => (
          <Card key={provider.id}>
            <CardHeader>
              <div className="flex justify-between items-start">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    {provider.name}
                    {provider.isActive ? (
                      <Badge variant="default">Aktivní</Badge>
                    ) : (
                      <Badge variant="secondary">Neaktivní</Badge>
                    )}
                  </CardTitle>
                  <CardDescription className="mt-2">
                    <span className="font-mono text-xs">{provider.code}</span>
                    {" • "}
                    <a href={provider.baseUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                      {provider.baseUrl}
                    </a>
                  </CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button
                    onClick={() => handleEdit(provider)}
                    variant="outline"
                    size="sm"
                  >
                    Edit
                  </Button>
                  <Button
                    onClick={() => handleSync(provider)}
                    disabled={!provider.isActive || isProviderSyncing(provider.id)}
                    size="sm"
                  >
                    {isProviderSyncing(provider.id) ? "Syncing..." : "Sync Football Leagues"}
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-gray-600">Typ:</span>
                  <span className="ml-2 font-medium">{getProviderTypeName(provider.type)}</span>
                </div>
                <div>
                  <span className="text-gray-600">Priorita:</span>
                  <span className="ml-2 font-medium">{provider.priority}</span>
                </div>
                {provider.notes && (
                  <div className="col-span-2">
                    <span className="text-gray-600">Poznámka:</span>
                    <span className="ml-2">{provider.notes}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Back Link */}
      <div className="pt-4">
        <Link href="/">
          <Button variant="outline">← Zpět na Dashboard</Button>
        </Link>
      </div>

      {/* Edit Provider Dialog */}
      <EditProviderDialog
        provider={editingProvider}
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
      />
    </div>
  );
}
