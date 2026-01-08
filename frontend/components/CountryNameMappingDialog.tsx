"use client";

import { useState, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { countryMappingApi, configApi } from "@/lib/api/client";
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
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import type { CountryNameMapping } from "@/lib/api/types";

interface CountryNameMappingDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingMapping?: CountryNameMapping;
}

export function CountryNameMappingDialog({
  open,
  onOpenChange,
  editingMapping,
}: CountryNameMappingDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    providerCode: "",
    providerCountryName: "",
    betExplorerCode: "",
    isActive: true,
    notes: "",
    priority: 0,
    matchType: "substring" as "exact" | "substring" | "regex",
    isCaseSensitive: false,
    isSpecialCase: false,
    localizedName: "",
  });

  const isEditMode = !!editingMapping;

  // Fetch betting providers
  const { data: bettingProviders } = useQuery({
    queryKey: ["bettingProviders"],
    queryFn: () => configApi.getBettingProviders(),
  });

  // Load data for edit mode
  useEffect(() => {
    if (editingMapping) {
      setFormData({
        providerCode: editingMapping.providerCode || "",
        providerCountryName: editingMapping.providerCountryName || "",
        betExplorerCode: editingMapping.betExplorerCode || "",
        isActive: editingMapping.isActive ?? true,
        notes: editingMapping.notes || "",
        priority: editingMapping.priority ?? 0,
        matchType: editingMapping.matchType || "substring",
        isCaseSensitive: editingMapping.isCaseSensitive ?? false,
        isSpecialCase: editingMapping.isSpecialCase ?? false,
        localizedName: editingMapping.localizedName || "",
      });
    } else {
      // Reset form for create mode
      setFormData({
        providerCode: "",
        providerCountryName: "",
        betExplorerCode: "",
        isActive: true,
        notes: "",
        priority: 0,
        matchType: "substring",
        isCaseSensitive: false,
        isSpecialCase: false,
        localizedName: "",
      });
    }
  }, [editingMapping, open]);

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      countryMappingApi.createMapping({
        providerCode: data.providerCode,
        providerCountryName: data.providerCountryName,
        betExplorerCode: data.betExplorerCode,
        isActive: data.isActive,
        notes: data.notes || undefined,
        priority: data.priority,
        matchType: data.matchType,
        isCaseSensitive: data.isCaseSensitive,
        isSpecialCase: data.isSpecialCase,
        localizedName: data.localizedName || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["country-mappings"] });
      onOpenChange(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      countryMappingApi.updateMapping(editingMapping!.id, {
        providerCountryName: data.providerCountryName,
        betExplorerCode: data.betExplorerCode,
        isActive: data.isActive,
        notes: data.notes || undefined,
        priority: data.priority,
        matchType: data.matchType,
        isCaseSensitive: data.isCaseSensitive,
        isSpecialCase: data.isSpecialCase,
        localizedName: data.localizedName || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["country-mappings"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (
      !formData.providerCode ||
      !formData.providerCountryName ||
      !formData.betExplorerCode
    ) {
      alert("Prosím vyplňte všechna povinná pole");
      return;
    }

    if (isEditMode) {
      updateMutation.mutate(formData);
    } else {
      createMutation.mutate(formData);
    }
  };

  const saveMutation = isEditMode ? updateMutation : createMutation;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {isEditMode ? "Upravit" : "Přidat"} Mapování Země
          </DialogTitle>
          <DialogDescription>
            {isEditMode
              ? "Upravte mapování názvů zemí mezi providerem a BetExplorer"
              : "Přidejte nové mapování názvů zemí mezi providerem a BetExplorer"}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            {/* Provider Code */}
            <div className="grid gap-2">
              <Label htmlFor="providerCode">Provider Code *</Label>
              <select
                id="providerCode"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.providerCode}
                onChange={(e) =>
                  setFormData({ ...formData, providerCode: e.target.value })
                }
                disabled={isEditMode}
                required
              >
                <option value="">-- Vyberte providera --</option>
                {bettingProviders?.map((provider) => (
                  <option key={provider.id} value={provider.code}>
                    {provider.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Provider Country Name */}
            <div className="grid gap-2">
              <Label htmlFor="providerCountryName">Název Země u Providera *</Label>
              <Input
                id="providerCountryName"
                type="text"
                placeholder="např. Česko, Slovensko"
                value={formData.providerCountryName}
                onChange={(e) =>
                  setFormData({ ...formData, providerCountryName: e.target.value })
                }
                required
                maxLength={200}
              />
              <p className="text-xs text-muted-foreground">
                Název země jak se objevuje u providera
              </p>
            </div>

            {/* BetExplorer Code */}
            <div className="grid gap-2">
              <Label htmlFor="betExplorerCode">BetExplorer Code *</Label>
              <Input
                id="betExplorerCode"
                type="text"
                placeholder="např. czech-republic, slovakia"
                value={formData.betExplorerCode}
                onChange={(e) =>
                  setFormData({ ...formData, betExplorerCode: e.target.value })
                }
                required
                maxLength={200}
              />
              <p className="text-xs text-muted-foreground">
                Kód země na BetExplorer.com (malými písmeny, s pomlčkami)
              </p>
            </div>

            {/* Localized Name */}
            <div className="grid gap-2">
              <Label htmlFor="localizedName">Lokalizovaný název</Label>
              <Input
                id="localizedName"
                type="text"
                placeholder="např. Anglie, Německo, Španělsko"
                value={formData.localizedName}
                onChange={(e) =>
                  setFormData({ ...formData, localizedName: e.target.value })
                }
                maxLength={100}
              />
              <p className="text-xs text-muted-foreground">
                Název země v cílovém jazyce (pro zobrazení uživateli)
              </p>
            </div>

            {/* Match Type */}
            <div className="grid gap-2">
              <Label htmlFor="matchType">Typ shody</Label>
              <select
                id="matchType"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.matchType}
                onChange={(e) =>
                  setFormData({ ...formData, matchType: e.target.value as "exact" | "substring" | "regex" })
                }
              >
                <option value="substring">Substring (obsahuje)</option>
                <option value="exact">Exact (přesná shoda)</option>
                <option value="regex">Regex (regulární výraz)</option>
              </select>
              <p className="text-xs text-muted-foreground">
                Jak se má pattern matchovat: substring hledá uvnitř textu, exact vyžaduje přesnou shodu
              </p>
            </div>

            {/* Priority */}
            <div className="grid gap-2">
              <Label htmlFor="priority">Priorita</Label>
              <Input
                id="priority"
                type="number"
                min="0"
                value={formData.priority}
                onChange={(e) =>
                  setFormData({ ...formData, priority: parseInt(e.target.value) || 0 })
                }
              />
              <p className="text-xs text-muted-foreground">
                Vyšší číslo = vyšší priorita (kontroluje se dříve). Doporučeno: 50 standard, 100 pro konflikty, 200 pro special cases
              </p>
            </div>

            {/* Checkboxes row */}
            <div className="flex flex-wrap gap-6">
              {/* Active Checkbox */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) =>
                    setFormData({ ...formData, isActive: e.target.checked })
                  }
                />
                <Label
                  htmlFor="isActive"
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                >
                  Aktivní
                </Label>
              </div>

              {/* Case Sensitive Checkbox */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isCaseSensitive"
                  checked={formData.isCaseSensitive}
                  onChange={(e) =>
                    setFormData({ ...formData, isCaseSensitive: e.target.checked })
                  }
                />
                <Label
                  htmlFor="isCaseSensitive"
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                >
                  Rozlišovat velká/malá
                </Label>
              </div>

              {/* Special Case Checkbox */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isSpecialCase"
                  checked={formData.isSpecialCase}
                  onChange={(e) =>
                    setFormData({ ...formData, isSpecialCase: e.target.checked })
                  }
                />
                <Label
                  htmlFor="isSpecialCase"
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                >
                  Special case (turnaje, zkratky)
                </Label>
              </div>
            </div>

            {/* Notes */}
            <div className="grid gap-2">
              <Label htmlFor="notes">Poznámky</Label>
              <Textarea
                id="notes"
                placeholder="Volitelné poznámky k mapování"
                value={formData.notes}
                onChange={(e) =>
                  setFormData({ ...formData, notes: e.target.value })
                }
                maxLength={500}
                rows={3}
              />
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
              {saveMutation.isPending
                ? "Ukládám..."
                : isEditMode
                ? "Uložit"
                : "Přidat"}
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
