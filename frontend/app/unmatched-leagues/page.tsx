"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { unmatchedLeagueApi, configApi, betExplorerApi } from "@/lib/api/client";
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
  RefreshCw,
  Ban,
  Copy,
  Info,
} from "lucide-react";
import Link from "next/link";
import type {
  UnmatchedLeague,
  League,
  BetExplorerLeague,
  CopyResolutionMatch,
  CopyResolutionsPreviewResponse,
} from "@/lib/api/types";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { UnmatchedLeagueMappingDialog } from "@/components/UnmatchedLeagueMappingDialog";

export default function UnmatchedLeaguesPage() {
  const queryClient = useQueryClient();
  const [providerFilter, setProviderFilter] = useState<string>("");
  const [statusFilter, setStatusFilter] = useState<string>("unresolved");
  const [countryFilter, setCountryFilter] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [page, setPage] = useState(0);
  const itemsPerPage = 25;

  // Map dialog state
  const [mapDialogOpen, setMapDialogOpen] = useState(false);
  const [selectedUnmatched, setSelectedUnmatched] = useState<UnmatchedLeague | null>(null);
  const [selectedLeagueId, setSelectedLeagueId] = useState<string>("");
  const [mapNotes, setMapNotes] = useState("");
  const [mapTab, setMapTab] = useState<"database" | "betexplorer">("database");
  const [selectedBetExplorerSlug, setSelectedBetExplorerSlug] = useState<string>("");
  const [betExplorerLeagueName, setBetExplorerLeagueName] = useState<string>("");
  const [selectedCountryId, setSelectedCountryId] = useState<string>("");
  const [selectedCountryCode, setSelectedCountryCode] = useState<string>("");

  // Ignore dialog state
  const [ignoreDialogOpen, setIgnoreDialogOpen] = useState(false);
  const [ignoreNotes, setIgnoreNotes] = useState("");

  // Unavailable dialog state
  const [unavailableDialogOpen, setUnavailableDialogOpen] = useState(false);
  const [unavailableNotes, setUnavailableNotes] = useState("");

  // Copy resolutions dialog state
  const [copyDialogOpen, setCopyDialogOpen] = useState(false);
  const [copyDialogStep, setCopyDialogStep] = useState<"select" | "preview">("select");
  const [copySourceProviderId, setCopySourceProviderId] = useState("");
  const [copyTargetProviderId, setCopyTargetProviderId] = useState("");
  const [copyPreviewData, setCopyPreviewData] = useState<CopyResolutionsPreviewResponse | null>(null);

  // Detail mapping dialog state
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  const [detailLeagueId, setDetailLeagueId] = useState<string | null>(null);

  // Fetch unmatched leagues
  const { data: unmatchedLeagues, isLoading, error } = useQuery({
    queryKey: ["unmatched-leagues", providerFilter, statusFilter === "unresolved"],
    queryFn: () =>
      unmatchedLeagueApi.getUnmatchedLeagues({
        providerId: providerFilter || undefined,
        unresolvedOnly: statusFilter === "unresolved" ? true : undefined,
      }),
  });

  // Fetch stats
  const { data: stats } = useQuery({
    queryKey: ["unmatched-leagues-stats"],
    queryFn: () => unmatchedLeagueApi.getStats(),
  });

  // Fetch providers for filter
  const { data: providers } = useQuery({
    queryKey: ["betting-providers"],
    queryFn: () => configApi.getBettingProviders(),
  });

  // Fetch all leagues for mapping dialog
  const { data: allLeagues } = useQuery({
    queryKey: ["all-leagues"],
    queryFn: () => configApi.getLeagues(),
  });

  // Fetch all countries for manual country selection
  const { data: allCountries } = useQuery({
    queryKey: ["all-countries"],
    queryFn: () => configApi.getCountries(),
  });

  // Effective country code - use selected override if set, otherwise use unmatched league's code
  const effectiveCountryCode = selectedCountryCode || selectedUnmatched?.countryCode || "";

  // Fetch BetExplorer leagues when dialog is open and BetExplorer tab is selected
  const {
    data: betExplorerLeagues,
    isLoading: isLoadingBetExplorer,
    isFetching: isFetchingBetExplorer,
    refetch: refetchBetExplorer,
  } = useQuery({
    queryKey: ["betexplorer-leagues", effectiveCountryCode],
    queryFn: () =>
      betExplorerApi.getLeagues(effectiveCountryCode, false),
    enabled: mapDialogOpen && mapTab === "betexplorer" && !!effectiveCountryCode,
    staleTime: 1000 * 60 * 60, // 1 hour cache
  });

  // Force refresh mutation - calls API with forceRefresh=true
  const forceRefreshMutation = useMutation({
    mutationFn: () =>
      betExplorerApi.getLeagues(effectiveCountryCode, true),
    onSuccess: (data) => {
      // Update the query cache with new data
      queryClient.setQueryData(
        ["betexplorer-leagues", effectiveCountryCode],
        data
      );
    },
  });

  // Handler for force refresh
  const handleRefreshBetExplorer = () => {
    forceRefreshMutation.mutate();
  };

  // Mutations
  const mapMutation = useMutation({
    mutationFn: (params: { id: string; leagueId: string; notes?: string }) =>
      unmatchedLeagueApi.resolveAsMap(params.id, params.leagueId, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      queryClient.invalidateQueries({ queryKey: ["all-leagues"] });
      setMapDialogOpen(false);
      setSelectedUnmatched(null);
      setSelectedLeagueId("");
      setMapNotes("");
      setMapTab("database");
      setSelectedCountryId("");
      setSelectedCountryCode("");
    },
  });

  const createFromBetExplorerMutation = useMutation({
    mutationFn: (params: { id: string; betExplorerSlug: string; leagueName?: string; countryId?: string; notes?: string }) =>
      unmatchedLeagueApi.resolveCreateFromBetExplorer(
        params.id,
        params.betExplorerSlug,
        params.leagueName,
        params.countryId,
        params.notes
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      queryClient.invalidateQueries({ queryKey: ["all-leagues"] });
      setMapDialogOpen(false);
      setSelectedUnmatched(null);
      setSelectedBetExplorerSlug("");
      setBetExplorerLeagueName("");
      setMapNotes("");
      setMapTab("database");
      setSelectedCountryId("");
      setSelectedCountryCode("");
    },
  });

  const ignoreMutation = useMutation({
    mutationFn: (params: { id: string; notes?: string }) =>
      unmatchedLeagueApi.resolveAsIgnore(params.id, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      setIgnoreDialogOpen(false);
      setSelectedUnmatched(null);
      setIgnoreNotes("");
    },
  });

  const unavailableMutation = useMutation({
    mutationFn: (params: { id: string; notes?: string }) =>
      unmatchedLeagueApi.resolveAsUnavailable(params.id, params.notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      setUnavailableDialogOpen(false);
      setSelectedUnmatched(null);
      setUnavailableNotes("");
    },
  });

  const unresolveMutation = useMutation({
    mutationFn: (id: string) => unmatchedLeagueApi.unresolve(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => unmatchedLeagueApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
    },
  });

  // Copy resolutions mutations
  const copyPreviewMutation = useMutation({
    mutationFn: (params: { sourceProviderId: string; targetProviderId: string }) =>
      unmatchedLeagueApi.previewCopyResolutions(params.sourceProviderId, params.targetProviderId),
    onSuccess: (data) => {
      setCopyPreviewData(data);
      setCopyDialogStep("preview");
    },
  });

  const copyExecuteMutation = useMutation({
    mutationFn: (params: { sourceProviderId: string; targetProviderId: string }) =>
      unmatchedLeagueApi.executeCopyResolutions(params.sourceProviderId, params.targetProviderId),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      setCopyDialogOpen(false);
      setCopyDialogStep("select");
      setCopySourceProviderId("");
      setCopyTargetProviderId("");
      setCopyPreviewData(null);
      alert(`Kopírování dokončeno! Zkopírováno: ${data.copied}, Přeskočeno: ${data.skipped}, Nenalezeno: ${data.notFound}`);
    },
  });

  // Filter and search
  const filteredLeagues = (unmatchedLeagues || []).filter((league) => {
    if (statusFilter === "resolved" && !league.isResolved) return false;
    if (statusFilter === "mapped" && league.resolutionType !== "Mapped") return false;
    if (statusFilter === "ignored" && league.resolutionType !== "Ignored") return false;
    if (statusFilter === "unavailable" && league.resolutionType !== "Unavailable") return false;
    if (
      countryFilter &&
      !league.countryCode.toLowerCase().includes(countryFilter.toLowerCase()) &&
      !league.countryName?.toLowerCase().includes(countryFilter.toLowerCase())
    ) {
      return false;
    }
    if (
      searchQuery &&
      !league.providerLeagueName.toLowerCase().includes(searchQuery.toLowerCase())
    ) {
      return false;
    }
    return true;
  });

  // Pagination
  const paginatedLeagues = filteredLeagues.slice(
    page * itemsPerPage,
    (page + 1) * itemsPerPage
  );
  const totalPages = Math.ceil(filteredLeagues.length / itemsPerPage);

  // Filter leagues by country for mapping dialog
  // Use manually selected country if set, otherwise try to match by unmatched league's country
  const suggestedLeagues = selectedUnmatched
    ? (allLeagues || []).filter((l) => {
        if (selectedCountryId) {
          return l.countryId === selectedCountryId;
        }
        return (
          l.country?.code?.toLowerCase() === selectedUnmatched.countryCode.toLowerCase() ||
          l.country?.name?.toLowerCase() === selectedUnmatched.countryName?.toLowerCase()
        );
      })
    : [];

  const handleOpenMapDialog = (league: UnmatchedLeague) => {
    setSelectedUnmatched(league);
    setSelectedLeagueId("");
    setSelectedBetExplorerSlug("");
    setBetExplorerLeagueName("");
    setMapNotes("");
    setMapTab("database");
    setSelectedCountryId("");
    setSelectedCountryCode("");
    setMapDialogOpen(true);
  };

  const handleOpenIgnoreDialog = (league: UnmatchedLeague) => {
    setSelectedUnmatched(league);
    setIgnoreNotes("");
    setIgnoreDialogOpen(true);
  };

  const handleOpenUnavailableDialog = (league: UnmatchedLeague) => {
    setSelectedUnmatched(league);
    setUnavailableNotes("");
    setUnavailableDialogOpen(true);
  };

  const handleMap = () => {
    if (selectedUnmatched && selectedLeagueId) {
      mapMutation.mutate({
        id: selectedUnmatched.id,
        leagueId: selectedLeagueId,
        notes: mapNotes || undefined,
      });
    }
  };

  const handleCreateFromBetExplorer = () => {
    if (selectedUnmatched && selectedBetExplorerSlug) {
      createFromBetExplorerMutation.mutate({
        id: selectedUnmatched.id,
        betExplorerSlug: selectedBetExplorerSlug,
        leagueName: betExplorerLeagueName || undefined,
        countryId: selectedCountryId || undefined,
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

  const handleOpenCopyDialog = () => {
    setCopyDialogStep("select");
    setCopySourceProviderId("");
    setCopyTargetProviderId("");
    setCopyPreviewData(null);
    setCopyDialogOpen(true);
  };

  const handleCopyPreview = () => {
    if (copySourceProviderId && copyTargetProviderId) {
      copyPreviewMutation.mutate({
        sourceProviderId: copySourceProviderId,
        targetProviderId: copyTargetProviderId,
      });
    }
  };

  const handleCopyExecute = () => {
    if (copySourceProviderId && copyTargetProviderId) {
      copyExecuteMutation.mutate({
        sourceProviderId: copySourceProviderId,
        targetProviderId: copyTargetProviderId,
      });
    }
  };

  const handleCopyDialogClose = (open: boolean) => {
    if (!open) {
      setCopyDialogStep("select");
      setCopySourceProviderId("");
      setCopyTargetProviderId("");
      setCopyPreviewData(null);
    }
    setCopyDialogOpen(open);
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
            <h1 className="text-3xl font-bold">Nespárované Ligy</h1>
            <p className="text-muted-foreground">
              Ligy z betting providerů bez shody v BetExploreru
            </p>
          </div>
        </div>
        <Button variant="outline" onClick={handleOpenCopyDialog}>
          <Copy className="h-4 w-4 mr-2" />
          Kopírovat řešení
        </Button>
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
              Nevyřešené
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
              Namapované
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
              Ignorované
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
              Nedostupné
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
          <div className="grid gap-4 md:grid-cols-4">
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
                <option value="">Všechny</option>
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
                <option value="unresolved">Nevyřešené</option>
                <option value="all">Všechny</option>
                <option value="mapped">Namapované</option>
                <option value="ignored">Ignorované</option>
                <option value="unavailable">Nedostupné</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Země</label>
              <Input
                placeholder="Kód nebo název země"
                value={countryFilter}
                onChange={(e) => {
                  setCountryFilter(e.target.value);
                  setPage(0);
                }}
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Hledat</label>
              <Input
                placeholder="Název ligy"
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
          <p className="text-muted-foreground">Načítám nespárované ligy...</p>
        </div>
      )}

      {error && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-red-600">
              Chyba při načítání: {(error as Error).message}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Pagination Top */}
      {!isLoading && !error && totalPages > 1 && (
        <div className="flex justify-between items-center">
          <p className="text-sm text-muted-foreground">
            Zobrazeno {page * itemsPerPage + 1} -{" "}
            {Math.min((page + 1) * itemsPerPage, filteredLeagues.length)} z{" "}
            {filteredLeagues.length}
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

      {/* Leagues Table */}
      {!isLoading && !error && filteredLeagues.length > 0 && (
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
                      Název Ligy
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Země
                    </th>
                    <th className="px-4 py-3 text-center text-sm font-medium">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Namapováno na
                    </th>
                    <th className="px-4 py-3 text-right text-sm font-medium">
                      Akce
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedLeagues.map((league) => (
                    <tr key={league.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 text-sm">
                        {league.providerName || "—"}
                      </td>
                      <td className="px-4 py-3 text-sm font-medium">
                        {league.providerLeagueName}
                        {league.providerSlug && (
                          <div className="text-xs text-muted-foreground font-mono">
                            {league.providerSlug}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {league.countryName || league.countryCode}
                        <div className="text-xs text-muted-foreground font-mono uppercase">
                          {league.countryCode}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {!league.isResolved ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-yellow-100 text-yellow-800 text-xs">
                            <AlertTriangle className="h-3 w-3" />
                            Nevyřešeno
                          </span>
                        ) : league.resolutionType === "Mapped" ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-green-100 text-green-800 text-xs">
                            <LinkIcon className="h-3 w-3" />
                            Namapováno
                          </span>
                        ) : league.resolutionType === "Unavailable" ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-orange-100 text-orange-800 text-xs">
                            <Ban className="h-3 w-3" />
                            Nedostupné
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-gray-100 text-gray-800 text-xs">
                            <X className="h-3 w-3" />
                            Ignorováno
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {league.resolvedLeagueName || "—"}
                        {league.resolutionNotes && (
                          <div className="text-xs text-muted-foreground">
                            {league.resolutionNotes}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              setDetailLeagueId(league.id);
                              setDetailDialogOpen(true);
                            }}
                            title="Detail mapování"
                            className="text-blue-600 hover:text-blue-700"
                          >
                            <Info className="h-4 w-4" />
                          </Button>
                          {!league.isResolved ? (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenMapDialog(league)}
                                title="Namapovat na existující ligu"
                              >
                                <LinkIcon className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenIgnoreDialog(league)}
                                title="Ignorovat (nechceme importovat)"
                              >
                                <X className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleOpenUnavailableDialog(league)}
                                title="Nedostupné v BetExploreru"
                                className="text-orange-600 hover:text-orange-700"
                              >
                                <Ban className="h-4 w-4" />
                              </Button>
                            </>
                          ) : (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => unresolveMutation.mutate(league.id)}
                              disabled={unresolveMutation.isPending}
                              title="Zrušit vyřešení"
                            >
                              <RotateCcw className="h-4 w-4" />
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              if (confirm("Opravdu smazat tento záznam?")) {
                                deleteMutation.mutate(league.id);
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
      {!isLoading && !error && filteredLeagues.length === 0 && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-center text-muted-foreground">
              {unmatchedLeagues?.length === 0
                ? "Žádné nespárované ligy. Spusťte scan lig z betting providera."
                : "Žádné ligy nevyhovují vybraným filtrům."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Map Dialog */}
      <Dialog open={mapDialogOpen} onOpenChange={setMapDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Namapovat ligu</DialogTitle>
            <DialogDescription>
              Namapujte <strong>{selectedUnmatched?.providerLeagueName}</strong> na ligu z BetExploreru.
              <br />
              <span className="text-xs">
                Země z providera: {selectedUnmatched?.countryName || selectedUnmatched?.countryCode}
                {selectedCountryId && (
                  <span className="text-green-600 ml-2">
                    → Ručně zvoleno: {allCountries?.find(c => c.id === selectedCountryId)?.name}
                  </span>
                )}
              </span>
            </DialogDescription>
          </DialogHeader>

          {/* Country override selector */}
          <div className="mb-4 p-3 bg-muted/50 rounded-lg">
            <label className="text-sm font-medium mb-2 block">
              Přepsat zemi (pokud se nedetekuje správně)
            </label>
            <select
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={selectedCountryId}
              onChange={(e) => {
                const countryId = e.target.value;
                setSelectedCountryId(countryId);
                const country = allCountries?.find(c => c.id === countryId);
                setSelectedCountryCode(country?.code || "");
                // Reset league selection when country changes
                setSelectedLeagueId("");
                setSelectedBetExplorerSlug("");
                setBetExplorerLeagueName("");
              }}
            >
              <option value="">-- Automaticky ({selectedUnmatched?.countryName || selectedUnmatched?.countryCode}) --</option>
              {(allCountries || [])
                .sort((a, b) => (a.name || "").localeCompare(b.name || ""))
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.flagEmoji} {c.name} ({c.code})
                  </option>
                ))}
            </select>
          </div>

          <Tabs value={mapTab} onValueChange={(v) => setMapTab(v as "database" | "betexplorer")}>
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="database">Z databáze</TabsTrigger>
              <TabsTrigger value="betexplorer">Z BetExploreru</TabsTrigger>
            </TabsList>

            <TabsContent value="database" className="space-y-4 mt-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Vyberte existující ligu ({suggestedLeagues.length} lig ve stejné zemi)
                </label>
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={selectedLeagueId}
                  onChange={(e) => setSelectedLeagueId(e.target.value)}
                >
                  <option value="">-- Vyberte ligu --</option>
                  {suggestedLeagues.length > 0 ? (
                    <optgroup label={`Ligy v ${selectedUnmatched?.countryName || selectedUnmatched?.countryCode}`}>
                      {suggestedLeagues.map((l) => (
                        <option key={l.id} value={l.id}>
                          {l.name} ({l.betExplorerSlug})
                        </option>
                      ))}
                    </optgroup>
                  ) : null}
                  <optgroup label="Všechny ligy">
                    {(allLeagues || [])
                      .filter((l) => !suggestedLeagues.some((s) => s.id === l.id))
                      .slice(0, 100)
                      .map((l) => (
                        <option key={l.id} value={l.id}>
                          {l.name} ({l.country?.name}) - {l.betExplorerSlug}
                        </option>
                      ))}
                  </optgroup>
                </select>
                {suggestedLeagues.length === 0 && (
                  <p className="text-sm text-muted-foreground mt-2">
                    V databázi nemáme žádné ligy pro tuto zemi. Zkuste načíst z BetExploreru.
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Poznámky (volitelné)
                </label>
                <Input
                  placeholder="Např. ruční mapování, ženská liga..."
                  value={mapNotes}
                  onChange={(e) => setMapNotes(e.target.value)}
                />
              </div>

              <DialogFooter>
                <Button variant="outline" onClick={() => setMapDialogOpen(false)}>
                  Zrušit
                </Button>
                <Button
                  onClick={handleMap}
                  disabled={!selectedLeagueId || mapMutation.isPending}
                >
                  {mapMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  <Check className="mr-2 h-4 w-4" />
                  Namapovat
                </Button>
              </DialogFooter>
            </TabsContent>

            <TabsContent value="betexplorer" className="space-y-4 mt-4">
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-sm font-medium">
                    Ligy z BetExploreru pro {selectedCountryId
                      ? allCountries?.find(c => c.id === selectedCountryId)?.name
                      : (selectedUnmatched?.countryName || selectedUnmatched?.countryCode)}
                  </label>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleRefreshBetExplorer}
                    disabled={isFetchingBetExplorer || forceRefreshMutation.isPending}
                    title="Načíst znovu z BetExploreru (smaže cache)"
                  >
                    <RefreshCw className={`h-4 w-4 ${(isFetchingBetExplorer || forceRefreshMutation.isPending) ? "animate-spin" : ""}`} />
                  </Button>
                </div>

                {isLoadingBetExplorer || isFetchingBetExplorer || forceRefreshMutation.isPending ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="h-6 w-6 animate-spin mr-2" />
                    <span className="text-muted-foreground">
                      {forceRefreshMutation.isPending ? "Scrapuji ligy z BetExploreru..." : "Načítám ligy z BetExploreru..."}
                    </span>
                  </div>
                ) : betExplorerLeagues && betExplorerLeagues.length > 0 ? (
                  <>
                    <select
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                      value={selectedBetExplorerSlug}
                      onChange={(e) => {
                        setSelectedBetExplorerSlug(e.target.value);
                        const league = betExplorerLeagues.find(l => l.slug === e.target.value);
                        setBetExplorerLeagueName(league?.name || "");
                      }}
                    >
                      <option value="">-- Vyberte ligu --</option>
                      {betExplorerLeagues.map((l) => (
                        <option key={l.slug} value={l.slug}>
                          {l.name} ({l.slug})
                          {l.fromCache ? " [cache]" : ""}
                        </option>
                      ))}
                    </select>
                    {betExplorerLeagues[0]?.fromCache && (
                      <p className="text-xs text-muted-foreground mt-1">
                        Data z cache. Pro čerstvá data klikněte na refresh.
                      </p>
                    )}
                  </>
                ) : (
                  <div className="text-center py-4 text-muted-foreground">
                    Žádné ligy nenalezeny pro tuto zemi.
                  </div>
                )}
              </div>

              {selectedBetExplorerSlug && (
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Název ligy (lze upravit)
                  </label>
                  <Input
                    value={betExplorerLeagueName}
                    onChange={(e) => setBetExplorerLeagueName(e.target.value)}
                    placeholder="Název ligy"
                  />
                </div>
              )}

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Poznámky (volitelné)
                </label>
                <Input
                  placeholder="Např. nová liga vytvořená z BetExploreru..."
                  value={mapNotes}
                  onChange={(e) => setMapNotes(e.target.value)}
                />
              </div>

              <DialogFooter>
                <Button variant="outline" onClick={() => setMapDialogOpen(false)}>
                  Zrušit
                </Button>
                <Button
                  onClick={handleCreateFromBetExplorer}
                  disabled={!selectedBetExplorerSlug || createFromBetExplorerMutation.isPending}
                >
                  {createFromBetExplorerMutation.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  <Check className="mr-2 h-4 w-4" />
                  Vytvořit a namapovat
                </Button>
              </DialogFooter>
            </TabsContent>
          </Tabs>
        </DialogContent>
      </Dialog>

      {/* Ignore Dialog */}
      <Dialog open={ignoreDialogOpen} onOpenChange={setIgnoreDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Ignorovat ligu</DialogTitle>
            <DialogDescription>
              Liga <strong>{selectedUnmatched?.providerLeagueName}</strong> bude
              označena jako ignorovaná (nechceme ji importovat).
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">
                Důvod ignorování (volitelné)
              </label>
              <Input
                placeholder="Např. ženská liga, mládež, nezajímá nás..."
                value={ignoreNotes}
                onChange={(e) => setIgnoreNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIgnoreDialogOpen(false)}>
              Zrušit
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
            <DialogTitle>Nedostupné v BetExploreru</DialogTitle>
            <DialogDescription>
              Liga <strong>{selectedUnmatched?.providerLeagueName}</strong> bude
              označena jako nedostupná (BetExplorer tuto ligu nepodporuje).
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">
                Poznámka (volitelné)
              </label>
              <Input
                placeholder="Např. exotická liga, BetExplorer nemá data..."
                value={unavailableNotes}
                onChange={(e) => setUnavailableNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setUnavailableDialogOpen(false)}>
              Zrušit
            </Button>
            <Button
              onClick={handleUnavailable}
              disabled={unavailableMutation.isPending}
              className="bg-orange-600 hover:bg-orange-700"
            >
              <Ban className="mr-2 h-4 w-4" />
              Označit jako nedostupné
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Copy Resolutions Dialog */}
      <Dialog open={copyDialogOpen} onOpenChange={handleCopyDialogClose}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Kopírovat řešení mezi providery</DialogTitle>
            <DialogDescription>
              Zkopírujte vyřešená mapování z jednoho betting providera na druhého.
              Kopírují se pouze záznamy, kde cílový provider ještě nemá řešení.
            </DialogDescription>
          </DialogHeader>

          {copyDialogStep === "select" ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Zdrojový provider (kopírovat Z)
                  </label>
                  <select
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={copySourceProviderId}
                    onChange={(e) => setCopySourceProviderId(e.target.value)}
                  >
                    <option value="">-- Vyberte provider --</option>
                    {providers?.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Cílový provider (kopírovat DO)
                  </label>
                  <select
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={copyTargetProviderId}
                    onChange={(e) => setCopyTargetProviderId(e.target.value)}
                  >
                    <option value="">-- Vyberte provider --</option>
                    {providers?.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {copySourceProviderId && copyTargetProviderId && copySourceProviderId === copyTargetProviderId && (
                <p className="text-sm text-red-500">
                  Zdrojový a cílový provider musí být různí.
                </p>
              )}

              <DialogFooter>
                <Button variant="outline" onClick={() => setCopyDialogOpen(false)}>
                  Zrušit
                </Button>
                <Button
                  onClick={handleCopyPreview}
                  disabled={
                    !copySourceProviderId ||
                    !copyTargetProviderId ||
                    copySourceProviderId === copyTargetProviderId ||
                    copyPreviewMutation.isPending
                  }
                >
                  {copyPreviewMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Náhled
                </Button>
              </DialogFooter>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex justify-between items-center text-sm">
                <span>
                  <strong>Zdroj:</strong> {providers?.find(p => p.id === copySourceProviderId)?.name}
                  {" → "}
                  <strong>Cíl:</strong> {providers?.find(p => p.id === copyTargetProviderId)?.name}
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setCopyDialogStep("select")}
                >
                  Změnit
                </Button>
              </div>

              <div className="p-3 bg-muted rounded-lg text-sm">
                <div className="grid grid-cols-3 gap-4 text-center">
                  <div>
                    <div className="text-2xl font-bold text-green-600">
                      {copyPreviewData?.matches.length || 0}
                    </div>
                    <div className="text-muted-foreground">Ke zkopírování</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-gray-500">
                      {copyPreviewData?.skipped || 0}
                    </div>
                    <div className="text-muted-foreground">Již vyřešeno</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-yellow-600">
                      {copyPreviewData?.notFound || 0}
                    </div>
                    <div className="text-muted-foreground">Nenalezeno</div>
                  </div>
                </div>
              </div>

              {copyPreviewData?.matches && copyPreviewData.matches.length > 0 ? (
                <div className="max-h-80 overflow-y-auto border rounded-lg">
                  <table className="w-full text-sm">
                    <thead className="sticky top-0 bg-muted">
                      <tr>
                        <th className="px-3 py-2 text-left">Liga</th>
                        <th className="px-3 py-2 text-left">Země</th>
                        <th className="px-3 py-2 text-left">Řešení</th>
                        <th className="px-3 py-2 text-left">Cílová liga</th>
                      </tr>
                    </thead>
                    <tbody>
                      {copyPreviewData.matches.map((match) => (
                        <tr key={match.targetId} className="border-t">
                          <td className="px-3 py-2">{match.targetLeagueName}</td>
                          <td className="px-3 py-2 font-mono text-xs uppercase">
                            {match.targetCountryCode}
                          </td>
                          <td className="px-3 py-2">
                            <span className={`px-2 py-1 rounded text-xs ${
                              match.sourceResolutionType === "Mapped"
                                ? "bg-green-100 text-green-800"
                                : match.sourceResolutionType === "Ignored"
                                ? "bg-gray-100 text-gray-800"
                                : "bg-orange-100 text-orange-800"
                            }`}>
                              {match.sourceResolutionType === "Mapped" && "Namapováno"}
                              {match.sourceResolutionType === "Ignored" && "Ignorováno"}
                              {match.sourceResolutionType === "Unavailable" && "Nedostupné"}
                              {match.sourceResolutionType === "ManuallyMapped" && "Ručně mapováno"}
                            </span>
                          </td>
                          <td className="px-3 py-2 text-muted-foreground">
                            {match.sourceResolvedLeagueName
                              ? match.sourceResolvedLeagueName
                              : match.sourceResolvedLeagueId
                                ? <span className="text-red-500">(smazaná liga)</span>
                                : "—"}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">
                  Žádné shody k zkopírování nenalezeny.
                </div>
              )}

              <DialogFooter>
                <Button variant="outline" onClick={() => setCopyDialogOpen(false)}>
                  Zrušit
                </Button>
                <Button
                  onClick={handleCopyExecute}
                  disabled={
                    !copyPreviewData?.matches ||
                    copyPreviewData.matches.length === 0 ||
                    copyExecuteMutation.isPending
                  }
                >
                  {copyExecuteMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  <Check className="mr-2 h-4 w-4" />
                  Potvrdit ({copyPreviewData?.matches.length || 0})
                </Button>
              </DialogFooter>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Detail Mapping Dialog */}
      <UnmatchedLeagueMappingDialog
        open={detailDialogOpen}
        onOpenChange={setDetailDialogOpen}
        leagueId={detailLeagueId}
      />
    </div>
  );
}
