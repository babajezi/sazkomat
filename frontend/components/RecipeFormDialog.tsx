"use client";

import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { recipeApi } from "@/lib/api/client";
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
import type { ScraperRecipe, CreateRecipeRequest, UpdateRecipeRequest } from "@/lib/api/types";

interface RecipeFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  recipe: ScraperRecipe | null;
  mode: "create" | "edit";
}

const DEFAULT_ACTIONS = JSON.stringify([
  { type: "navigate", url: "{baseUrl}{season}/results/" },
  { type: "waitForLoadState", state: "networkidle", timeout: 30000 },
  { type: "extractHtml", selector: "table.table-main" }
], null, 2);

export function RecipeFormDialog({
  open,
  onOpenChange,
  recipe,
  mode,
}: RecipeFormDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    provider: "betexplorer",
    pageType: "results",
    priority: 100,
    isActive: true,
    actionsJson: DEFAULT_ACTIONS,
    roundHeaderSelector: ".//th[contains(text(), 'Round')]",
    groupPatternRegex: "",
    matchRowSelector: ".//tr[td[contains(@class, 'h-text-left')]]",
    oddsCellSelector: "",
  });
  const [jsonError, setJsonError] = useState<string | null>(null);

  // Update form data when recipe changes
  useEffect(() => {
    if (recipe && mode === "edit") {
      setFormData({
        name: recipe.name || "",
        description: recipe.description || "",
        provider: recipe.provider || "betexplorer",
        pageType: recipe.pageType || "results",
        priority: recipe.priority || 100,
        isActive: recipe.isActive ?? true,
        actionsJson: formatJson(recipe.actionsJson) || DEFAULT_ACTIONS,
        roundHeaderSelector: recipe.roundHeaderSelector || "",
        groupPatternRegex: recipe.groupPatternRegex || "",
        matchRowSelector: recipe.matchRowSelector || "",
        oddsCellSelector: recipe.oddsCellSelector || "",
      });
    } else if (mode === "create") {
      setFormData({
        name: "",
        description: "",
        provider: "betexplorer",
        pageType: "results",
        priority: 100,
        isActive: true,
        actionsJson: DEFAULT_ACTIONS,
        roundHeaderSelector: ".//th[contains(text(), 'Round')]",
        groupPatternRegex: "",
        matchRowSelector: ".//tr[td[contains(@class, 'h-text-left')]]",
        oddsCellSelector: "",
      });
    }
    setJsonError(null);
  }, [recipe, mode, open]);

  const createMutation = useMutation({
    mutationFn: (data: CreateRecipeRequest) => recipeApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipes"] });
      onOpenChange(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateRecipeRequest) =>
      recipeApi.update(recipe!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipes"] });
      onOpenChange(false);
    },
  });

  const formatJson = (json: string): string => {
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  };

  const validateJson = (json: string): boolean => {
    try {
      JSON.parse(json);
      setJsonError(null);
      return true;
    } catch (e) {
      setJsonError((e as Error).message);
      return false;
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateJson(formData.actionsJson)) {
      return;
    }

    const requestData = {
      name: formData.name,
      description: formData.description || undefined,
      provider: formData.provider,
      pageType: formData.pageType,
      priority: formData.priority,
      isActive: formData.isActive,
      actionsJson: formData.actionsJson,
      roundHeaderSelector: formData.roundHeaderSelector,
      groupPatternRegex: formData.groupPatternRegex || undefined,
      matchRowSelector: formData.matchRowSelector,
      oddsCellSelector: formData.oddsCellSelector || undefined,
    };

    if (mode === "create") {
      createMutation.mutate(requestData);
    } else {
      updateMutation.mutate(requestData);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-[90vw] w-[1200px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {mode === "create" ? "Vytvořit nový recept" : "Upravit recept"}
          </DialogTitle>
          <DialogDescription>
            {mode === "create"
              ? "Vytvořte nový scraping recept pro automatické načítání dat"
              : `Upravte nastavení receptu ${recipe?.name}`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            {/* Basic Info */}
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="name">Název *</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  placeholder="BetExplorer Full Workflow"
                  required
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="priority">Priorita</Label>
                <Input
                  id="priority"
                  type="number"
                  value={formData.priority}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      priority: parseInt(e.target.value) || 100,
                    })
                  }
                  min="1"
                />
                <p className="text-xs text-gray-500">Nižší = vyšší priorita</p>
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="description">Popis</Label>
              <Input
                id="description"
                value={formData.description}
                onChange={(e) =>
                  setFormData({ ...formData, description: e.target.value })
                }
                placeholder="Sort by round + Show more loop"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="provider">Provider</Label>
                <Input
                  id="provider"
                  value={formData.provider}
                  onChange={(e) =>
                    setFormData({ ...formData, provider: e.target.value })
                  }
                  placeholder="betexplorer"
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="pageType">Typ stránky</Label>
                <Input
                  id="pageType"
                  value={formData.pageType}
                  onChange={(e) =>
                    setFormData({ ...formData, pageType: e.target.value })
                  }
                  placeholder="results"
                />
              </div>
            </div>

            <div className="flex items-center gap-4">
              <Checkbox
                id="isActive"
                label="Aktivní"
                checked={formData.isActive}
                onChange={(e) =>
                  setFormData({ ...formData, isActive: e.target.checked })
                }
              />
            </div>

            {/* Actions JSON */}
            <div className="grid gap-2">
              <Label htmlFor="actionsJson">Akce (JSON) *</Label>
              <Textarea
                id="actionsJson"
                value={formData.actionsJson}
                onChange={(e) => {
                  setFormData({ ...formData, actionsJson: e.target.value });
                  validateJson(e.target.value);
                }}
                className="font-mono text-sm min-h-[400px] resize-y"
                placeholder="[...]"
                required
              />
              {jsonError && (
                <p className="text-sm text-red-600">JSON Error: {jsonError}</p>
              )}
              <p className="text-xs text-gray-500">
                Podporované akce: navigate, click, wait, waitForLoadState, waitForSelector, evaluate, extractHtml
              </p>
            </div>

            {/* Parsing Selectors */}
            <div className="border-t pt-4 mt-2">
              <h4 className="text-sm font-medium mb-3">Parsovací selektory (XPath)</h4>

              <div className="grid gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="roundHeaderSelector">Round Header Selector *</Label>
                  <Input
                    id="roundHeaderSelector"
                    value={formData.roundHeaderSelector}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        roundHeaderSelector: e.target.value,
                      })
                    }
                    placeholder=".//th[contains(text(), 'Round')]"
                    className="font-mono text-sm"
                    required
                  />
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="matchRowSelector">Match Row Selector *</Label>
                  <Input
                    id="matchRowSelector"
                    value={formData.matchRowSelector}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        matchRowSelector: e.target.value,
                      })
                    }
                    placeholder=".//tr[td[contains(@class, 'h-text-left')]]"
                    className="font-mono text-sm"
                    required
                  />
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="groupPatternRegex">Group Pattern Regex</Label>
                  <Input
                    id="groupPatternRegex"
                    value={formData.groupPatternRegex}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        groupPatternRegex: e.target.value,
                      })
                    }
                    placeholder="^(.+?)\s*-\s*(\d+)\.\s*Round$"
                    className="font-mono text-sm"
                  />
                  <p className="text-xs text-gray-500">
                    Pro ligy s groupami (např. &quot;East - 1. Round&quot;)
                  </p>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="oddsCellSelector">Odds Cell Selector</Label>
                  <Input
                    id="oddsCellSelector"
                    value={formData.oddsCellSelector}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        oddsCellSelector: e.target.value,
                      })
                    }
                    placeholder=".//td[@data-odd]"
                    className="font-mono text-sm"
                  />
                </div>
              </div>
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
            <Button type="submit" disabled={isPending || !!jsonError}>
              {isPending
                ? "Ukládání..."
                : mode === "create"
                ? "Vytvořit"
                : "Uložit"}
            </Button>
          </DialogFooter>

          {error && (
            <p className="text-sm text-red-600 mt-2">
              Chyba: {(error as Error).message}
            </p>
          )}
        </form>
      </DialogContent>
    </Dialog>
  );
}
