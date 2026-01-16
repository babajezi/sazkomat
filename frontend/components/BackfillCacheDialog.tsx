"use client";

import { useState, useEffect, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { ProviderLogo } from "@/components/ProviderLogo";
import { Loader2, RefreshCw } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { DataProvider } from "@/lib/api/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";

interface BackfillCacheDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  selectedProviderId?: string;
}

interface ProviderSelection {
  providerId: string;
  providerName: string;
  providerType: string;
  countries: boolean;
  leagues: boolean;
}

interface BackfillResult {
  providerId: string;
  providerName: string;
  type: "countries" | "leagues";
  created: number;
  updated: number;
  error?: string;
}

export function BackfillCacheDialog({
  open,
  onOpenChange,
  selectedProviderId,
}: BackfillCacheDialogProps) {
  const queryClient = useQueryClient();
  const [selections, setSelections] = useState<ProviderSelection[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [results, setResults] = useState<BackfillResult[]>([]);

  // Fetch all providers
  const { data: providers = [] } = useQuery<DataProvider[]>({
    queryKey: ["providers"],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/providers`);
      if (!res.ok) throw new Error("Failed to fetch providers");
      return res.json();
    },
  });

  // Initialize selections when dialog opens or providers change
  useEffect(() => {
    if (open && providers.length > 0) {
      const activeProviders = providers.filter((p) => p.isActive);
      const initialSelections: ProviderSelection[] = activeProviders.map((p) => ({
        providerId: p.id,
        providerName: p.name,
        providerType: p.type,
        countries: p.id === selectedProviderId,
        leagues: p.id === selectedProviderId,
      }));
      setSelections(initialSelections);
      setResults([]);
    }
  }, [open, providers, selectedProviderId]);

  // Group providers by type
  const groupedProviders = useMemo(() => {
    const dataProviders = selections.filter(
      (s) => s.providerType === "Scraper" || s.providerType === "Reference"
    );
    const bettingProviders = selections.filter(
      (s) => s.providerType === "BettingProvider"
    );
    return { dataProviders, bettingProviders };
  }, [selections]);

  // Check if all countries/leagues are selected
  const allCountriesSelected = selections.every((s) => s.countries);
  const allLeaguesSelected = selections.every((s) => s.leagues);
  const hasAnySelection = selections.some((s) => s.countries || s.leagues);

  const toggleProvider = (
    providerId: string,
    field: "countries" | "leagues"
  ) => {
    setSelections((prev) =>
      prev.map((s) =>
        s.providerId === providerId ? { ...s, [field]: !s[field] } : s
      )
    );
  };

  const toggleAllCountries = () => {
    const newValue = !allCountriesSelected;
    setSelections((prev) =>
      prev.map((s) => ({ ...s, countries: newValue }))
    );
  };

  const toggleAllLeagues = () => {
    const newValue = !allLeaguesSelected;
    setSelections((prev) => prev.map((s) => ({ ...s, leagues: newValue })));
  };

  const runBackfill = async () => {
    setIsRunning(true);
    setResults([]);
    const newResults: BackfillResult[] = [];

    // Process each selected provider
    for (const selection of selections) {
      const provider = providers.find((p) => p.id === selection.providerId);
      if (!provider) continue;

      // Backfill countries if selected
      if (selection.countries) {
        try {
          const response = await fetch(
            `${API_URL}/api/scan/backfill-provider-countries`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ providerId: selection.providerId }),
            }
          );
          const data = await response.json();
          if (!response.ok) {
            throw new Error(data.error || "Failed");
          }
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "countries",
            created: data.created,
            updated: data.updated,
          });
        } catch (error) {
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "countries",
            created: 0,
            updated: 0,
            error: error instanceof Error ? error.message : "Unknown error",
          });
        }
      }

      // Backfill leagues if selected
      if (selection.leagues) {
        try {
          const response = await fetch(
            `${API_URL}/api/scan/backfill-provider-leagues`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ providerId: selection.providerId }),
            }
          );
          const data = await response.json();
          if (!response.ok) {
            throw new Error(data.error || "Failed");
          }
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "leagues",
            created: data.created,
            updated: data.updated,
          });
        } catch (error) {
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "leagues",
            created: 0,
            updated: 0,
            error: error instanceof Error ? error.message : "Unknown error",
          });
        }
      }
    }

    setResults(newResults);
    setIsRunning(false);

    // Refresh cache tables
    queryClient.invalidateQueries({ queryKey: ["provider-cache"] });
    queryClient.invalidateQueries({ queryKey: ["provider-countries"] });
    queryClient.invalidateQueries({ queryKey: ["provider-leagues"] });
  };

  const renderProviderRow = (selection: ProviderSelection) => {
    const provider = providers.find((p) => p.id === selection.providerId);
    if (!provider) return null;

    return (
      <tr key={selection.providerId} className="border-b last:border-b-0">
        <td className="py-2 px-2">
          <div className="flex items-center gap-2">
            <ProviderLogo provider={provider} size="sm" />
            <span className="text-sm font-medium">{provider.name}</span>
          </div>
        </td>
        <td className="py-2 px-4 text-center">
          <Checkbox
            checked={selection.countries}
            onChange={() =>
              toggleProvider(selection.providerId, "countries")
            }
            disabled={isRunning}
          />
        </td>
        <td className="py-2 px-4 text-center">
          <Checkbox
            checked={selection.leagues}
            onChange={() =>
              toggleProvider(selection.providerId, "leagues")
            }
            disabled={isRunning}
          />
        </td>
      </tr>
    );
  };

  const totalCreated = results.reduce((sum, r) => sum + r.created, 0);
  const totalUpdated = results.reduce((sum, r) => sum + r.updated, 0);
  const hasErrors = results.some((r) => r.error);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <RefreshCw className="h-5 w-5" />
            Backfill Cache
          </DialogTitle>
          <DialogDescription>
            Doplni provider cache z vyresenych nespárovanych zaznamu
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Select all options */}
          <div className="flex gap-6 pb-2 border-b">
            <label className="flex items-center gap-2 cursor-pointer">
              <Checkbox
                checked={allCountriesSelected}
                onChange={toggleAllCountries}
                disabled={isRunning}
              />
              <span className="text-sm">Vsechny zeme</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <Checkbox
                checked={allLeaguesSelected}
                onChange={toggleAllLeagues}
                disabled={isRunning}
              />
              <span className="text-sm">Vsechny ligy</span>
            </label>
          </div>

          {/* Data Providers */}
          {groupedProviders.dataProviders.length > 0 && (
            <div>
              <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
                Data Providers
              </h4>
              <table className="w-full table-fixed">
                <thead>
                  <tr className="text-xs text-muted-foreground border-b">
                    <th className="text-left py-1 px-2 w-auto">Provider</th>
                    <th className="text-center py-1 px-4 w-16">Zeme</th>
                    <th className="text-center py-1 px-4 w-16">Ligy</th>
                  </tr>
                </thead>
                <tbody>
                  {groupedProviders.dataProviders.map(renderProviderRow)}
                </tbody>
              </table>
            </div>
          )}

          {/* Betting Providers */}
          {groupedProviders.bettingProviders.length > 0 && (
            <div>
              <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
                Betting Providers
              </h4>
              <table className="w-full table-fixed">
                <thead>
                  <tr className="text-xs text-muted-foreground border-b">
                    <th className="text-left py-1 px-2 w-auto">Provider</th>
                    <th className="text-center py-1 px-4 w-16">Zeme</th>
                    <th className="text-center py-1 px-4 w-16">Ligy</th>
                  </tr>
                </thead>
                <tbody>
                  {groupedProviders.bettingProviders.map(renderProviderRow)}
                </tbody>
              </table>
            </div>
          )}

          {/* Results */}
          {results.length > 0 && (
            <div className="mt-4 p-3 rounded-lg bg-muted/50">
              <h4 className="text-sm font-semibold mb-2">Vysledky:</h4>
              <div className="space-y-1 text-sm">
                {results.map((r, idx) => (
                  <div
                    key={idx}
                    className={`flex justify-between ${
                      r.error ? "text-red-600" : ""
                    }`}
                  >
                    <span>
                      {r.providerName} ({r.type === "countries" ? "zeme" : "ligy"})
                    </span>
                    {r.error ? (
                      <span className="text-red-600">{r.error}</span>
                    ) : (
                      <span className="text-green-600">
                        +{r.created} / ~{r.updated}
                      </span>
                    )}
                  </div>
                ))}
              </div>
              <div className="mt-2 pt-2 border-t text-sm font-medium">
                Celkem: {totalCreated} vytvoreno, {totalUpdated} aktualizovano
                {hasErrors && (
                  <span className="text-red-600 ml-2">(s chybami)</span>
                )}
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isRunning}
          >
            {results.length > 0 ? "Zavrit" : "Zrusit"}
          </Button>
          <Button
            onClick={runBackfill}
            disabled={!hasAnySelection || isRunning}
          >
            {isRunning && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isRunning ? "Zpracovavam..." : "Spustit Backfill"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
