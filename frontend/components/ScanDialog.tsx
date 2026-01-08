"use client";

import { useState, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { ProviderLogo } from "@/components/ProviderLogo";
import { Loader2, ScanLine, CheckCircle2, AlertCircle } from "lucide-react";
import type { SyncEntityType, ScanJobResponse } from "@/lib/api/types";
import { configApi } from "@/lib/api/client";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";
const BET_EXPLORER_PROVIDER_ID = "a0000000-0000-0000-0000-000000000001";

interface ScanDialogProps {
  entityType: SyncEntityType;
  entityIds?: string[];
  trigger?: React.ReactNode;
  onSuccess?: (jobId: string) => void;
  providerId?: string;
}

export function ScanDialog({
  entityType,
  entityIds = [],
  trigger,
  onSuccess,
  providerId,
}: ScanDialogProps) {
  const [open, setOpen] = useState(false);
  const [selectedProviderIds, setSelectedProviderIds] = useState<string[]>([]);
  const [hasPreselected, setHasPreselected] = useState(false);
  const queryClient = useQueryClient();

  // Load selected provider details for display in alert message
  const { data: selectedProvider } = useQuery({
    queryKey: ["provider", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/providers`);
      if (!res.ok) throw new Error("Failed to fetch providers");
      const providers = await res.json();
      return providers.find((p: any) => p.id === providerId);
    },
    enabled: open && !!providerId && entityType !== "Leagues",
  });

  // Load betting providers for Leagues scan
  const { data: bettingProviders, isLoading: providersLoading } = useQuery({
    queryKey: ["betting-providers"],
    queryFn: () => configApi.getBettingProviders(),
    enabled: entityType === "Leagues" && open,
  });

  // Pre-select provider from sync page when dialog opens
  useEffect(() => {
    if (open && entityType === "Leagues" && providerId && bettingProviders && !hasPreselected) {
      // Check if the providerId is a betting provider (available in the list)
      const isBettingProvider = bettingProviders.some(p => p.id === providerId);
      if (isBettingProvider) {
        setSelectedProviderIds([providerId]);
        setHasPreselected(true);
      }
    }
    // Reset preselection flag when dialog closes
    if (!open) {
      setHasPreselected(false);
    }
  }, [open, entityType, providerId, bettingProviders, hasPreselected]);

  const scanMutation = useMutation<ScanJobResponse | ScanJobResponse[]>({
    mutationFn: async () => {
      // For Leagues with multi-provider selection
      if (entityType === "Leagues" && selectedProviderIds.length > 0) {
        const results: ScanJobResponse[] = [];

        // Scan each selected provider
        for (const provId of selectedProviderIds) {
          // Find provider to determine which endpoint to use
          const provider = bettingProviders?.find(p => p.id === provId);
          const providerCode = provider?.code?.toLowerCase() || "";

          // Betano uses /api/scan/full (combined countries+leagues in single HTTP request)
          // Other providers use /api/scan/leagues (requires countryIds, pass empty for providers like Tipsport)
          const isBetano = providerCode === "betano";
          const endpoint = isBetano ? "/api/scan/full" : "/api/scan/leagues";
          const body = isBetano
            ? { providerId: provId }
            : { providerId: provId, countryIds: [] }; // Empty countryIds = scan all countries

          const res = await fetch(`${API_URL}${endpoint}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
          });

          if (!res.ok) {
            const error = await res.json();
            throw new Error(error.error || `Scan failed for provider ${provider?.name || provId}`);
          }

          const result = await res.json() as ScanJobResponse;
          results.push(result);
        }

        return results; // Return full array for multi-provider scan
      }

      // Original single-provider flow for Countries and Seasons
      let endpoint = "";
      let body = {};

      switch (entityType) {
        case "Countries":
          endpoint = "/api/scan/countries";
          body = { providerId: providerId || BET_EXPLORER_PROVIDER_ID };
          break;
        case "Leagues":
          // This should not be reached - Leagues scan requires provider selection above
          throw new Error("League scan requires selecting at least one betting provider");
        case "Seasons":
          endpoint = "/api/scan/seasons";
          body = {
            providerId: providerId || BET_EXPLORER_PROVIDER_ID,
            leagueIds: entityIds,
          };
          break;
      }

      const res = await fetch(`${API_URL}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.error || "Scan failed");
      }

      return res.json() as Promise<ScanJobResponse>;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["sync-jobs"] });

      // Handle both single job and array of jobs
      if (onSuccess) {
        if (Array.isArray(data)) {
          // Multi-provider: call onSuccess for each job
          data.forEach(job => onSuccess(job.jobId));
        } else {
          // Single-provider: call once
          onSuccess(data.jobId);
        }
      }

      setTimeout(() => {
        setOpen(false);
        scanMutation.reset();
        setSelectedProviderIds([]);
      }, 2000);
    },
  });

  const getEntityLabel = () => {
    switch (entityType) {
      case "Countries":
        return "země";
      case "Leagues":
        return "ligy";
      case "Seasons":
        return "sezóny";
      default:
        return "entity";
    }
  };

  const getDescription = () => {
    const label = getEntityLabel();
    if (entityIds.length > 0) {
      return `Spustit scan pro vybrané ${label} (${entityIds.length})?`;
    }
    return `Spustit scan všech dostupných ${label}?`;
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {trigger || (
          <Button variant="default">
            <ScanLine className="mr-2 h-4 w-4" />
            Scan {getEntityLabel()}
          </Button>
        )}
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Scan {getEntityLabel()}</DialogTitle>
          <DialogDescription>{getDescription()}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {entityIds.length > 0 && (
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">Počet položek:</span>
              <Badge variant="secondary">{entityIds.length}</Badge>
            </div>
          )}

          {/* Provider selection for Leagues */}
          {entityType === "Leagues" && (
            <div className="space-y-3 p-4 border rounded-lg bg-muted/50">
              <Label className="text-sm font-semibold">
                Vyberte betting providery:
              </Label>
              {providersLoading ? (
                <div className="text-sm text-muted-foreground">
                  Načítám providery...
                </div>
              ) : bettingProviders && bettingProviders.length > 0 ? (
                <div className="grid grid-cols-2 gap-3">
                  {bettingProviders.map((provider) => (
                    <Button
                      key={provider.id}
                      type="button"
                      variant={selectedProviderIds.includes(provider.id) ? "default" : "outline"}
                      className="h-auto py-3 px-4 justify-start"
                      onClick={() => {
                        if (selectedProviderIds.includes(provider.id)) {
                          setSelectedProviderIds(
                            selectedProviderIds.filter((id) => id !== provider.id)
                          );
                        } else {
                          setSelectedProviderIds([...selectedProviderIds, provider.id]);
                        }
                      }}
                    >
                      <ProviderLogo provider={provider} size="sm" className="mr-3" />
                      <span className="font-medium">{provider.name}</span>
                    </Button>
                  ))}
                </div>
              ) : (
                <div className="text-sm text-muted-foreground">
                  Žádní betting providers nejsou k dispozici.
                </div>
              )}
              {selectedProviderIds.length > 0 && (
                <div className="text-xs text-muted-foreground pt-2">
                  Vybráno: {selectedProviderIds.length} provider
                  {selectedProviderIds.length > 1 && "ů"}
                </div>
              )}
            </div>
          )}

          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-sm">
              {entityType === "Leagues" ? (
                <>
                  Scan načte ligy z vybraných betting providerů a spáruje je s BetExplorer
                  (použije databázová mapování nebo automatické párování).
                  Data se uloží do cache pro kontrolu před importem.
                </>
              ) : (
                <>
                  Scan načte data z <strong>{selectedProvider?.name || "BetExplorer"}</strong> do cache tabulek.
                  Data můžete zkontrolovat před importem do hlavní databáze.
                </>
              )}
            </AlertDescription>
          </Alert>

          {scanMutation.isSuccess && (
            <Alert className="bg-green-50 border-green-200">
              <CheckCircle2 className="h-4 w-4 text-green-600" />
              <AlertDescription className="text-green-900">
                {Array.isArray(scanMutation.data) ? (
                  <>
                    Scan úspěšně spuštěn pro {scanMutation.data.length} provider
                    {scanMutation.data.length > 1 && "ů"}!
                    <div className="mt-1 text-xs space-y-1">
                      {scanMutation.data.map((result, idx) => (
                        <div key={idx}>
                          Job {idx + 1}: {result.jobId.slice(0, 8)}...
                        </div>
                      ))}
                    </div>
                  </>
                ) : (
                  <>
                    Scan úspěšně spuštěn! Job ID: {scanMutation.data?.jobId.slice(0, 8)}...
                  </>
                )}
              </AlertDescription>
            </Alert>
          )}

          {scanMutation.isError && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{scanMutation.error.message}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => setOpen(false)}
            disabled={scanMutation.isPending}
          >
            Zrušit
          </Button>
          <Button
            onClick={() => scanMutation.mutate()}
            disabled={
              scanMutation.isPending ||
              scanMutation.isSuccess ||
              (entityType === "Leagues" && selectedProviderIds.length === 0)
            }
          >
            {scanMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {scanMutation.isSuccess && <CheckCircle2 className="mr-2 h-4 w-4" />}
            {!scanMutation.isSuccess && <ScanLine className="mr-2 h-4 w-4" />}
            {scanMutation.isSuccess ? "Spuštěno" : "Spustit scan"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
