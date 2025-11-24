"use client";

import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
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
import type { Country } from "@/lib/api/types";

interface EditCountryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  country: Country | null;
}

export function EditCountryDialog({
  open,
  onOpenChange,
  country,
}: EditCountryDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    name: "",
    nameCs: "",
    code: "",
    flagEmoji: "",
  });

  // Update form data when country changes
  useEffect(() => {
    if (country) {
      setFormData({
        name: country.name || "",
        nameCs: country.nameCs || "",
        code: country.code || "",
        flagEmoji: country.flagEmoji || "",
      });
    }
  }, [country]);

  const updateMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      configApi.updateCountry(country!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate(formData);
  };

  if (!country) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Upravit zemi</DialogTitle>
          <DialogDescription>
            Upravte údaje země {country.nameCs || country.name}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="edit-name">Název (anglicky) *</Label>
              <Input
                id="edit-name"
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                placeholder="Czech Republic"
                required
              />
              <p className="text-xs text-gray-500">
                Anglický název země z BetExploreru
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-nameCs">Název (česky)</Label>
              <Input
                id="edit-nameCs"
                value={formData.nameCs}
                onChange={(e) =>
                  setFormData({ ...formData, nameCs: e.target.value })
                }
                placeholder="Česko"
              />
              <p className="text-xs text-gray-500">
                Český název země (primárně zobrazovaný)
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-code">Kód *</Label>
              <Input
                id="edit-code"
                value={formData.code}
                onChange={(e) =>
                  setFormData({ ...formData, code: e.target.value.toUpperCase() })
                }
                placeholder="CZE"
                maxLength={3}
                required
              />
              <p className="text-xs text-gray-500">
                3-písmený kód země (např. CZE, SVK, ENG)
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-flagEmoji">Vlajka (emoji) *</Label>
              <Input
                id="edit-flagEmoji"
                value={formData.flagEmoji}
                onChange={(e) =>
                  setFormData({ ...formData, flagEmoji: e.target.value })
                }
                placeholder="🇨🇿"
                maxLength={4}
                required
              />
              <p className="text-xs text-gray-500">
                Emoji vlajky země (např. 🇨🇿, 🇸🇰, 🏴)
              </p>
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
            <Button type="submit" disabled={updateMutation.isPending}>
              {updateMutation.isPending ? "Ukládání..." : "Uložit"}
            </Button>
          </DialogFooter>

          {updateMutation.isError && (
            <p className="text-sm text-red-600 mt-2">
              Chyba: {(updateMutation.error as Error).message}
            </p>
          )}
        </form>
      </DialogContent>
    </Dialog>
  );
}
