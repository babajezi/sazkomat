"use client";

import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { configApi } from "@/lib/api/client";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { LeagueProvider } from "@/lib/api/types";
import { ProviderType } from "@/lib/api/types";

interface LeagueProviderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  leagueId: string;
  leagueName: string;
  editingMapping?: LeagueProvider;
}

export function LeagueProviderDialog({
  open,
  onOpenChange,
  leagueId,
  leagueName,
  editingMapping,
}: LeagueProviderDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    providerId: "",
    providerSlug: "",
    providerName: "",
    isActive: true,
  });

  const isEditMode = !!editingMapping;

  // Load data for edit mode
  useEffect(() => {
    if (editingMapping) {
      setFormData({
        providerId: editingMapping.providerId || "",
        providerSlug: editingMapping.providerSlug || "",
        providerName: editingMapping.providerName || "",
        isActive: editingMapping.isActive ?? true,
      });
    } else {
      // Reset form for create mode
      setFormData({
        providerId: "",
        providerSlug: "",
        providerName: "",
        isActive: true,
      });
    }
  }, [editingMapping, open]);

  // Fetch available providers
  const { data: providers } = useQuery({
    queryKey: ["bettingProviders"],
    queryFn: () => configApi.getBettingProviders(),
    enabled: open,
  });

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      configApi.createLeagueProvider({
        leagueId,
        providerId: data.providerId,
        providerSlug: data.providerSlug,
        providerName: data.providerName || undefined,
        isActive: data.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      onOpenChange(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      configApi.updateLeagueProvider(editingMapping!.id, {
        providerSlug: data.providerSlug,
        providerName: data.providerName || undefined,
        isActive: data.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.providerId || !formData.providerSlug) {
      alert("Prosím vyplňte všechna povinná pole");
      return;
    }

    if (isEditMode) {
      updateMutation.mutate(formData);
    } else {
      createMutation.mutate(formData);
    }
  };

  const selectedProvider = providers?.find((p) => p.id === formData.providerId);
  const saveMutation = isEditMode ? updateMutation : createMutation;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {isEditMode ? "Upravit" : "Přidat"} Provider Mapping
          </DialogTitle>
          <DialogDescription>
            {isEditMode ? "Upravte" : "Přidejte"} mapování providera pro ligu: {leagueName}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="provider">Provider *</Label>
              <select
                id="provider"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.providerId}
                onChange={(e) =>
                  setFormData({ ...formData, providerId: e.target.value })
                }
                disabled={isEditMode}
                required
              >
                <option value="">-- Vyberte providera --</option>
                {(providers?.filter(p => p.type === ProviderType.Scraper) || []).length > 0 && (
                  <optgroup label="Scraper">
                    {providers?.filter(p => p.type === ProviderType.Scraper).map((provider) => (
                      <option key={provider.id} value={provider.id}>
                        {provider.name} ({provider.code})
                      </option>
                    ))}
                  </optgroup>
                )}
                {(providers?.filter(p => p.type === ProviderType.API) || []).length > 0 && (
                  <optgroup label="API">
                    {providers?.filter(p => p.type === ProviderType.API).map((provider) => (
                      <option key={provider.id} value={provider.id}>
                        {provider.name} ({provider.code})
                      </option>
                    ))}
                  </optgroup>
                )}
                {(providers?.filter(p => p.type === ProviderType.Manual) || []).length > 0 && (
                  <optgroup label="Manual">
                    {providers?.filter(p => p.type === ProviderType.Manual).map((provider) => (
                      <option key={provider.id} value={provider.id}>
                        {provider.name} ({provider.code})
                      </option>
                    ))}
                  </optgroup>
                )}
                {(providers?.filter(p => p.type === ProviderType.BettingProvider) || []).length > 0 && (
                  <optgroup label="Betting Provider">
                    {providers?.filter(p => p.type === ProviderType.BettingProvider).map((provider) => (
                      <option key={provider.id} value={provider.id}>
                        {provider.name} ({provider.code})
                      </option>
                    ))}
                  </optgroup>
                )}
              </select>
              {selectedProvider && (
                <p className="text-xs text-gray-500">
                  Base URL: {selectedProvider.baseUrl}
                </p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="providerSlug">
                Provider Slug *
                <span className="text-xs text-gray-500 ml-2">
                  (např. "premier-league" pro BetExplorer)
                </span>
              </Label>
              <Input
                id="providerSlug"
                value={formData.providerSlug}
                onChange={(e) =>
                  setFormData({ ...formData, providerSlug: e.target.value })
                }
                placeholder="premier-league"
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="providerName">
                Provider Name
                <span className="text-xs text-gray-500 ml-2">(volitelné)</span>
              </Label>
              <Input
                id="providerName"
                value={formData.providerName}
                onChange={(e) =>
                  setFormData({ ...formData, providerName: e.target.value })
                }
                placeholder="Premier League"
              />
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) =>
                  setFormData({ ...formData, isActive: e.target.checked })
                }
                className="h-4 w-4"
              />
              <Label htmlFor="isActive" className="cursor-pointer">
                Aktivní
              </Label>
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Zrušit
            </Button>
            <Button type="submit" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? "Ukládám..." : isEditMode ? "Uložit" : "Přidat"}
            </Button>
          </DialogFooter>

          {saveMutation.isError && (
            <div className="mt-2 text-sm text-red-600">
              Chyba: {(saveMutation.error as Error).message}
            </div>
          )}
        </form>
      </DialogContent>
    </Dialog>
  );
}
