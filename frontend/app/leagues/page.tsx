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
import Link from "next/link";
import { EditLeagueDialog } from "@/components/LeagueFormDialog";
import { LeagueSeasonsDisplay } from "@/components/LeagueSeasonsDisplay";
import type { League } from "@/lib/api/types";

export default function LeaguesPage() {
  const queryClient = useQueryClient();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedLeague, setSelectedLeague] = useState<League | null>(null);
  const [filterSportId, setFilterSportId] = useState<string>("");
  const [filterCountryId, setFilterCountryId] = useState<string>("");
  const [filterEnabled, setFilterEnabled] = useState<string>("");
  const [filterBettable, setFilterBettable] = useState<string>("");

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

  const deleteMutation = useMutation({
    mutationFn: (id: string) => configApi.deleteLeague(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });

  const toggleSyncEnabledMutation = useMutation({
    mutationFn: ({ id, isSyncEnabled }: { id: string; isSyncEnabled: boolean }) =>
      configApi.updateLeague(id, { isSyncEnabled }),
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

  const handleEdit = (league: League) => {
    setSelectedLeague(league);
    setEditDialogOpen(true);
  };

  const handleDelete = async (league: League) => {
    if (
      window.confirm(
        `Opravdu chcete smazat ligu "${league.displayName}"? Tato akce je nevratná.`
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
        `Nelze aktivovat synchronizaci pro neaktivní ligu "${league.displayName}". Prosím nejprve aktivujte ligu.`
      );
      return;
    }

    const country = countries?.find((c) => c.id === league.countryId);
    if (checked && country && !country.isActive) {
      alert(
        `Nelze aktivovat synchronizaci pro ligu "${league.displayName}", protože země "${country.name}" není aktivní. Prosím nejprve aktivujte zemi.`
      );
      return;
    }

    toggleProviderSyncMutation.mutate({
      leagueId: league.id,
      providerId,
      isActive: checked,
    });
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

  const getCountryInfo = (countryId: string) => {
    const country = countries?.find((c) => c.id === countryId);
    return country ? `${country.flagEmoji} ${country.name}` : "Unknown";
  };

  // Filter leagues based on selected filters
  const filteredLeagues = leagues?.filter((league) => {
    if (filterSportId && league.sportId !== filterSportId) return false;
    if (filterCountryId && league.countryId !== filterCountryId) return false;
    if (filterEnabled === "true" && !league.isSyncEnabled) return false;
    if (filterEnabled === "false" && league.isSyncEnabled) return false;
    if (filterBettable === "true" && !league.isBettable) return false;
    if (filterBettable === "false" && league.isBettable) return false;
    return true;
  });

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

        {/* Filters */}
        {sports && countries && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle className="text-lg">Filtry</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="filter-sport">Sport</Label>
                  <select
                    id="filter-sport"
                    value={filterSportId}
                    onChange={(e) => setFilterSportId(e.target.value)}
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
                    onChange={(e) => setFilterCountryId(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value="">Všechny země</option>
                    {countries?.filter((c) => c.isActive).map((country) => (
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
                    onChange={(e) => setFilterEnabled(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value="">Vše</option>
                    <option value="true">Ano</option>
                    <option value="false">Ne</option>
                  </select>
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="filter-bettable">Sázkově aktivní</Label>
                  <select
                    id="filter-bettable"
                    value={filterBettable}
                    onChange={(e) => setFilterBettable(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <option value="">Vše</option>
                    <option value="true">Ano</option>
                    <option value="false">Ne</option>
                  </select>
                </div>

                <div className="grid gap-2 items-end">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setFilterSportId("");
                      setFilterCountryId("");
                      setFilterEnabled("");
                      setFilterBettable("");
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

          {filteredLeagues?.map((league) => (
            <Card key={league.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      {getCountryInfo(league.countryId)} {league.displayName}
                      <Badge variant={league.isSyncEnabled ? "default" : "secondary"}>
                        {league.isSyncEnabled ? "Sync povolen" : "Sync zakázán"}
                      </Badge>
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

                  {/* Provider Sync Section */}
                  {league.leagueProviders && league.leagueProviders.length > 0 && (
                    <div className="pt-3 border-t">
                      <h4 className="text-sm font-semibold mb-3">Synchronizace providerů</h4>
                      <div className="space-y-2">
                        {league.leagueProviders.map((provider) => (
                          <div key={provider.id} className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <Label
                                htmlFor={`provider-${provider.id}`}
                                className="text-sm"
                              >
                                {provider.providerName}
                              </Label>
                              <Badge
                                variant={provider.isActive ? "default" : "outline"}
                                className="text-xs"
                              >
                                {provider.isActive ? "Aktivní" : "Neaktivní"}
                              </Badge>
                            </div>
                            <Switch
                              id={`provider-${provider.id}`}
                              checked={provider.isActive}
                              onCheckedChange={(checked) =>
                                handleToggleProviderSync(league, provider.providerId, checked)
                              }
                              disabled={toggleProviderSyncMutation.isPending}
                            />
                          </div>
                        ))}
                      </div>
                      {!league.isActive && (
                        <p className="text-xs text-amber-600 mt-2">
                          ⚠️ Liga musí být aktivní pro povolení synchronizace
                        </p>
                      )}
                      {league.isActive && league.country && !league.country.isActive && (
                        <p className="text-xs text-amber-600 mt-2">
                          ⚠️ Země musí být aktivní pro povolení synchronizace
                        </p>
                      )}
                    </div>
                  )}
                </div>
                <LeagueSeasonsDisplay leagueId={league.id} />
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
    </div>
  );
}
