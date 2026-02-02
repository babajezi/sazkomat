"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { recipeApi } from "@/lib/api/client";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { RecipeFormDialog } from "@/components/RecipeFormDialog";
import { TestRecipeDialog } from "@/components/TestRecipeDialog";
import {
  Plus,
  Pencil,
  Trash2,
  PlayCircle,
  Search,
  CheckCircle2,
  XCircle,
  ArrowUpDown,
  BarChart3,
  RefreshCw,
  FileCode,
} from "lucide-react";
import type { RecipeListItem, ScraperRecipe } from "@/lib/api/types";

export default function RecipesPage() {
  const queryClient = useQueryClient();

  // State
  const [searchQuery, setSearchQuery] = useState("");
  const [filterProvider, setFilterProvider] = useState<string>("all");
  const [filterActive, setFilterActive] = useState<string>("all");
  const [sortBy, setSortBy] = useState<"priority" | "name" | "successRate">("priority");
  const [sortDesc, setSortDesc] = useState(false);

  // Dialog states
  const [formDialogOpen, setFormDialogOpen] = useState(false);
  const [formDialogMode, setFormDialogMode] = useState<"create" | "edit">("create");
  const [selectedRecipe, setSelectedRecipe] = useState<ScraperRecipe | null>(null);
  const [testDialogOpen, setTestDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [recipeToDelete, setRecipeToDelete] = useState<RecipeListItem | null>(null);

  // Queries
  const { data: recipes, isLoading, refetch } = useQuery({
    queryKey: ["recipes"],
    queryFn: () => recipeApi.getAll(),
  });

  // Mutations
  const deleteMutation = useMutation({
    mutationFn: (id: string) => recipeApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recipes"] });
      setDeleteDialogOpen(false);
      setRecipeToDelete(null);
    },
  });

  // Handlers
  const handleCreate = () => {
    setSelectedRecipe(null);
    setFormDialogMode("create");
    setFormDialogOpen(true);
  };

  const handleEdit = async (recipe: RecipeListItem) => {
    // Fetch full recipe details
    const fullRecipe = await recipeApi.getById(recipe.id);
    setSelectedRecipe(fullRecipe);
    setFormDialogMode("edit");
    setFormDialogOpen(true);
  };

  const handleTest = async (recipe: RecipeListItem) => {
    const fullRecipe = await recipeApi.getById(recipe.id);
    setSelectedRecipe(fullRecipe);
    setTestDialogOpen(true);
  };

  const handleDelete = (recipe: RecipeListItem) => {
    setRecipeToDelete(recipe);
    setDeleteDialogOpen(true);
  };

  const confirmDelete = () => {
    if (recipeToDelete) {
      deleteMutation.mutate(recipeToDelete.id);
    }
  };

  // Filtering and sorting
  const filteredRecipes = recipes
    ?.filter((recipe) => {
      // Search
      if (
        searchQuery &&
        !recipe.name.toLowerCase().includes(searchQuery.toLowerCase()) &&
        !recipe.description?.toLowerCase().includes(searchQuery.toLowerCase())
      ) {
        return false;
      }
      // Provider filter
      if (filterProvider !== "all" && recipe.provider !== filterProvider) {
        return false;
      }
      // Active filter
      if (filterActive === "active" && !recipe.isActive) return false;
      if (filterActive === "inactive" && recipe.isActive) return false;

      return true;
    })
    .sort((a, b) => {
      let cmp = 0;
      switch (sortBy) {
        case "priority":
          cmp = a.priority - b.priority;
          break;
        case "name":
          cmp = a.name.localeCompare(b.name);
          break;
        case "successRate":
          cmp = a.successRate - b.successRate;
          break;
      }
      return sortDesc ? -cmp : cmp;
    });

  // Get unique providers for filter
  const providers = [...new Set(recipes?.map((r) => r.provider) || [])];

  // Statistics
  const totalRecipes = recipes?.length || 0;
  const activeRecipes = recipes?.filter((r) => r.isActive).length || 0;
  const totalAttempts = recipes?.reduce((sum, r) => sum + r.totalAttempts, 0) || 0;
  const avgSuccessRate =
    recipes && recipes.length > 0
      ? recipes.reduce((sum, r) => sum + r.successRate, 0) / recipes.length
      : 0;

  const toggleSort = (field: typeof sortBy) => {
    if (sortBy === field) {
      setSortDesc(!sortDesc);
    } else {
      setSortBy(field);
      setSortDesc(false);
    }
  };

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold">Scraping Recipes</h1>
          <p className="text-gray-500">
            Správa konfigurovatelných receptů pro web scraping
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Obnovit
          </Button>
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Nový recept
          </Button>
        </div>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-gray-500">
              Celkem receptů
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalRecipes}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-gray-500">
              Aktivních
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{activeRecipes}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-gray-500">
              Celkem pokusů
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalAttempts}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-gray-500">
              Průměrná úspěšnost
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {avgSuccessRate.toFixed(1)}%
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-4 items-center">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Hledat recepty..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
            <Select value={filterProvider} onValueChange={setFilterProvider}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Provider" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Všechny providery</SelectItem>
                {providers.map((p) => (
                  <SelectItem key={p} value={p}>
                    {p}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={filterActive} onValueChange={setFilterActive}>
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Všechny</SelectItem>
                <SelectItem value="active">Aktivní</SelectItem>
                <SelectItem value="inactive">Neaktivní</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <div className="text-center py-8 text-gray-500">Načítání...</div>
          ) : filteredRecipes?.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              {recipes?.length === 0
                ? "Zatím nejsou vytvořeny žádné recepty"
                : "Žádné recepty neodpovídají filtrům"}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleSort("name")}
                      className="-ml-3"
                    >
                      Název
                      <ArrowUpDown className="ml-2 h-4 w-4" />
                    </Button>
                  </TableHead>
                  <TableHead>Provider / Typ</TableHead>
                  <TableHead>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleSort("priority")}
                      className="-ml-3"
                    >
                      Priorita
                      <ArrowUpDown className="ml-2 h-4 w-4" />
                    </Button>
                  </TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleSort("successRate")}
                      className="-ml-3"
                    >
                      Úspěšnost
                      <ArrowUpDown className="ml-2 h-4 w-4" />
                    </Button>
                  </TableHead>
                  <TableHead>Pokusy</TableHead>
                  <TableHead className="text-right">Akce</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredRecipes?.map((recipe) => (
                  <TableRow key={recipe.id}>
                    <TableCell>
                      <div>
                        <div className="font-medium">{recipe.name}</div>
                        {recipe.description && (
                          <div className="text-sm text-gray-500">
                            {recipe.description}
                          </div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Badge variant="outline">{recipe.provider}</Badge>
                        <Badge variant="secondary">{recipe.pageType}</Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{recipe.priority}</Badge>
                    </TableCell>
                    <TableCell>
                      {recipe.isActive ? (
                        <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
                          <CheckCircle2 className="h-3 w-3 mr-1" />
                          Aktivní
                        </Badge>
                      ) : (
                        <Badge variant="secondary">
                          <XCircle className="h-3 w-3 mr-1" />
                          Neaktivní
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div
                          className={`w-16 h-2 rounded-full bg-gray-200 overflow-hidden`}
                        >
                          <div
                            className={`h-full ${
                              recipe.successRate >= 80
                                ? "bg-green-500"
                                : recipe.successRate >= 50
                                ? "bg-yellow-500"
                                : "bg-red-500"
                            }`}
                            style={{ width: `${recipe.successRate}%` }}
                          />
                        </div>
                        <span className="text-sm text-gray-600">
                          {recipe.successRate.toFixed(0)}%
                        </span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">
                        {recipe.successfulAttempts}/{recipe.totalAttempts}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleTest(recipe)}
                          title="Testovat"
                        >
                          <PlayCircle className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEdit(recipe)}
                          title="Upravit"
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(recipe)}
                          className="text-red-600 hover:text-red-700 hover:bg-red-50"
                          title="Smazat"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <RecipeFormDialog
        open={formDialogOpen}
        onOpenChange={setFormDialogOpen}
        recipe={selectedRecipe}
        mode={formDialogMode}
      />

      {/* Test Dialog */}
      <TestRecipeDialog
        open={testDialogOpen}
        onOpenChange={setTestDialogOpen}
        recipe={selectedRecipe}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Smazat recept?</AlertDialogTitle>
            <AlertDialogDescription>
              Opravdu chcete smazat recept &quot;{recipeToDelete?.name}&quot;? Tato akce je
              nevratná.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Zrušit</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-red-600 hover:bg-red-700"
            >
              {deleteMutation.isPending ? "Mazání..." : "Smazat"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
