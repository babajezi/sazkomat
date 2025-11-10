"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { mappingApi } from "@/lib/api/client";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ArrowLeft, Plus, Pencil, Trash2, Check, X } from "lucide-react";
import Link from "next/link";
import { LeagueNameMappingDialog } from "@/components/LeagueNameMappingDialog";
import type { LeagueNameMapping } from "@/lib/api/types";

export default function MappingsPage() {
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingMapping, setEditingMapping] = useState<LeagueNameMapping | undefined>(undefined);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  // Filters
  const [providerFilter, setProviderFilter] = useState<string>("");
  const [countryFilter, setCountryFilter] = useState<string>("");
  const [activeFilter, setActiveFilter] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState<string>("");

  // Pagination
  const [page, setPage] = useState(0);
  const itemsPerPage = 20;

  // Fetch mappings
  const { data: mappings, isLoading, error } = useQuery({
    queryKey: ["mappings"],
    queryFn: () => mappingApi.getMappings(),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => mappingApi.deleteMapping(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mappings"] });
      setDeleteConfirmId(null);
    },
  });

  // Filter mappings
  const filteredMappings = (mappings || []).filter((m) => {
    if (providerFilter && m.providerCode !== providerFilter) return false;
    if (countryFilter && m.countryCode !== countryFilter) return false;
    if (activeFilter === "active" && !m.isActive) return false;
    if (activeFilter === "inactive" && m.isActive) return false;
    if (
      searchQuery &&
      !m.providerLeagueName.toLowerCase().includes(searchQuery.toLowerCase()) &&
      !m.betExplorerSlug.toLowerCase().includes(searchQuery.toLowerCase())
    ) {
      return false;
    }
    return true;
  });

  // Calculate statistics
  const stats = {
    total: mappings?.length || 0,
    active: mappings?.filter((m) => m.isActive).length || 0,
    providers: new Set(mappings?.map((m) => m.providerCode)).size || 0,
    countries: new Set(mappings?.map((m) => m.countryCode)).size || 0,
  };

  // Pagination
  const paginatedMappings = filteredMappings.slice(
    page * itemsPerPage,
    (page + 1) * itemsPerPage
  );
  const totalPages = Math.ceil(filteredMappings.length / itemsPerPage);

  // Reset page when filters change
  const handleFilterChange = (setter: (value: string) => void, value: string) => {
    setter(value);
    setPage(0);
  };

  const handleEdit = (mapping: LeagueNameMapping) => {
    setEditingMapping(mapping);
    setDialogOpen(true);
  };

  const handleAdd = () => {
    setEditingMapping(undefined);
    setDialogOpen(true);
  };

  const handleDelete = (id: string) => {
    if (deleteConfirmId === id) {
      deleteMutation.mutate(id);
    } else {
      setDeleteConfirmId(id);
    }
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/">
            <Button variant="outline" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">Mapování Názvů Lig</h1>
            <p className="text-muted-foreground">
              Správa manuálních mapování mezi providery a BetExplorer
            </p>
          </div>
        </div>
        <Button onClick={handleAdd}>
          <Plus className="mr-2 h-4 w-4" />
          Přidat Mapování
        </Button>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Celkem Mapování</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.total}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Aktivní</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.active}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Provideři</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.providers}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Země</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.countries}</div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filtry</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Provider</label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={providerFilter}
                onChange={(e) => handleFilterChange(setProviderFilter, e.target.value)}
              >
                <option value="">Všechny</option>
                <option value="betano">Betano</option>
                <option value="fortuna">Fortuna</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Země</label>
              <Input
                placeholder="Např. cz, sk"
                value={countryFilter}
                onChange={(e) => handleFilterChange(setCountryFilter, e.target.value)}
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Status</label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={activeFilter}
                onChange={(e) => handleFilterChange(setActiveFilter, e.target.value)}
              >
                <option value="all">Všechny</option>
                <option value="active">Aktivní</option>
                <option value="inactive">Neaktivní</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Hledat</label>
              <Input
                placeholder="Název ligy nebo slug"
                value={searchQuery}
                onChange={(e) => handleFilterChange(setSearchQuery, e.target.value)}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Loading/Error States */}
      {isLoading && (
        <div className="text-center py-8">
          <p className="text-muted-foreground">Načítám mapování...</p>
        </div>
      )}

      {error && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-red-600">Chyba při načítání: {(error as Error).message}</p>
          </CardContent>
        </Card>
      )}

      {/* Pagination Top */}
      {!isLoading && !error && totalPages > 1 && (
        <div className="flex justify-between items-center">
          <p className="text-sm text-muted-foreground">
            Zobrazeno {page * itemsPerPage + 1} -{" "}
            {Math.min((page + 1) * itemsPerPage, filteredMappings.length)} z{" "}
            {filteredMappings.length}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              disabled={page === 0}
            >
              Předchozí
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
              disabled={page >= totalPages - 1}
            >
              Další
            </Button>
          </div>
        </div>
      )}

      {/* Mappings Table */}
      {!isLoading && !error && filteredMappings.length > 0 && (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-sm font-medium">Provider</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Země</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Název u Providera</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">BetExplorer Slug</th>
                    <th className="px-4 py-3 text-center text-sm font-medium">Aktivní</th>
                    <th className="px-4 py-3 text-center text-sm font-medium">Priorita</th>
                    <th className="px-4 py-3 text-right text-sm font-medium">Akce</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedMappings.map((mapping) => (
                    <tr key={mapping.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 text-sm">{mapping.providerCode}</td>
                      <td className="px-4 py-3 text-sm uppercase">{mapping.countryCode}</td>
                      <td className="px-4 py-3 text-sm">{mapping.providerLeagueName}</td>
                      <td className="px-4 py-3 text-sm font-mono text-xs">{mapping.betExplorerSlug}</td>
                      <td className="px-4 py-3 text-center">
                        {mapping.isActive ? (
                          <Check className="h-4 w-4 text-green-600 mx-auto" />
                        ) : (
                          <X className="h-4 w-4 text-red-600 mx-auto" />
                        )}
                      </td>
                      <td className="px-4 py-3 text-center text-sm">{mapping.priority}</td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleEdit(mapping)}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleDelete(mapping.id)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2
                              className={`h-4 w-4 ${
                                deleteConfirmId === mapping.id ? "text-red-600" : ""
                              }`}
                            />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {!isLoading && !error && filteredMappings.length === 0 && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-center text-muted-foreground">
              {mappings?.length === 0
                ? "Zatím nejsou žádná mapování. Přidejte první."
                : "Žádná mapování nevyhovují vybraným filtrům."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Pagination Bottom */}
      {!isLoading && !error && totalPages > 1 && (
        <div className="flex justify-between items-center">
          <p className="text-sm text-muted-foreground">
            Stránka {page + 1} z {totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              disabled={page === 0}
            >
              Předchozí
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
              disabled={page >= totalPages - 1}
            >
              Další
            </Button>
          </div>
        </div>
      )}

      {/* Dialog */}
      <LeagueNameMappingDialog
        open={dialogOpen}
        onOpenChange={(open) => {
          setDialogOpen(open);
          if (!open) setEditingMapping(undefined);
        }}
        editingMapping={editingMapping}
      />
    </div>
  );
}
