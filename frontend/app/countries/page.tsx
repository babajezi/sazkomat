"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { configApi } from "@/lib/api/client";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import Link from "next/link";
import { EditCountryDialog } from "@/components/CountryFormDialog";
import { CountryProviderDialog } from "@/components/CountryProviderDialog";
import { PaginationControls } from "@/components/PaginationControls";
import { CountryFlag } from "@/components/CountryFlag";
import { getCountryDisplayName } from "@/lib/utils/country";
import type { Country, CountryProvider } from "@/lib/api/types";
import { ProviderType } from "@/lib/api/types";

export default function CountriesPage() {
  const queryClient = useQueryClient();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedCountry, setSelectedCountry] = useState<Country | null>(null);
  const [providerDialogOpen, setProviderDialogOpen] = useState(false);
  const [editingProviderMapping, setEditingProviderMapping] = useState<CountryProvider | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [filterActive, setFilterActive] = useState<string>("");
  const [filterProviderId, setFilterProviderId] = useState<string>("");
  const [filterHasProviders, setFilterHasProviders] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState<string>("");

  const { data: countries, isLoading, error } = useQuery({
    queryKey: ["countries"],
    queryFn: () => configApi.getCountries(),
  });

  const { data: providers } = useQuery({
    queryKey: ["allProviders"],
    queryFn: () => configApi.getProviders(),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => configApi.deleteCountry(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      configApi.updateCountry(id, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
    },
  });

  const toggleProviderSyncMutation = useMutation({
    mutationFn: ({
      countryId,
      providerId,
      isActive,
    }: {
      countryId: string;
      providerId: string;
      isActive: boolean;
    }) => configApi.toggleCountryProviderSync(countryId, providerId, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
    },
  });

  const deleteProviderMappingMutation = useMutation({
    mutationFn: (mappingId: string) => configApi.deleteCountryProvider(mappingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["countries"] });
    },
  });

  const handleEdit = (country: Country) => {
    setSelectedCountry(country);
    setEditDialogOpen(true);
  };

  const handleDelete = async (country: Country) => {
    if (
      window.confirm(
        `Opravdu chcete smazat zemi "${getCountryDisplayName(country)}"? Tato akce je nevratná.`
      )
    ) {
      deleteMutation.mutate(country.id);
    }
  };

  const handleToggleActive = (country: Country, checked: boolean) => {
    toggleActiveMutation.mutate({ id: country.id, isActive: checked });
  };

  const handleToggleProviderSync = (
    country: Country,
    providerId: string,
    checked: boolean
  ) => {
    // Validate: Cannot enable sync if country is not active
    if (checked && !country.isActive) {
      alert(
        `Nelze aktivovat synchronizaci pro neaktivní zemi "${getCountryDisplayName(country)}". Prosím nejprve aktivujte zemi.`
      );
      return;
    }

    toggleProviderSyncMutation.mutate({
      countryId: country.id,
      providerId,
      isActive: checked,
    });
  };

  const handleAddProvider = (country: Country) => {
    setSelectedCountry(country);
    setEditingProviderMapping(null);
    setProviderDialogOpen(true);
  };

  const handleEditProviderMapping = (country: Country, mapping: CountryProvider) => {
    setSelectedCountry(country);
    setEditingProviderMapping(mapping);
    setProviderDialogOpen(true);
  };

  const handleDeleteProviderMapping = async (mapping: CountryProvider) => {
    if (
      window.confirm(
        `Opravdu chcete smazat provider mapping "${mapping.providerName}"? Tato akce je nevratná.`
      )
    ) {
      deleteProviderMappingMutation.mutate(mapping.id);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-destructive">Chyba</CardTitle>
          </CardHeader>
          <CardContent>
            <p>Nelze načíst data: {(error as Error).message}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Filter countries based on selected filters
  const filteredCountries = countries?.filter((country) => {
    // Filtr podle názvu (search query)
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesName = country.name.toLowerCase().includes(query);
      const matchesNameCs = country.nameCs?.toLowerCase().includes(query);
      const matchesCode = country.code.toLowerCase().includes(query);
      if (!matchesName && !matchesNameCs && !matchesCode) return false;
    }

    // Filtr podle stavu
    if (filterActive === "active" && !country.isActive) return false;
    if (filterActive === "inactive" && country.isActive) return false;

    // Filtr podle konkrétního providera
    if (filterProviderId) {
      const hasProvider = country.countryProviders?.some(
        (cp) => cp.providerId === filterProviderId
      );
      if (!hasProvider) return false;
    }

    // Filtr podle existence providerů
    if (filterHasProviders === "yes") {
      if (!country.countryProviders || country.countryProviders.length === 0)
        return false;
    }
    if (filterHasProviders === "no") {
      if (country.countryProviders && country.countryProviders.length > 0)
        return false;
    }

    return true;
  });

  // Pagination - client-side slice
  const paginatedCountries = filteredCountries?.slice(
    page * pageSize,
    (page + 1) * pageSize
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Konfigurace Zemí</h1>
            <p className="text-gray-600">
              Spravujte číselník zemí pro sportovní ligy
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        {/* Statistics */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-6">
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold">{countries?.length || 0}</div>
              <p className="text-xs text-muted-foreground">Celkem zemí</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-green-600">
                {countries?.filter(c => c.isActive).length || 0}
              </div>
              <p className="text-xs text-muted-foreground">Aktivní země</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-blue-600">
                {countries?.filter(c => c.countryProviders && c.countryProviders.length > 0).length || 0}
              </div>
              <p className="text-xs text-muted-foreground">S provider mappings</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-orange-600">
                {countries?.reduce((sum, c) => sum + (c.countryProviders?.length || 0), 0) || 0}
              </div>
              <p className="text-xs text-muted-foreground">Celkem mappingů</p>
            </CardContent>
          </Card>
        </div>

        {/* Filters */}
        <Card className="mb-6">
          <CardHeader>
            <CardTitle className="text-lg">Vyhledávání a Filtry</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 gap-4 mb-4">
              {/* Search Bar */}
              <div className="grid gap-2">
                <Label htmlFor="search-query">Vyhledat podle názvu nebo kódu</Label>
                <Input
                  id="search-query"
                  type="text"
                  placeholder="Zadejte název země nebo kód..."
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value);
                    setPage(0);
                  }}
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {/* Filtr: Stav */}
              <div className="grid gap-2">
                <Label htmlFor="filter-active">Stav</Label>
                <select
                  id="filter-active"
                  value={filterActive}
                  onChange={(e) => setFilterActive(e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <option value="">Všechny země</option>
                  <option value="active">Pouze aktivní</option>
                  <option value="inactive">Pouze neaktivní</option>
                </select>
              </div>

              {/* Filtr: Provider */}
              <div className="grid gap-2">
                <Label htmlFor="filter-provider">Provider</Label>
                <select
                  id="filter-provider"
                  value={filterProviderId}
                  onChange={(e) => setFilterProviderId(e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <option value="">Všichni provideři</option>
                  {(providers?.filter(p => p.type === ProviderType.Scraper) || []).length > 0 && (
                    <optgroup label="Scraper">
                      {providers?.filter(p => p.type === ProviderType.Scraper).map((provider) => (
                        <option key={provider.id} value={provider.id}>
                          {provider.name}
                        </option>
                      ))}
                    </optgroup>
                  )}
                  {(providers?.filter(p => p.type === ProviderType.API) || []).length > 0 && (
                    <optgroup label="API">
                      {providers?.filter(p => p.type === ProviderType.API).map((provider) => (
                        <option key={provider.id} value={provider.id}>
                          {provider.name}
                        </option>
                      ))}
                    </optgroup>
                  )}
                  {(providers?.filter(p => p.type === ProviderType.Manual) || []).length > 0 && (
                    <optgroup label="Manual">
                      {providers?.filter(p => p.type === ProviderType.Manual).map((provider) => (
                        <option key={provider.id} value={provider.id}>
                          {provider.name}
                        </option>
                      ))}
                    </optgroup>
                  )}
                  {(providers?.filter(p => p.type === ProviderType.BettingProvider) || []).length > 0 && (
                    <optgroup label="Betting Provider">
                      {providers?.filter(p => p.type === ProviderType.BettingProvider).map((provider) => (
                        <option key={provider.id} value={provider.id}>
                          {provider.name}
                        </option>
                      ))}
                    </optgroup>
                  )}
                </select>
              </div>

              {/* Filtr: Má providery */}
              <div className="grid gap-2">
                <Label htmlFor="filter-has-providers">Má providery</Label>
                <select
                  id="filter-has-providers"
                  value={filterHasProviders}
                  onChange={(e) => setFilterHasProviders(e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <option value="">Vše</option>
                  <option value="yes">Ano (≥1 provider)</option>
                  <option value="no">Ne (0 providerů)</option>
                </select>
              </div>

              {/* Clear filters button */}
              <div className="grid gap-2 items-end">
                <Button
                  variant="outline"
                  onClick={() => {
                    setSearchQuery("");
                    setFilterActive("");
                    setFilterProviderId("");
                    setFilterHasProviders("");
                    setPage(0);
                  }}
                  className="w-full"
                >
                  Zrušit filtry
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Pagination Controls - Top */}
        {filteredCountries && filteredCountries.length > 0 && (
          <PaginationControls
            page={page}
            pageSize={pageSize}
            totalCount={countries?.length || 0}
            displayedCount={filteredCountries.length}
            itemName="zemí"
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(0);
            }}
            className="mb-6"
          />
        )}

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredCountries && filteredCountries.length === 0 && (
            <Card className="col-span-full">
              <CardContent className="p-6 text-center text-gray-500">
                Žádné země nenalezeny pro zvolený filtr.
              </CardContent>
            </Card>
          )}

          {paginatedCountries?.map((country) => (
            <Card key={country.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="flex items-center gap-2 text-xl">
                      <CountryFlag isoCode={country.isoCode} className="text-3xl" />
                      {getCountryDisplayName(country)}
                    </CardTitle>
                    <CardDescription className="mt-1">
                      Kód: {country.code}
                      {country.nameCs && country.nameCs !== country.name && (
                        <span className="text-muted-foreground ml-2">({country.name})</span>
                      )}
                    </CardDescription>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant={country.isActive ? "default" : "outline"}
                      size="sm"
                      onClick={() => handleToggleActive(country, !country.isActive)}
                      disabled={toggleActiveMutation.isPending}
                      title={country.isActive ? "Deaktivovat" : "Aktivovat"}
                    >
                      {toggleActiveMutation.isPending
                        ? "..."
                        : country.isActive
                        ? "✓ Aktivní"
                        : "○ Neaktivní"}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEdit(country)}
                    >
                      Upravit
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDelete(country)}
                      disabled={deleteMutation.isPending}
                    >
                      {deleteMutation.isPending ? "..." : "Smazat"}
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="text-sm font-semibold">Provider Mappings:</div>
                  {country.countryProviders && country.countryProviders.length > 0 ? (
                    <div className="space-y-1">
                      {country.countryProviders.map((cp) => (
                        <div
                          key={cp.id}
                          className="flex items-center justify-between text-sm p-2 bg-gray-50 rounded"
                        >
                          <div>
                            <span className="font-medium">{cp.provider?.name || "Unknown"}</span>
                            <span className="text-gray-500 ml-2">({cp.providerCode})</span>
                          </div>
                          <div className="flex gap-2 items-center">
                            {cp.isActive ? (
                              <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">
                                Active
                              </span>
                            ) : (
                              <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">
                                Inactive
                              </span>
                            )}
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleEditProviderMapping(country, cp)}
                            >
                              Upravit
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              onClick={() => handleDeleteProviderMapping(cp)}
                              disabled={deleteProviderMappingMutation.isPending}
                            >
                              Smazat
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-gray-500">Žádné provider mappings</p>
                  )}
                  <Button
                    size="sm"
                    variant="outline"
                    className="w-full mt-2"
                    onClick={() => handleAddProvider(country)}
                  >
                    + Přidat Provider
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      <EditCountryDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        country={selectedCountry}
      />

      <CountryProviderDialog
        open={providerDialogOpen}
        onOpenChange={setProviderDialogOpen}
        countryId={selectedCountry?.id || ""}
        countryName={selectedCountry?.name || ""}
        editingMapping={editingProviderMapping || undefined}
      />
    </div>
  );
}
