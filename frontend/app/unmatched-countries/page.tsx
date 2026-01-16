"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { unmatchedCountryApi, configApi } from "@/lib/api/client";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  ArrowLeft,
  Check,
  X,
  AlertTriangle,
  Link as LinkIcon,
  Trash2,
  RotateCcw,
  Loader2,
  Ban,
} from "lucide-react";
import Link from "next/link";
import type { UnmatchedCountry } from "@/lib/api/types";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { CountrySearchSelect } from "@/components/CountrySearchSelect";

export default function UnmatchedCountriesPage() {
  const queryClient = useQueryClient();
  const [providerFilter, setProviderFilter] = useState<string>("");
  const [statusFilter, setStatusFilter] = useState<string>("unresolved");
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [page, setPage] = useState(0);
  const itemsPerPage = 25;

  // Map dialog state
  const [mapDialogOpen, setMapDialogOpen] = useState(false);
  const [selectedUnmatched, setSelectedUnmatched] = useState<UnmatchedCountry | null>(null);
  const [selectedCountryId, setSelectedCountryId] = useState<string | null>(null);
  const [mapNotes, setMapNotes] = useState("");

  // Ignore dialog state
  const [ignoreDialogOpen, setIgnoreDialogOpen] = useState(false);
  const [ignoreNotes, setIgnoreNotes] = useState("");

  // Unavailable dialog state
  const [unavailableDialogOpen, setUnavailableDialogOpen] = useState(false);
  const [unavailableNotes, setUnavailableNotes] = useState("");

  // Fetch unmatched countries
  const { data: unmatchedCountries, isLoading, error } = useQuery({
    queryKey: ["unmatched-countries", providerFilter, statusFilter === "unresolved"],
    queryFn: () =>
      unmatchedCountryApi.getAll({
        providerId: providerFilter || undefined,
        unresolvedOnly: statusFilter === "unresolved" ? true : undefined,
      }),
  });

  // Fetch stats
  const { data: stats } = useQuery({
    queryKey: ["unmatched-countries-stats", providerFilter],
    queryFn: () => unmatchedCountryApi.getStats(providerFilter || undefined),
  });

  // Fetch providers for filter
  const { data: providers } = useQuery({
    queryKey: ["betting-providers"],
    queryFn: () => configApi.getBettingProviders(),
  });

  // Fetch all countries for mapping dialog
  const { data: allCountries } = useQuery({
    queryKey: ["all-countries"],
    queryFn: () => configApi.getCountries(),
  });

  // Mutations
  const mapMutation = useMutation({
    mutationFn: (params: { id: string; countryId: string; notes?: string }) =>
      unmatchedCountryApi.resolveAsMap(params.id, params.countryId, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
      setMapDialogOpen(false);
      setSelectedUnmatched(null);
      setSelectedCountryId(null);
      setMapNotes("");
    },
  });

  const ignoreMutation = useMutation({
    mutationFn: (params: { id: string; notes?: string }) =>
      unmatchedCountryApi.resolveAsIgnore(params.id, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
      setIgnoreDialogOpen(false);
      setSelectedUnmatched(null);
      setIgnoreNotes("");
    },
  });

  const unavailableMutation = useMutation({
    mutationFn: (params: { id: string; notes?: string }) =>
      unmatchedCountryApi.resolveAsUnavailable(params.id, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
      setUnavailableDialogOpen(false);
      setSelectedUnmatched(null);
      setUnavailableNotes("");
    },
  });

  const unresolveMutation = useMutation({
    mutationFn: (id: string) => unmatchedCountryApi.unresolve(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => unmatchedCountryApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
    },
  });

  // Filter and search
  const filteredCountries = (unmatchedCountries || []).filter((country) => {
    if (statusFilter === "resolved" && !country.isResolved) return false;
    if (statusFilter === "mapped" && country.resolutionType !== "Mapped") return false;
    if (statusFilter === "ignored" && country.resolutionType !== "Ignored") return false;
    if (statusFilter === "unavailable" && country.resolutionType !== "Unavailable") return false;
    if (
      searchQuery &&
      !country.providerCountryName.toLowerCase().includes(searchQuery.toLowerCase())
    ) {
      return false;
    }
    return true;
  });

  // Pagination
  const paginatedCountries = filteredCountries.slice(
    page * itemsPerPage,
    (page + 1) * itemsPerPage
  );
  const totalPages = Math.ceil(filteredCountries.length / itemsPerPage);

  const handleOpenMapDialog = (country: UnmatchedCountry) => {
    setSelectedUnmatched(country);
    setSelectedCountryId(null);
    setMapNotes("");
    setMapDialogOpen(true);
  };

  const handleOpenIgnoreDialog = (country: UnmatchedCountry) => {
    setSelectedUnmatched(country);
    setIgnoreNotes("");
    setIgnoreDialogOpen(true);
  };

  const handleOpenUnavailableDialog = (country: UnmatchedCountry) => {
    setSelectedUnmatched(country);
    setUnavailableNotes("");
    setUnavailableDialogOpen(true);
  };

  const handleMap = () => {
    if (selectedUnmatched && selectedCountryId) {
      mapMutation.mutate({
        id: selectedUnmatched.id,
        countryId: selectedCountryId,
        notes: mapNotes || undefined,
      });
    }
  };

  const handleIgnore = () => {
    if (selectedUnmatched) {
      ignoreMutation.mutate({
        id: selectedUnmatched.id,
        notes: ignoreNotes || undefined,
      });
    }
  };

  const handleUnavailable = () => {
    if (selectedUnmatched) {
      unavailableMutation.mutate({
        id: selectedUnmatched.id,
        notes: unavailableNotes || undefined,
      });
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
            <h1 className="text-3xl font-bold">Nesparovane Zeme</h1>
            <p className="text-muted-foreground">
              Zeme z betting provideru bez shody v BetExploreru
            </p>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Celkem</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.total || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-yellow-500" />
              Nevyresene
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
              {stats?.unresolved || 0}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <LinkIcon className="h-4 w-4 text-green-500" />
              Namapovane
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {stats?.mapped || 0}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <X className="h-4 w-4 text-gray-500" />
              Ignorovane
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-gray-600">
              {stats?.ignored || 0}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Ban className="h-4 w-4 text-orange-500" />
              Nedostupne
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              {stats?.unavailable || 0}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filtry</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="text-sm font-medium mb-2 block">Provider</label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={providerFilter}
                onChange={(e) => {
                  setProviderFilter(e.target.value);
                  setPage(0);
                }}
              >
                <option value="">Vsechny</option>
                {providers?.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Status</label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={statusFilter}
                onChange={(e) => {
                  setStatusFilter(e.target.value);
                  setPage(0);
                }}
              >
                <option value="unresolved">Nevyresene</option>
                <option value="all">Vsechny</option>
                <option value="mapped">Namapovane</option>
                <option value="ignored">Ignorovane</option>
                <option value="unavailable">Nedostupne</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Hledat</label>
              <Input
                placeholder="Nazev zeme"
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value);
                  setPage(0);
                }}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Loading/Error States */}
      {isLoading && (
        <div className="text-center py-8">
          <p className="text-muted-foreground">Nacitam nesparovane zeme...</p>
        </div>
      )}

      {error && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-red-600">
              Chyba pri nacitani: {(error as Error).message}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Pagination Top */}
      {!isLoading && !error && totalPages > 1 && (
        <div className="flex justify-between items-center">
          <p className="text-sm text-muted-foreground">
            Zobrazeno {page * itemsPerPage + 1} -{" "}
            {Math.min((page + 1) * itemsPerPage, filteredCountries.length)} z{" "}
            {filteredCountries.length}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(0, p - 1))}
              disabled={page === 0}
            >
              Predchozi
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
              disabled={page >= totalPages - 1}
            >
              Dalsi
            </Button>
          </div>
        </div>
      )}

      {/* Countries Table */}
      {!isLoading && !error && filteredCountries.length > 0 && (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Provider
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Nazev Zeme
                    </th>
                    <th className="px-4 py-3 text-center text-sm font-medium">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Namapovano na
                    </th>
                    <th className="px-4 py-3 text-right text-sm font-medium">
                      Akce
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedCountries.map((country) => (
                    <tr key={country.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 text-sm">
                        {country.providerName || "—"}
                      </td>
                      <td className="px-4 py-3 text-sm font-medium">
                        {country.providerCountryName}
                        {country.providerSlug && (
                          <div className="text-xs text-muted-foreground font-mono">
                            {country.providerSlug}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {!country.isResolved ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-yellow-100 text-yellow-800 text-xs">
                            <AlertTriangle className="h-3 w-3" />
                            Nevyreseno
                          </span>
                        ) : country.resolutionType === "Mapped" ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-green-100 text-green-800 text-xs">
                            <LinkIcon className="h-3 w-3" />
                            Namapovano
                          </span>
                        ) : country.resolutionType === "Unavailable" ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-orange-100 text-orange-800 text-xs">
                            <Ban className="h-3 w-3" />
                            Nedostupne
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-gray-100 text-gray-800 text-xs">
                            <X className="h-3 w-3" />
                            Ignorovano
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {country.resolvedCountryName ? (
                          <span>
                            {country.resolvedCountryName}
                            {country.resolvedCountryCode && (
                              <span className="text-muted-foreground ml-1">
                                ({country.resolvedCountryCode})
                              </span>
                            )}
                          </span>
                        ) : (
                          "—"
                        )}
                        {country.resolutionNotes && (
                          <div className="text-xs text-muted-foreground">
                            {country.resolutionNotes}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {!country.isResolved ? (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenMapDialog(country)}
                                title="Namapovat na existujici zemi"
                              >
                                <LinkIcon className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenIgnoreDialog(country)}
                                title="Ignorovat"
                              >
                                <X className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenUnavailableDialog(country)}
                                title="Nedostupne v BetExploreru"
                                className="text-orange-600 hover:text-orange-700"
                              >
                                <Ban className="h-4 w-4" />
                              </Button>
                            </>
                          ) : (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => unresolveMutation.mutate(country.id)}
                              disabled={unresolveMutation.isPending}
                              title="Zrusit vyreseni"
                            >
                              <RotateCcw className="h-4 w-4" />
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              if (confirm("Opravdu smazat tento zaznam?")) {
                                deleteMutation.mutate(country.id);
                              }
                            }}
                            disabled={deleteMutation.isPending}
                            title="Smazat"
                          >
                            <Trash2 className="h-4 w-4" />
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
      {!isLoading && !error && filteredCountries.length === 0 && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-center text-muted-foreground">
              {unmatchedCountries?.length === 0
                ? "Zadne nesparovane zeme. Spustte scan zemi z betting providera."
                : "Zadne zeme nevyhovuji vybranym filtrum."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Map Dialog */}
      <Dialog open={mapDialogOpen} onOpenChange={setMapDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Namapovat zemi</DialogTitle>
            <DialogDescription>
              Namapujte <strong>{selectedUnmatched?.providerCountryName}</strong> na
              zemi z BetExploreru.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">
                Vyberte BetExplorer zemi
              </label>
              <CountrySearchSelect
                countries={allCountries || []}
                value={selectedCountryId}
                onValueChange={setSelectedCountryId}
                placeholder="Hledejte podle nazvu, kodu..."
              />
            </div>

            <div>
              <label className="text-sm font-medium mb-2 block">
                Poznamky (volitelne)
              </label>
              <Input
                placeholder="Napr. rucni mapovani..."
                value={mapNotes}
                onChange={(e) => setMapNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setMapDialogOpen(false)}>
              Zrusit
            </Button>
            <Button
              onClick={handleMap}
              disabled={!selectedCountryId || mapMutation.isPending}
            >
              {mapMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <Check className="mr-2 h-4 w-4" />
              Namapovat
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Ignore Dialog */}
      <Dialog open={ignoreDialogOpen} onOpenChange={setIgnoreDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Ignorovat zemi</DialogTitle>
            <DialogDescription>
              Zeme <strong>{selectedUnmatched?.providerCountryName}</strong> bude
              oznacena jako ignorovana.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">
                Duvod ignorovani (volitelne)
              </label>
              <Input
                placeholder="Napr. nepodporovana zeme..."
                value={ignoreNotes}
                onChange={(e) => setIgnoreNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIgnoreDialogOpen(false)}>
              Zrusit
            </Button>
            <Button
              variant="destructive"
              onClick={handleIgnore}
              disabled={ignoreMutation.isPending}
            >
              <X className="mr-2 h-4 w-4" />
              Ignorovat
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Unavailable Dialog */}
      <Dialog open={unavailableDialogOpen} onOpenChange={setUnavailableDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nedostupne v BetExploreru</DialogTitle>
            <DialogDescription>
              Zeme <strong>{selectedUnmatched?.providerCountryName}</strong> bude
              oznacena jako nedostupna v BetExploreru.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">
                Poznamka (volitelne)
              </label>
              <Input
                placeholder="Napr. BetExplorer nepodporuje..."
                value={unavailableNotes}
                onChange={(e) => setUnavailableNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setUnavailableDialogOpen(false)}>
              Zrusit
            </Button>
            <Button
              onClick={handleUnavailable}
              disabled={unavailableMutation.isPending}
              className="bg-orange-600 hover:bg-orange-700"
            >
              <Ban className="mr-2 h-4 w-4" />
              Oznacit jako nedostupne
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
