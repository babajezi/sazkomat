"use client";

import { useState, useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { strategiesApi } from "@/lib/api/client";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
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
import {
  Search,
  Play,
  Trash2,
  Clock,
  ChevronDown,
  ChevronRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Loader2,
  TrendingUp,
  TrendingDown,
  Target,
  AlertTriangle,
} from "lucide-react";
import type {
  StrategyInfo,
  StrategySimulationSpec,
  ScreeningResult,
  SimulationResult,
  LeagueScreening,
  LeagueSimulationResult,
  ScreeningListItem,
} from "@/lib/api/types";

type SortField = string;
type SortDir = "asc" | "desc";

export default function StrategiesPage() {
  const queryClient = useQueryClient();

  // Strategy selection
  const [selectedStrategy, setSelectedStrategy] = useState<string>("");
  const [params, setParams] = useState<Record<string, unknown>>({});
  const [minMatches, setMinMatches] = useState(4);
  const [requireOdds, setRequireOdds] = useState(false);
  const [startYear, setStartYear] = useState(2015);

  // Results
  const [screeningResult, setScreeningResult] = useState<ScreeningResult | null>(null);
  const [simulationResult, setSimulationResult] = useState<SimulationResult | null>(null);
  const [selectedLeagueIds, setSelectedLeagueIds] = useState<Set<string>>(new Set());

  // Sorting
  const [screenSort, setScreenSort] = useState<{ field: SortField; dir: SortDir }>({
    field: "near1Rate",
    dir: "desc",
  });
  const [simSort, setSimSort] = useState<{ field: SortField; dir: SortDir }>({
    field: "profit",
    dir: "desc",
  });

  // UI
  const [expandedLeagues, setExpandedLeagues] = useState<Set<string>>(new Set());
  const [deleteScreeningId, setDeleteScreeningId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"config" | "screening" | "simulation">("config");

  // Queries
  const { data: strategies } = useQuery({
    queryKey: ["strategy-types"],
    queryFn: strategiesApi.getTypes,
  });

  const { data: savedScreenings } = useQuery({
    queryKey: ["strategy-screenings"],
    queryFn: strategiesApi.getScreenings,
  });

  const activeStrategy = strategies?.find((s) => s.type === selectedStrategy);

  // Initialize params when strategy changes
  const handleStrategyChange = (type: string) => {
    setSelectedStrategy(type);
    const strategy = strategies?.find((s) => s.type === type);
    if (strategy) {
      const defaults: Record<string, unknown> = {};
      strategy.parameters.forEach((p) => {
        defaults[p.name] = p.defaultValue;
      });
      setParams(defaults);
    }
    setScreeningResult(null);
    setSimulationResult(null);
    setSelectedLeagueIds(new Set());
  };

  const buildSpec = (leagueIds?: string[]): StrategySimulationSpec => ({
    strategyType: selectedStrategy,
    parameters: params,
    leagueIds: leagueIds && leagueIds.length > 0 ? leagueIds : undefined,
    requireOdds: requireOdds || undefined,
    minMatches,
    startYear: startYear || undefined,
  });

  // Mutations
  const screenMutation = useMutation({
    mutationFn: () => strategiesApi.screen(buildSpec()),
    onSuccess: (result) => {
      setScreeningResult(result);
      setActiveTab("screening");
      setSelectedLeagueIds(new Set());
      queryClient.invalidateQueries({ queryKey: ["strategy-screenings"] });
    },
  });

  const simulateMutation = useMutation({
    mutationFn: () => {
      const ids = Array.from(selectedLeagueIds);
      return strategiesApi.simulate(buildSpec(ids.length > 0 ? ids : undefined));
    },
    onSuccess: (result) => {
      setSimulationResult(result);
      setActiveTab("simulation");
    },
  });

  const loadScreeningMutation = useMutation({
    mutationFn: (id: string) => strategiesApi.getScreening(id),
    onSuccess: (detail) => {
      if (detail.result) {
        setScreeningResult(detail.result);
        setActiveTab("screening");
        setSelectedLeagueIds(new Set());
        if (detail.spec) {
          setSelectedStrategy(detail.spec.strategyType);
          setParams(detail.spec.parameters || {});
          setMinMatches(detail.spec.minMatches);
          setRequireOdds(detail.spec.requireOdds || false);
          setStartYear(detail.spec.startYear || 2015);
        }
      }
    },
  });

  const deleteScreeningMutation = useMutation({
    mutationFn: (id: string) => strategiesApi.deleteScreening(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["strategy-screenings"] });
      setDeleteScreeningId(null);
    },
  });

  // Sorting helpers
  const sortData = <T extends Record<string, unknown>>(
    data: T[],
    field: string,
    dir: SortDir
  ): T[] => {
    return [...data].sort((a, b) => {
      const av = a[field] as number;
      const bv = b[field] as number;
      return dir === "asc" ? av - bv : bv - av;
    });
  };

  const toggleSort = (
    current: { field: SortField; dir: SortDir },
    setter: (v: { field: SortField; dir: SortDir }) => void,
    field: string
  ) => {
    if (current.field === field) {
      setter({ field, dir: current.dir === "asc" ? "desc" : "asc" });
    } else {
      setter({ field, dir: "desc" });
    }
  };

  const SortIcon = ({ field, current }: { field: string; current: { field: string; dir: SortDir } }) => {
    if (current.field !== field) return <ArrowUpDown className="w-3 h-3 ml-1 opacity-40" />;
    return current.dir === "asc" ? (
      <ArrowUp className="w-3 h-3 ml-1" />
    ) : (
      <ArrowDown className="w-3 h-3 ml-1" />
    );
  };

  const sortedScreeningLeagues = useMemo(() => {
    if (!screeningResult) return [];
    return sortData(screeningResult.leagues as unknown as Record<string, unknown>[], screenSort.field, screenSort.dir) as unknown as LeagueScreening[];
  }, [screeningResult, screenSort]);

  const sortedSimLeagues = useMemo(() => {
    if (!simulationResult) return [];
    return sortData(simulationResult.leagues as unknown as Record<string, unknown>[], simSort.field, simSort.dir) as unknown as LeagueSimulationResult[];
  }, [simulationResult, simSort]);

  // Selection helpers
  const toggleLeagueSelection = (leagueId: string) => {
    setSelectedLeagueIds((prev) => {
      const next = new Set(prev);
      if (next.has(leagueId)) next.delete(leagueId);
      else next.add(leagueId);
      return next;
    });
  };

  const selectAllLeagues = () => {
    if (!screeningResult) return;
    setSelectedLeagueIds(new Set(screeningResult.leagues.map((l) => l.leagueId)));
  };

  const deselectAllLeagues = () => {
    setSelectedLeagueIds(new Set());
  };

  const toggleExpandLeague = (leagueId: string) => {
    setExpandedLeagues((prev) => {
      const next = new Set(prev);
      if (next.has(leagueId)) next.delete(leagueId);
      else next.add(leagueId);
      return next;
    });
  };

  const fmtNum = (n: number, decimals = 2) => n.toFixed(decimals);
  const fmtCurrency = (n: number) =>
    n.toLocaleString("cs-CZ", { minimumFractionDigits: 0, maximumFractionDigits: 0 });

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Strategie</h1>
          <p className="text-sm text-gray-500 mt-1">
            Screening lig a simulace sázkových strategií
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant={activeTab === "config" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveTab("config")}
          >
            Konfigurace
          </Button>
          <Button
            variant={activeTab === "screening" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveTab("screening")}
            disabled={!screeningResult}
          >
            Screening
            {screeningResult && (
              <Badge variant="secondary" className="ml-1">
                {screeningResult.totalLeagues}
              </Badge>
            )}
          </Button>
          <Button
            variant={activeTab === "simulation" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveTab("simulation")}
            disabled={!simulationResult}
          >
            Simulace
          </Button>
        </div>
      </div>

      {/* ===== CONFIGURATION TAB ===== */}
      {activeTab === "config" && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Strategy config */}
          <div className="lg:col-span-2 space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Parametry strategie</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Strategy type */}
                <div className="space-y-2">
                  <Label>Strategie</Label>
                  <Select value={selectedStrategy} onValueChange={handleStrategyChange}>
                    <SelectTrigger>
                      <SelectValue placeholder="Vyber strategii..." />
                    </SelectTrigger>
                    <SelectContent>
                      {strategies?.map((s) => (
                        <SelectItem key={s.type} value={s.type}>
                          {s.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {activeStrategy && (
                    <p className="text-xs text-gray-500">{activeStrategy.description}</p>
                  )}
                </div>

                {/* Dynamic parameters */}
                {activeStrategy?.parameters.map((p) => (
                  <div key={p.name} className="space-y-1">
                    <Label>{p.label}</Label>
                    {p.type === "select" && p.options ? (
                      <Select
                        value={String(params[p.name] ?? p.defaultValue)}
                        onValueChange={(v) => setParams((prev) => ({ ...prev, [p.name]: v }))}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {p.options.map((opt) => (
                            <SelectItem key={opt.value} value={opt.value}>
                              {opt.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    ) : p.type === "boolean" ? (
                      <div className="flex items-center gap-2">
                        <Checkbox
                          checked={Boolean(params[p.name] ?? p.defaultValue)}
                          onChange={(e) =>
                            setParams((prev) => ({ ...prev, [p.name]: e.target.checked }))
                          }
                        />
                      </div>
                    ) : (
                      <Input
                        type="number"
                        value={String(params[p.name] ?? p.defaultValue ?? "")}
                        onChange={(e) =>
                          setParams((prev) => ({
                            ...prev,
                            [p.name]: parseFloat(e.target.value) || 0,
                          }))
                        }
                      />
                    )}
                  </div>
                ))}

                {/* Common filters */}
                <div className="border-t pt-4 space-y-3">
                  <h3 className="text-sm font-medium text-gray-700">Společné filtry</h3>
                  <div className="grid grid-cols-3 gap-4">
                    <div className="space-y-1">
                      <Label>Simulovat od roku</Label>
                      <Input
                        type="number"
                        value={startYear}
                        onChange={(e) => setStartYear(parseInt(e.target.value) || 0)}
                        min={1900}
                        max={2030}
                        placeholder="např. 2015"
                      />
                    </div>
                    <div className="space-y-1">
                      <Label>Min. zápasů v kole</Label>
                      <Input
                        type="number"
                        value={minMatches}
                        onChange={(e) => setMinMatches(parseInt(e.target.value) || 4)}
                        min={1}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label>&nbsp;</Label>
                      <div className="flex items-center gap-2 h-10">
                        <Checkbox
                          checked={requireOdds}
                          onChange={(e) => setRequireOdds(e.target.checked)}
                        />
                        <span className="text-sm">Pouze kola s kurzy</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Action buttons */}
                <div className="flex gap-3 pt-2">
                  <Button
                    onClick={() => screenMutation.mutate()}
                    disabled={!selectedStrategy || screenMutation.isPending}
                  >
                    {screenMutation.isPending ? (
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    ) : (
                      <Search className="w-4 h-4 mr-2" />
                    )}
                    Screening lig
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => simulateMutation.mutate()}
                    disabled={!selectedStrategy || simulateMutation.isPending}
                  >
                    {simulateMutation.isPending ? (
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    ) : (
                      <Play className="w-4 h-4 mr-2" />
                    )}
                    Simulovat
                    {selectedLeagueIds.size > 0 && (
                      <Badge variant="secondary" className="ml-1">
                        {selectedLeagueIds.size} lig
                      </Badge>
                    )}
                  </Button>
                </div>

                {(screenMutation.isError || simulateMutation.isError) && (
                  <div className="text-sm text-red-600 bg-red-50 p-3 rounded">
                    {(screenMutation.error || simulateMutation.error)?.message || "Chyba"}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Saved screenings */}
          <div>
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Uložené screeningy</CardTitle>
              </CardHeader>
              <CardContent>
                {!savedScreenings || savedScreenings.length === 0 ? (
                  <p className="text-sm text-gray-500">Zatím žádné uložené screeningy.</p>
                ) : (
                  <div className="space-y-2">
                    {savedScreenings.map((s) => (
                      <div
                        key={s.id}
                        className="flex items-center justify-between p-2 rounded border hover:bg-gray-50 cursor-pointer"
                        onClick={() => loadScreeningMutation.mutate(s.id)}
                      >
                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-medium truncate">{s.name}</p>
                          <div className="flex items-center gap-2 text-xs text-gray-500">
                            <Clock className="w-3 h-3" />
                            {new Date(s.calculatedAt).toLocaleDateString("cs-CZ")}
                            <Badge variant="outline" className="text-xs">
                              {s.roundsAnalyzed.toLocaleString()} kol
                            </Badge>
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="shrink-0"
                          onClick={(e) => {
                            e.stopPropagation();
                            setDeleteScreeningId(s.id);
                          }}
                        >
                          <Trash2 className="w-3.5 h-3.5 text-red-500" />
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {/* ===== SCREENING TAB ===== */}
      {activeTab === "screening" && screeningResult && (
        <div className="space-y-4">
          {/* Stats bar */}
          <div className="flex items-center gap-4 text-sm text-gray-600">
            <span>
              <strong>{screeningResult.totalLeagues}</strong> lig
            </span>
            <span>
              <strong>{screeningResult.totalRounds.toLocaleString()}</strong> kol
            </span>
            <span>{screeningResult.executionMs} ms</span>
            <div className="flex-1" />
            <Button variant="outline" size="sm" onClick={selectAllLeagues}>
              Vybrat vše
            </Button>
            <Button variant="outline" size="sm" onClick={deselectAllLeagues}>
              Zrušit výběr
            </Button>
            <Button
              size="sm"
              disabled={simulateMutation.isPending || !selectedStrategy}
              onClick={() => simulateMutation.mutate()}
            >
              {simulateMutation.isPending ? (
                <Loader2 className="w-4 h-4 mr-1 animate-spin" />
              ) : (
                <Play className="w-4 h-4 mr-1" />
              )}
              Simulovat ({selectedLeagueIds.size})
            </Button>
          </div>

          {/* Screening table */}
          <Card>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-10">
                      <Checkbox
                        checked={
                          screeningResult.leagues.length > 0 &&
                          selectedLeagueIds.size === screeningResult.leagues.length
                        }
                        onChange={(e) =>
                          e.target.checked ? selectAllLeagues() : deselectAllLeagues()
                        }
                      />
                    </TableHead>
                    <TableHead>Liga</TableHead>
                    <TableHead>Země</TableHead>
                    {[
                      { field: "totalSeasons", label: "Sezóny" },
                      { field: "totalRounds", label: "Kola" },
                      { field: "perfectRate", label: "PERFECT %" },
                      { field: "near1Rate", label: "NEAR-1 %" },
                      { field: "near2Rate", label: "NEAR-2 %" },
                      { field: "avgGap", label: "Avg Gap" },
                      { field: "maxGap", label: "Max Gap" },
                      { field: "roundsWithOdds", label: "S kurzy" },
                    ].map((col) => (
                      <TableHead
                        key={col.field}
                        className="cursor-pointer hover:bg-gray-50 text-right"
                        onClick={() => toggleSort(screenSort, setScreenSort, col.field)}
                      >
                        <div className="flex items-center justify-end">
                          {col.label}
                          <SortIcon field={col.field} current={screenSort} />
                        </div>
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedScreeningLeagues.map((l) => (
                    <TableRow
                      key={l.leagueId}
                      className={selectedLeagueIds.has(l.leagueId) ? "bg-blue-50" : ""}
                    >
                      <TableCell>
                        <Checkbox
                          checked={selectedLeagueIds.has(l.leagueId)}
                          onChange={() => toggleLeagueSelection(l.leagueId)}
                        />
                      </TableCell>
                      <TableCell className="font-medium">{l.league}</TableCell>
                      <TableCell className="text-gray-600">{l.country}</TableCell>
                      <TableCell className="text-right">{l.totalSeasons}</TableCell>
                      <TableCell className="text-right">{l.totalRounds}</TableCell>
                      <TableCell className="text-right font-mono">
                        {fmtNum(l.perfectRate)}
                      </TableCell>
                      <TableCell className="text-right font-mono font-medium">
                        {fmtNum(l.near1Rate)}
                      </TableCell>
                      <TableCell className="text-right font-mono">
                        {fmtNum(l.near2Rate)}
                      </TableCell>
                      <TableCell className="text-right font-mono">{fmtNum(l.avgGap, 1)}</TableCell>
                      <TableCell className="text-right font-mono">{l.maxGap}</TableCell>
                      <TableCell className="text-right">{l.roundsWithOdds}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </Card>
        </div>
      )}

      {/* ===== SIMULATION TAB ===== */}
      {activeTab === "simulation" && simulationResult && (
        <div className="space-y-4">
          {/* Summary cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
            <SummaryCard
              label="Win Rate"
              value={`${fmtNum(simulationResult.summary.winRate)}%`}
              sub={`${simulationResult.summary.winningRounds}/${simulationResult.summary.totalRounds}`}
            />
            <SummaryCard
              label="ROI"
              value={`${fmtNum(simulationResult.summary.roi)}%`}
              color={simulationResult.summary.roi >= 0 ? "text-green-600" : "text-red-600"}
              icon={
                simulationResult.summary.roi >= 0 ? (
                  <TrendingUp className="w-4 h-4 text-green-600" />
                ) : (
                  <TrendingDown className="w-4 h-4 text-red-600" />
                )
              }
            />
            <SummaryCard
              label="Profit"
              value={`${fmtCurrency(simulationResult.summary.profit)} Kč`}
              color={simulationResult.summary.profit >= 0 ? "text-green-600" : "text-red-600"}
            />
            <SummaryCard
              label="Vsazeno"
              value={`${fmtCurrency(simulationResult.summary.totalStaked)} Kč`}
            />
            <SummaryCard
              label="Max séri proher"
              value={String(simulationResult.summary.maxConsecutiveLosses)}
              icon={<AlertTriangle className="w-4 h-4 text-orange-500" />}
            />
            <SummaryCard
              label="Max sázka"
              value={`${fmtCurrency(simulationResult.summary.maxStake)} Kč`}
            />
          </div>

          {/* Info bar */}
          <div className="flex items-center gap-4 text-sm text-gray-600">
            <span>
              <strong>{simulationResult.leagues.length}</strong> lig
            </span>
            <span>
              <strong>{simulationResult.summary.totalLeagueSeasons}</strong> sezón
            </span>
            <span>
              <strong>{simulationResult.summary.roundsWithOdds}</strong> kol s kurzy
            </span>
            <span>{simulationResult.executionMs} ms</span>
          </div>

          {/* Simulation table */}
          <Card>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-8" />
                    <TableHead>Liga</TableHead>
                    <TableHead>Země</TableHead>
                    {[
                      { field: "totalSeasons", label: "Sezóny" },
                      { field: "totalRounds", label: "Kola" },
                      { field: "winningRounds", label: "Výhry" },
                      { field: "totalStaked", label: "Vsazeno" },
                      { field: "totalWon", label: "Výhra" },
                      { field: "profit", label: "Profit" },
                      { field: "maxConsecutiveLosses", label: "Max série" },
                      { field: "maxStake", label: "Max sázka" },
                    ].map((col) => (
                      <TableHead
                        key={col.field}
                        className="cursor-pointer hover:bg-gray-50 text-right"
                        onClick={() => toggleSort(simSort, setSimSort, col.field)}
                      >
                        <div className="flex items-center justify-end">
                          {col.label}
                          <SortIcon field={col.field} current={simSort} />
                        </div>
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sortedSimLeagues.map((l) => (
                    <>
                      <TableRow
                        key={l.leagueId}
                        className="cursor-pointer hover:bg-gray-50"
                        onClick={() => toggleExpandLeague(l.leagueId)}
                      >
                        <TableCell>
                          {expandedLeagues.has(l.leagueId) ? (
                            <ChevronDown className="w-4 h-4 text-gray-400" />
                          ) : (
                            <ChevronRight className="w-4 h-4 text-gray-400" />
                          )}
                        </TableCell>
                        <TableCell className="font-medium">{l.league}</TableCell>
                        <TableCell className="text-gray-600">{l.country}</TableCell>
                        <TableCell className="text-right">{l.totalSeasons}</TableCell>
                        <TableCell className="text-right">{l.totalRounds}</TableCell>
                        <TableCell className="text-right">{l.winningRounds}</TableCell>
                        <TableCell className="text-right font-mono">
                          {fmtCurrency(l.totalStaked)}
                        </TableCell>
                        <TableCell className="text-right font-mono">
                          {l.hasOdds ? fmtCurrency(l.totalWon) : "—"}
                        </TableCell>
                        <TableCell
                          className={`text-right font-mono font-medium ${
                            l.profit >= 0 ? "text-green-600" : "text-red-600"
                          }`}
                        >
                          {l.hasOdds ? fmtCurrency(l.profit) : "—"}
                        </TableCell>
                        <TableCell className="text-right">{l.maxConsecutiveLosses}</TableCell>
                        <TableCell className="text-right font-mono">
                          {fmtCurrency(l.maxStake)}
                        </TableCell>
                      </TableRow>
                      {expandedLeagues.has(l.leagueId) &&
                        l.seasons.map((s) => (
                          <TableRow key={`${l.leagueId}-${s.season}`} className="bg-gray-50">
                            <TableCell />
                            <TableCell className="pl-8 text-sm text-gray-500" colSpan={2}>
                              {s.season}
                            </TableCell>
                            <TableCell />
                            <TableCell className="text-right text-sm">{s.totalRounds}</TableCell>
                            <TableCell className="text-right text-sm">
                              {s.winningRounds}
                            </TableCell>
                            <TableCell className="text-right font-mono text-sm">
                              {fmtCurrency(s.totalStaked)}
                            </TableCell>
                            <TableCell className="text-right font-mono text-sm">
                              {s.hasOdds ? fmtCurrency(s.totalWon) : "—"}
                            </TableCell>
                            <TableCell
                              className={`text-right font-mono text-sm ${
                                s.profit >= 0 ? "text-green-600" : "text-red-600"
                              }`}
                            >
                              {s.hasOdds ? fmtCurrency(s.profit) : "—"}
                            </TableCell>
                            <TableCell />
                            <TableCell />
                          </TableRow>
                        ))}
                    </>
                  ))}
                </TableBody>
              </Table>
            </div>
          </Card>
        </div>
      )}

      {/* Delete confirmation dialog */}
      <AlertDialog
        open={!!deleteScreeningId}
        onOpenChange={(open) => !open && setDeleteScreeningId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Smazat screening?</AlertDialogTitle>
            <AlertDialogDescription>
              Tato akce je nevratná. Screening bude trvale smazán.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Zrušit</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteScreeningId && deleteScreeningMutation.mutate(deleteScreeningId)}
            >
              Smazat
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function SummaryCard({
  label,
  value,
  sub,
  color,
  icon,
}: {
  label: string;
  value: string;
  sub?: string;
  color?: string;
  icon?: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <p className="text-xs text-gray-500">{label}</p>
          {icon}
        </div>
        <p className={`text-lg font-bold mt-1 ${color || ""}`}>{value}</p>
        {sub && <p className="text-xs text-gray-400">{sub}</p>}
      </CardContent>
    </Card>
  );
}
