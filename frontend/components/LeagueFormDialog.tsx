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
import { Checkbox } from "@/components/ui/checkbox";
import { CountryFlag } from "@/components/CountryFlag";
import { getLeagueDisplayName } from "@/lib/utils/league";
import type { League, Sport, Country } from "@/lib/api/types";

interface EditLeagueDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  league: League | null;
  sports: Sport[];
  countries: Country[];
}

export function EditLeagueDialog({
  open,
  onOpenChange,
  league,
  sports,
  countries,
}: EditLeagueDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    name: "",
    nameCs: "",
    displayName: "",
    betExplorerSlug: "",
    isSyncEnabled: true,
    isBettable: true,
    priority: 1,
    notes: "",
  });

  // Update form data when league changes
  useEffect(() => {
    if (league) {
      setFormData({
        name: league.name || "",
        nameCs: league.nameCs || "",
        displayName: league.displayName || "",
        betExplorerSlug: league.betExplorerSlug || "",
        isSyncEnabled: league.isSyncEnabled ?? true,
        isBettable: league.isBettable ?? true,
        priority: league.priority || 1,
        notes: league.notes || "",
      });
    }
  }, [league]);

  const updateMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      configApi.updateLeague(league!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      onOpenChange(false);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate(formData);
  };

  if (!league) return null;

  const getSportName = () => sports.find((s) => s.id === league.sportId)?.name || "Unknown";
  const getCountry = () => countries.find((c) => c.id === league.countryId);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Upravit ligu</DialogTitle>
          <DialogDescription>
            Upravte nastavení ligy {getLeagueDisplayName(league)}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label>Sport</Label>
              <div className="px-3 py-2 bg-gray-50 rounded-md border border-gray-200 text-gray-600">
                {getSportName()}
              </div>
              <p className="text-xs text-gray-500">Sport nemůže být změněn po vytvoření ligy</p>
            </div>

            <div className="grid gap-2">
              <Label>Země</Label>
              <div className="px-3 py-2 bg-gray-50 rounded-md border border-gray-200 text-gray-600 flex items-center gap-2">
                {getCountry() ? (
                  <>
                    <CountryFlag isoCode={getCountry()!.isoCode} className="text-base" />
                    <span>{getCountry()!.name}</span>
                  </>
                ) : (
                  <span>Unknown</span>
                )}
              </div>
              <p className="text-xs text-gray-500">Země nemůže být změněna po vytvoření ligy</p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-name">Název (anglicky) *</Label>
              <Input
                id="edit-name"
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                placeholder="Premier League"
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-nameCs">Název (česky)</Label>
              <Input
                id="edit-nameCs"
                value={formData.nameCs || ""}
                onChange={(e) =>
                  setFormData({ ...formData, nameCs: e.target.value })
                }
                placeholder="Anglická Premier League"
              />
              <p className="text-xs text-gray-500">Preferovaný název pro zobrazení</p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-displayName">Zobrazovaný název *</Label>
              <Input
                id="edit-displayName"
                value={formData.displayName}
                onChange={(e) =>
                  setFormData({ ...formData, displayName: e.target.value })
                }
                placeholder="Anglická Premier League"
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-betExplorerSlug">BetExplorer Slug *</Label>
              <Input
                id="edit-betExplorerSlug"
                value={formData.betExplorerSlug}
                onChange={(e) =>
                  setFormData({ ...formData, betExplorerSlug: e.target.value })
                }
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-priority">Priorita</Label>
              <Input
                id="edit-priority"
                type="number"
                value={formData.priority}
                onChange={(e) =>
                  setFormData({ ...formData, priority: parseInt(e.target.value) })
                }
                min="1"
              />
            </div>

            <div className="grid gap-2">
              <Checkbox
                id="edit-isSyncEnabled"
                label="Povolit synchronizaci"
                checked={formData.isSyncEnabled}
                onChange={(e) =>
                  setFormData({ ...formData, isSyncEnabled: e.target.checked })
                }
              />
            </div>

            <div className="grid gap-2">
              <Checkbox
                id="edit-isBettable"
                label="Sázkově aktivní"
                checked={formData.isBettable}
                onChange={(e) =>
                  setFormData({ ...formData, isBettable: e.target.checked })
                }
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="edit-notes">Poznámky</Label>
              <Input
                id="edit-notes"
                value={formData.notes || ""}
                onChange={(e) =>
                  setFormData({ ...formData, notes: e.target.value })
                }
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
