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

interface SportProviderDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  sportId: string;
  sportName: string;
  editingMapping?: any; // If provided, dialog is in edit mode
}

export function SportProviderDialog({
  open,
  onOpenChange,
  sportId,
  sportName,
  editingMapping,
}: SportProviderDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    providerId: "",
    providerCode: "",
    isActive: true,
  });

  const isEditMode = !!editingMapping;

  // Load data for edit mode
  useEffect(() => {
    if (editingMapping) {
      setFormData({
        providerId: editingMapping.providerId || "",
        providerCode: editingMapping.providerCode || "",
        isActive: editingMapping.isActive ?? true,
      });
    } else {
      // Reset form for create mode
      setFormData({
        providerId: "",
        providerCode: "",
        isActive: true,
      });
    }
  }, [editingMapping, open]);

  // Fetch available providers (all types - including Scraper and BettingProvider)
  const { data: providers } = useQuery({
    queryKey: ["all-providers"],
    queryFn: () => configApi.getProviders(),
    enabled: open,
  });

  const saveMutation = useMutation({
    mutationFn: async (data: typeof formData) => {
      const url = isEditMode
        ? `${process.env.NEXT_PUBLIC_API_URL}/api/config/sports/${sportId}/providers/${data.providerId}`
        : `${process.env.NEXT_PUBLIC_API_URL}/api/config/sports/${sportId}/providers`;

      const method = isEditMode ? "PATCH" : "POST";

      const response = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || `Failed to ${isEditMode ? "update" : "create"} mapping`);
      }

      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sports"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.providerId || !formData.providerCode) {
      alert("Prosím vyplňte všechna povinná pole");
      return;
    }
    saveMutation.mutate(formData);
  };

  const selectedProvider = providers?.find((p) => p.id === formData.providerId);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {isEditMode ? "Upravit" : "Přidat"} Provider Mapping
          </DialogTitle>
          <DialogDescription>
            {isEditMode ? "Upravte" : "Přidejte"} mapování providera pro sport: {sportName}
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
                {providers?.map((provider) => (
                  <option key={provider.id} value={provider.id}>
                    {provider.name} ({provider.code})
                  </option>
                ))}
              </select>
              {selectedProvider && (
                <p className="text-xs text-gray-500">
                  Base URL: {selectedProvider.baseUrl}
                </p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="providerCode">
                Provider Code *
                <span className="text-xs text-gray-500 ml-2">
                  (např. "fotbal" pro Betano, "football" pro BetExplorer)
                </span>
              </Label>
              <Input
                id="providerCode"
                value={formData.providerCode}
                onChange={(e) =>
                  setFormData({ ...formData, providerCode: e.target.value })
                }
                placeholder="fotbal"
                required
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
