"use client";

import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { mappingApi } from "@/lib/api/client";
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
import type { LeagueNameMapping } from "@/lib/api/types";

interface LeagueNameMappingDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingMapping?: LeagueNameMapping;
}

export function LeagueNameMappingDialog({
  open,
  onOpenChange,
  editingMapping,
}: LeagueNameMappingDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    providerCode: "",
    countryCode: "",
    providerLeagueName: "",
    betExplorerSlug: "",
    isActive: true,
    notes: "",
    priority: 0,
  });

  const isEditMode = !!editingMapping;

  // Load data for edit mode
  useEffect(() => {
    if (editingMapping) {
      setFormData({
        providerCode: editingMapping.providerCode || "",
        countryCode: editingMapping.countryCode || "",
        providerLeagueName: editingMapping.providerLeagueName || "",
        betExplorerSlug: editingMapping.betExplorerSlug || "",
        isActive: editingMapping.isActive ?? true,
        notes: editingMapping.notes || "",
        priority: editingMapping.priority ?? 0,
      });
    } else {
      // Reset form for create mode
      setFormData({
        providerCode: "",
        countryCode: "",
        providerLeagueName: "",
        betExplorerSlug: "",
        isActive: true,
        notes: "",
        priority: 0,
      });
    }
  }, [editingMapping, open]);

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      mappingApi.createMapping({
        providerCode: data.providerCode,
        countryCode: data.countryCode,
        providerLeagueName: data.providerLeagueName,
        betExplorerSlug: data.betExplorerSlug,
        isActive: data.isActive,
        notes: data.notes || undefined,
        priority: data.priority,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mappings"] });
      onOpenChange(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      mappingApi.updateMapping(editingMapping!.id, {
        providerLeagueName: data.providerLeagueName,
        betExplorerSlug: data.betExplorerSlug,
        isActive: data.isActive,
        notes: data.notes || undefined,
        priority: data.priority,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mappings"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (
      !formData.providerCode ||
      !formData.countryCode ||
      !formData.providerLeagueName ||
      !formData.betExplorerSlug
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
            {isEditMode ? "Upravit" : "Přidat"} Mapování Ligy
          </DialogTitle>
          <DialogDescription>
            {isEditMode
              ? "Upravte mapování názvů lig mezi providerem a BetExplorer"
              : "Přidejte nové mapování názvů lig mezi providerem a BetExplorer"}
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
                <option value="betano">Betano</option>
                <option value="fortuna">Fortuna</option>
              </select>
            </div>

            {/* Country Code */}
            <div className="grid gap-2">
              <Label htmlFor="countryCode">Country Code *</Label>
              <Input
                id="countryCode"
                type="text"
                placeholder="např. cz, sk, gb"
                value={formData.countryCode}
                onChange={(e) =>
                  setFormData({ ...formData, countryCode: e.target.value.toLowerCase() })
                }
                disabled={isEditMode}
                required
                maxLength={10}
              />
              <p className="text-xs text-muted-foreground">
                ISO kód země (malými písmeny)
              </p>
            </div>

            {/* Provider League Name */}
            <div className="grid gap-2">
              <Label htmlFor="providerLeagueName">Název Ligy u Providera *</Label>
              <Input
                id="providerLeagueName"
                type="text"
                placeholder="např. 1. Česko, Chance liga"
                value={formData.providerLeagueName}
                onChange={(e) =>
                  setFormData({ ...formData, providerLeagueName: e.target.value })
                }
                required
                maxLength={200}
              />
              <p className="text-xs text-muted-foreground">
                Název ligy jak se objevuje u providera
              </p>
            </div>

            {/* BetExplorer Slug */}
            <div className="grid gap-2">
              <Label htmlFor="betExplorerSlug">BetExplorer Slug *</Label>
              <Input
                id="betExplorerSlug"
                type="text"
                placeholder="např. 1-liga, cup"
                value={formData.betExplorerSlug}
                onChange={(e) =>
                  setFormData({ ...formData, betExplorerSlug: e.target.value })
                }
                required
                maxLength={200}
              />
              <p className="text-xs text-muted-foreground">
                Slug ligy na BetExplorer.com
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
                Nižší číslo = vyšší priorita (pro případy s více mapováními)
              </p>
            </div>

            {/* Active Checkbox */}
            <div className="flex items-center space-x-2">
              <Checkbox
                id="isActive"
                checked={formData.isActive}
                onCheckedChange={(checked) =>
                  setFormData({ ...formData, isActive: checked as boolean })
                }
              />
              <Label
                htmlFor="isActive"
                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
              >
                Aktivní
              </Label>
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
