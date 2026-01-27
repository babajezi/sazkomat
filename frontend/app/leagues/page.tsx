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
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import Link from "next/link";
import { EditLeagueDialog } from "@/components/LeagueFormDialog";
import { LeagueSeasonsDisplay } from "@/components/LeagueSeasonsDisplay";
import { LeagueProviderDialog } from "@/components/LeagueProviderDialog";
import { PaginationControls } from "@/components/PaginationControls";
import { CountryFlag } from "@/components/CountryFlag";
import { getCountryDisplayName } from "@/lib/utils/country";
import { getLeagueDisplayName } from "@/lib/utils/league";
import { useLanguage } from "@/contexts/UserContext";
import type { League, LeagueProvider } from "@/lib/api/types";
import { ProviderType, BooleanFilterValue, HasProvidersFilter } from "@/lib/api/types";

export default function LeaguesPage() {
  const queryClient = useQueryClient();
  const { language } = useLanguage();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedLeague, setSelectedLeague] = useState<League | null>(null);
  const [providerDialogOpen, setProviderDialogOpen] = useState(false);
  const [editingProviderMapping, setEditingProviderMapping] = useState<LeagueProvider | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [filterSportId, setFilterSportId] = useState<string>("");
  const [filterCountryId, setFilterCountryId] = useState<string>("");
  const [filterEnabled, setFilterEnabled] = useState<string>("");
  const [filterBettable, setFilterBettable] = useState<string>("");
  const [filterHasProviders, setFilterHasProviders] = useState<string>(HasProvidersFilter.All);
  const [filterProviderId, setFilterProviderId] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState<string>("");

  const { data: leagues, isLoading, error } = useQuery({
    queryKey: ["leagues"],
    queryFn: () => configApi.getLeagues(),
  });

  const { data: sports } = useQuery({
    queryKey: ["sports"],
    queryFn: () => configApi.getSports(),
  });

  const { data: countries } = useQuery({
    queryKey: ["countries"],
    queryFn: () => configApi.getCountries(),
  });

  const { data: providers } = useQuery({
    queryKey: ["allProviders"],
    queryFn: () => configApi.getProviders(),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => configApi.deleteLeague(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const toggleBettableMutation = useMutation({
    mutationFn: ({ id, isBettable }: { id: string; isBettable: boolean }) =>
      configApi.updateLeague(id, { isBettable }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      configApi.updateLeague(id, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const toggleProviderSyncMutation = useMutation({
    mutationFn: ({
      leagueId,
      providerId,
      isActive,
    }: {
      leagueId: string;
      providerId: string;
      isActive: boolean;
    }) => configApi.toggleLeagueProviderSync(leagueId, providerId, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const deleteLeagueProviderMappingMutation = useMutation({
    mutationFn: (mappingId: string) => configApi.deleteLeagueProvider(mappingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const handleEdit = (league: League) => {
    setSelectedLeague(league);
    setEditDialogOpen(true);
  };

  const handleDelete = async (league: League) => {
    if (
      window.confirm(
        `Opravdu chcete smazat ligu "${getLeagueDisplayName(league, language)}"? Tato akce je nevratná.`
      )
    ) {
      deleteMutation.mutate(league.id);
    }
  };

  const handleToggleActive = (league: League, checked: boolean) => {
    toggleActiveMutation.mutate({ id: league.id, isActive: checked });
  };

  const handleToggleProviderSync = (
    league: League,
    providerId: string,
    checked: boolean
  ) => {
    // Validate: Cannot enable sync if league or country is not active
    if (checked && !league.isActive) {
      alert(
        `Nelze aktivovat synchronizaci pro neaktivní ligu "${getLeagueDisplayName(league, language)}". Prosím nejprve aktivujte ligu.`
      );
      return;
    }

    const country = countries?.find((c) => c.id === league.countryId);
    if (checked && country && !country.isActive) {
      alert(
        `Nelze aktivovat synchronizaci pro ligu "${getLeagueDisplayName(league, language)}", protože země "${getCountryDisplayName(country, language)}" není aktivní. Prosím nejprve aktivujte zemi.`
      );
      return;
    }

    toggleProviderSyncMutation.mutate({
      leagueId: league.id,
      providerId,
      isActive: checked,
    });
  };

  const handleAddLeagueProvider = (league: League) => {
    setSelectedLeague(league);
    setEditingProviderMapping(null);
    setProviderDialogOpen(true);
  };

  const handleEditLeagueProviderMapping = (league: League, mapping: LeagueProvider) => {
    setSelectedLeague(league);
    setEditingProviderMapping(mapping);
    setProviderDialogOpen(true);
  };

  const handleDeleteLeagueProviderMapping = async (mapping: LeagueProvider) => {
    if (
      window.confirm(
        `Opravdu chcete smazat provider mapping "${mapping.provider?.name || mapping.providerName}" (${mapping.providerSlug})? Tato akce je nevratná.`
      )
    ) {
      deleteLeagueProviderMappingMutation.mutate(mapping.id);
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

  const getSportName = (sportId: string) => {
    return sports?.find((s) => s.id === sportId)?.name || "Unknown";
  };

  const getCountry = (countryId: string) => {
    return countries?.find((c) => c.id === countryId);
  };

  // Filter leagues based on selected filters
  const filteredLeagues = leagues?.filter((league) => {
    // Filtr podle názvu (search query)
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesDisplayName = league.displayName.toLowerCase().includes(query);
      const matchesName = league.name.toLowerCase().includes(query);
      const matchesNameCs = league.nameCs?.toLowerCase().includes(query);
      if (!matchesDisplayName && !matchesName && !matchesNameCs) return false;
    }

    if (filterSportId && league.sportId !== filterSportId) return false;
    if (filterCountryId && league.countryId !== filterCountryId) return false;
    if (filterEnabled === BooleanFilterValue.True && !league.isActive) return false;
    if (filterEnabled === BooleanFilterValue.False && league.isActive) return false;
    if (filterBettable === BooleanFilterValue.True && !league.isBettable) return false;
    if (filterBettable === BooleanFilterValue.False && league.isBettable) return false;

    // Filtr podle existence providerů
    if (filterHasProviders === HasProvidersFilter.Yes) {
      if (!league.leagueProviders || league.leagueProviders.length === 0) return false;
    }
    if (filterHasProviders === HasProvidersFilter.No) {
      if (league.leagueProviders && league.leagueProviders.length > 0) return false;
    }

    // Filtr podle konkrétního providera
    if (filterProviderId) {
      if (!league.leagueProviders || !league.leagueProviders.some(lp => lp.providerId === filterProviderId)) {
        return false;
      }
    }

    return true;
  });

  // Pagination - client-side slice
  const paginatedLeagues = filteredLeagues?.slice(
    page * pageSize,
    (page + 1) * pageSize
  );

  // Reset page when filters change
  const resetPage = () => setPage(0);

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Konfigurace Lig</h1>
            <p className="text-gray-600">
              Spravujte sportovní ligy a jejich nastavení
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
              <div className="text-2xl font-bold">{leagues?.length || 0}</div>
              <p className="text-xs text-muted-foreground">Celkem lig</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-green-600">
                {leagues?.filter(l => l.isActive).length || 0}
              </div>
              <p className="text-xs text-muted-foreground">Aktivních lig</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-blue-600">
                {leagues?.filter(l => l.leagueProviders && l.leagueProviders.length > 0).length || 0}
              </div>
              <p className="text-xs text-muted-foreground">S provider mappings</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="text-2xl font-bold text-orange-600">
                {leagues?.reduce((sum, l) => sum + (l.leagueProviders?.length || 0), 0) || 0}
              </div>
              <p className="text-xs text-muted-foreground">Celkem mappingů</p>
            </CardContent>
          </Card>
        </div>

        {/* Filters */}
        {sports && countries && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle className="text-lg">Vyhledávání a Filtry</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 gap-4 mb-4">
                {/* Search Bar */}
                <div className="grid gap-2">
                  <Label htmlFor="search-query">Vyhledat podle názvu</Label>
                  <Input
                    id="search-query"
                    type="text"
                    placeholder="Zadejte název ligy..."
                    value={searchQuery}
                    onChange={(e) => {
                      setSearchQuery(e.target.value);
                      setPage(0);
                    }}
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-7 gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="filter-sport">Sport</Label>
                  <select
                    id="filter-sport"
                    value={filterSportId}
                    onChange={(e) => { setFilterSportId(e.target.value); setPage(0); }}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value="">Všechny sporty</option>
                    {sports?.map((sport) => (
                      <option key={sport.id} value={sport.id}>
                        {sport.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-country">Země</Label>
                  <select
                    id="filter-country"
                    value={filterCountryId}
                    onChange={(e) => { setFilterCountryId(e.target.value); setPage(0); }}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value="">Všechny země</option>
                    {countries
                      ?.filter((c) => c.isActive)
                      .sort((a, b) => a.name.localeCompare(b.name))
                      .map((country) => (
                        <option key={country.id} value={country.id}>
                          {country.flagEmoji} {country.name}
                        </option>
                      ))}
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-enabled">Aktivní</Label>
                  <select
                    id="filter-enabled"
                    value={filterEnabled}
                    onChange={(e) => { setFilterEnabled(e.target.value); setPage(0); }}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value={BooleanFilterValue.All}>Vše</option>
                    <option value={BooleanFilterValue.True}>Ano</option>
                    <option value={BooleanFilterValue.False}>Ne</option>
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-bettable">Sázkově aktivní</Label>
                  <select
                    id="filter-bettable"
                    value={filterBettable}
                    onChange={(e) => { setFilterBettable(e.target.value); setPage(0); }}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value={BooleanFilterValue.All}>Vše</option>
                    <option value={BooleanFilterValue.True}>Ano</option>
                    <option value={BooleanFilterValue.False}>Ne</option>
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-has-providers">Má providery</Label>
                  <select
                    id="filter-has-providers"
                    value={filterHasProviders}
                    onChange={(e) => { setFilterHasProviders(e.target.value); setPage(0); }}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value={HasProvidersFilter.All}>Vše</option>
                    <option value={HasProvidersFilter.Yes}>Ano (≥1 provider)</option>
                    <option value={HasProvidersFilter.No}>Ne (0 providerů)</option>
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-provider">Provider</Label>
                  <select
                    id="filter-provider"
                    value={filterProviderId}
                    onChange={(e) => { setFilterProviderId(e.target.value); setPage(0); }}
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

                <div className="grid gap-2 items-end">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setSearchQuery("");
                      setFilterSportId("");
                      setFilterCountryId("");
                      setFilterEnabled("");
                      setFilterBettable("");
                      setFilterHasProviders("");
                      setFilterProviderId("");
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
        )}

        {/* Pagination Controls - Top */}
        {filteredLeagues && filteredLeagues.length > 0 && (
          <PaginationControls
            page={page}
            pageSize={pageSize}
            totalCount={filteredLeagues.length}
            displayedCount={filteredLeagues.length}
            itemName="lig"
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(0);
            }}
            className="mb-6"
          />
        )}

        <div className="grid gap-4">
          {filteredLeagues && filteredLeagues.length === 0 && (
            <Card>
              <CardContent className="p-6 text-center text-gray-500">
                {leagues && leagues.length > 0
                  ? "Žádné ligy nevyhovují zvoleným filtrům."
                  : "Žádné ligy nenalezeny. Klikněte na 'Nová liga' pro vytvoření první ligy."}
              </CardContent>
            </Card>
          )}

          {paginatedLeagues?.map((league) => (
            <Card key={league.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      {getCountry(league.countryId) && (
                        <>
                          <CountryFlag isoCode={getCountry(league.countryId)!.isoCode} className="text-xl" />
                          <span>{getCountryDisplayName(getCountry(league.countryId)!, language)}</span>
                        </>
                      )}
                      {!getCountry(league.countryId) && <span>Unknown</span>}
                      {getLeagueDisplayName(league, language)}
                      <Badge variant={league.isActive ? "default" : "outline"}>
                        {league.isActive ? "Aktivní" : "Neaktivní"}
                      </Badge>
                    </CardTitle>
                    <CardDescription>
                      {getSportName(league.sportId)} • Priorita:{" "}
                      {league.priority}
                    </CardDescription>
                  </div>
                  <div className="flex gap-2 flex-wrap">
                    <Button
                      variant={league.isActive ? "default" : "outline"}
                      size="sm"
                      onClick={() => handleToggleActive(league, !league.isActive)}
                      disabled={toggleActiveMutation.isPending}
                      title={league.isActive ? "Deaktivovat ligu" : "Aktivovat ligu"}
                    >
                      {league.isActive ? "✓ Aktivní" : "○ Neaktivní"}
                    </Button>
                    <Button
                      variant={league.isBettable ? "default" : "outline"}
                      size="sm"
                      onClick={() =>
                        toggleBettableMutation.mutate({
                          id: league.id,
                          isBettable: !league.isBettable,
                        })
                      }
                      disabled={toggleBettableMutation.isPending}
                      title={league.isBettable ? "Vypnout sázky" : "Zapnout sázky"}
                    >
                      {league.isBettable ? "$ Sázky" : "○ Bez sázek"}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEdit(league)}
                    >
                      Upravit
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDelete(league)}
                      disabled={deleteMutation.isPending}
                    >
                      {deleteMutation.isPending ? "..." : "Smazat"}
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {/* Basic Info */}
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <span className="font-medium">Název:</span> {league.name}
                    </div>
                    <div>
                      <span className="font-medium">BetExplorer:</span>{" "}
                      {league.betExplorerSlug}
                    </div>
                    <div>
                      <span className="font-medium">Sázkově aktivní:</span>{" "}
                      {league.isBettable ? "Ano" : "Ne"}
                    </div>
                    {league.notes && (
                      <div className="col-span-2">
                        <span className="font-medium">Poznámky:</span>{" "}
                        {league.notes}
                      </div>
                    )}
                  </div>

                  {/* Provider Mappings Section */}
                  <div className="pt-3 border-t space-y-2">
                    <div className="text-sm font-semibold">Provider Mappings:</div>
                    {league.leagueProviders && league.leagueProviders.length > 0 ? (
                      <div className="space-y-1">
                        {league.leagueProviders.map((lp) => (
                          <div
                            key={lp.id}
                            className="flex items-center justify-between text-sm p-2 bg-gray-50 rounded"
                          >
                            <div>
                              <span className="font-medium">{lp.provider?.name || lp.providerName || "Unknown"}</span>
                              <span className="text-gray-500 ml-2">({lp.providerSlug})</span>
                            </div>
                            <div className="flex gap-2 items-center">
                              {lp.isActive ? (
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
                                onClick={() => handleEditLeagueProviderMapping(league, lp)}
                              >
                                Upravit
                              </Button>
                              <Button
                                size="sm"
                                variant="destructive"
                                onClick={() => handleDeleteLeagueProviderMapping(lp)}
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
                    <div className="mt-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleAddLeagueProvider(league)}
                      >
                        + Přidat Provider
                      </Button>
                    </div>
                  </div>
                </div>
                <LeagueSeasonsDisplay leagueId={league.id} leagueProviders={league.leagueProviders} />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      {sports && countries && (
        <EditLeagueDialog
          open={editDialogOpen}
          onOpenChange={setEditDialogOpen}
          league={selectedLeague}
          sports={sports}
          countries={countries}
        />
      )}

      {selectedLeague && (
        <LeagueProviderDialog
          open={providerDialogOpen}
          onOpenChange={setProviderDialogOpen}
          leagueId={selectedLeague.id}
          leagueName={selectedLeague.displayName}
          editingMapping={editingProviderMapping || undefined}
        />
      )}
    </div>
  );
}
