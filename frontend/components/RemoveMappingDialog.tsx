"use client";

import { useState, useEffect, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { ProviderLogo } from "@/components/ProviderLogo";
import { Loader2, Trash2 } from "lucide-react";
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

interface RemoveMappingDialogProps {
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

interface RemoveResult {
  providerId: string;
  providerName: string;
  type: "countries" | "leagues";
  deleted: number;
  error?: string;
}

export function RemoveMappingDialog({
  open,
  onOpenChange,
  selectedProviderId,
}: RemoveMappingDialogProps) {
  const queryClient = useQueryClient();
  const [selections, setSelections] = useState<ProviderSelection[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [results, setResults] = useState<RemoveResult[]>([]);

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
        countries: false,
        leagues: false,
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
  const allCountriesSelected = selections.length > 0 && selections.every((s) => s.countries);
  const allLeaguesSelected = selections.length > 0 && selections.every((s) => s.leagues);
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

  const runRemove = async () => {
    setIsRunning(true);
    setResults([]);
    const newResults: RemoveResult[] = [];

    // Process each selected provider
    for (const selection of selections) {
      const provider = providers.find((p) => p.id === selection.providerId);
      if (!provider) continue;

      // Remove country mappings if selected
      if (selection.countries) {
        try {
          const response = await fetch(
            `${API_URL}/api/config/country-providers/by-provider`,
            {
              method: "DELETE",
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
            deleted: data.deleted,
          });
        } catch (error) {
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "countries",
            deleted: 0,
            error: error instanceof Error ? error.message : "Unknown error",
          });
        }
      }

      // Remove league mappings if selected
      if (selection.leagues) {
        try {
          const response = await fetch(
            `${API_URL}/api/config/league-providers/by-provider`,
            {
              method: "DELETE",
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
            deleted: data.deleted,
          });
        } catch (error) {
          newResults.push({
            providerId: selection.providerId,
            providerName: provider.name,
            type: "leagues",
            deleted: 0,
            error: error instanceof Error ? error.message : "Unknown error",
          });
        }
      }
    }

    setResults(newResults);
    setIsRunning(false);

    // Refresh relevant queries
    queryClient.invalidateQueries({ queryKey: ["leagues"] });
    queryClient.invalidateQueries({ queryKey: ["countries"] });
    queryClient.invalidateQueries({ queryKey: ["provider-cache"] });
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

  const totalDeleted = results.reduce((sum, r) => sum + r.deleted, 0);
  const hasErrors = results.some((r) => r.error);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Trash2 className="h-5 w-5 text-destructive" />
            Remove Mappings
          </DialogTitle>
          <DialogDescription>
            Smazat mapovani provideru na zeme a ligy (CountryProvider, LeagueProvider)
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
                      <span className="text-orange-600">
                        -{r.deleted} smazano
                      </span>
                    )}
                  </div>
                ))}
              </div>
              <div className="mt-2 pt-2 border-t text-sm font-medium">
                Celkem smazano: {totalDeleted} mapovani
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
            variant="destructive"
            onClick={runRemove}
            disabled={!hasAnySelection || isRunning}
          >
            {isRunning && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isRunning ? "Mazani..." : "Smazat Mappings"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
