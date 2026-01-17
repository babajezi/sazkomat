"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Loader2,
  CheckCircle2,
  XCircle,
  AlertCircle,
  ArrowRight,
  ExternalLink,
  Ban,
  Clock
} from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";

interface UnmatchedLeagueMappingDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  leagueId: string | null;
}

interface MappingDetail {
  unmatchedLeague: {
    id: string;
    providerId: string;
    providerName: string;
    providerLeagueId: string | null;
    providerLeagueName: string;
    providerSlug: string | null;
    countryCode: string;
    countryName: string | null;
    scrapedAt: string;
    isResolved: boolean;
    resolutionType: string | null;
    resolvedAt: string | null;
    resolutionNotes: string | null;
  };
  matchedCountry: {
    id: string;
    name: string;
    code: string;
    flagEmoji: string;
    isActive: boolean;
  } | null;
  resolvedLeague: {
    id: string;
    name: string;
    betExplorerSlug: string | null;
    countryName: string | null;
    countryCode: string | null;
    isActive: boolean;
  } | null;
  leagueProvider: {
    id: string;
    providerName: string | null;
    providerSlug: string;
    isActive: boolean;
  } | null;
  nameMappings: Array<{
    id: string;
    providerCode: string;
    providerLeagueName: string;
    countryCode: string;
    betExplorerSlug: string;
    isActive: boolean;
    priority: number;
    usageCount: number;
  }>;
}

function formatDate(dateString: string | null) {
  if (!dateString) return "—";
  return new Date(dateString).toLocaleString("cs-CZ", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function UnmatchedLeagueMappingDialog({
  open,
  onOpenChange,
  leagueId,
}: UnmatchedLeagueMappingDialogProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["unmatched-league-mapping", leagueId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/unmatched-leagues/${leagueId}/mapping`);
      if (!res.ok) throw new Error("Failed to fetch mapping details");
      return res.json() as Promise<MappingDetail>;
    },
    enabled: open && !!leagueId,
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Detail mapování</DialogTitle>
          <DialogDescription>
            Informace o nespárované lize a jejím mapování
          </DialogDescription>
        </DialogHeader>

        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        )}

        {error && (
          <div className="text-red-600 py-4">
            Chyba: {(error as Error).message}
          </div>
        )}

        {data && <MappingContent data={data} />}
      </DialogContent>
    </Dialog>
  );
}

function MappingContent({ data }: { data: MappingDetail }) {
  const { unmatchedLeague, matchedCountry, resolvedLeague, leagueProvider, nameMappings } = data;

  // Determine status
  let status = "Nevyřešeno";
  let statusColor = "bg-yellow-600";
  if (unmatchedLeague.isResolved) {
    if (unmatchedLeague.resolutionType === "Mapped") {
      status = "Namapováno";
      statusColor = "bg-green-600";
    } else if (unmatchedLeague.resolutionType === "Ignored") {
      status = "Ignorováno";
      statusColor = "bg-gray-600";
    } else if (unmatchedLeague.resolutionType === "Unavailable") {
      status = "Nedostupné";
      statusColor = "bg-orange-600";
    }
  }

  return (
    <div className="space-y-4">
      {/* Status */}
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium">Status:</span>
        <Badge className={statusColor}>{status}</Badge>
      </div>

      {/* Provider Data */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">Data z Betting Providera</CardTitle>
        </CardHeader>
        <CardContent className="text-sm">
          <div className="grid grid-cols-2 gap-2">
            <div className="text-muted-foreground">Provider:</div>
            <div className="font-medium">{unmatchedLeague.providerName}</div>
            <div className="text-muted-foreground">Název ligy:</div>
            <div className="font-medium">{unmatchedLeague.providerLeagueName}</div>
            {unmatchedLeague.providerSlug && (
              <>
                <div className="text-muted-foreground">Slug:</div>
                <div className="font-mono text-xs">{unmatchedLeague.providerSlug}</div>
              </>
            )}
            <div className="text-muted-foreground">Země:</div>
            <div>
              {unmatchedLeague.countryName || unmatchedLeague.countryCode}
              <span className="font-mono text-xs text-muted-foreground ml-2 uppercase">
                ({unmatchedLeague.countryCode})
              </span>
            </div>
            <div className="text-muted-foreground">Skenováno:</div>
            <div>{formatDate(unmatchedLeague.scrapedAt)}</div>
          </div>
        </CardContent>
      </Card>

      {/* Resolution Info */}
      {unmatchedLeague.isResolved && (
        <Card className={
          unmatchedLeague.resolutionType === "Mapped" ? "border-green-200 bg-green-50" :
          unmatchedLeague.resolutionType === "Ignored" ? "border-gray-200 bg-gray-50" :
          "border-orange-200 bg-orange-50"
        }>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Řešení</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Vyřešeno:</div>
              <div>{formatDate(unmatchedLeague.resolvedAt)}</div>
              {unmatchedLeague.resolutionNotes && (
                <>
                  <div className="text-muted-foreground">Poznámky:</div>
                  <div>{unmatchedLeague.resolutionNotes}</div>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Matched Country */}
      {matchedCountry && (
        <Card className="border-blue-200 bg-blue-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Země v BetExploreru</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Název:</div>
              <div>{matchedCountry.flagEmoji} {matchedCountry.name}</div>
              <div className="text-muted-foreground">Kód:</div>
              <div className="font-mono uppercase">{matchedCountry.code}</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{matchedCountry.isActive ? "Ano" : "Ne"}</div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Resolved League */}
      {resolvedLeague && (
        <Card className="border-green-200 bg-green-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Namapovaná Liga</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Název:</div>
              <div className="font-medium">{resolvedLeague.name}</div>
              <div className="text-muted-foreground">Slug:</div>
              <div className="font-mono text-xs">{resolvedLeague.betExplorerSlug || "—"}</div>
              <div className="text-muted-foreground">Země:</div>
              <div>{resolvedLeague.countryName} ({resolvedLeague.countryCode})</div>
              {resolvedLeague.betExplorerSlug && resolvedLeague.countryCode && (
                <>
                  <div className="text-muted-foreground">BetExplorer:</div>
                  <div>
                    <a
                      href={`https://www.betexplorer.com/football/${resolvedLeague.countryCode.toLowerCase()}/${resolvedLeague.betExplorerSlug}/`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-blue-600 hover:underline flex items-center gap-1"
                    >
                      Otevřít <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* LeagueProvider Mapping */}
      {leagueProvider && (
        <Card className="border-purple-200 bg-purple-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">LeagueProvider vazba</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Provider název:</div>
              <div>{leagueProvider.providerName ?? "—"}</div>
              <div className="text-muted-foreground">Provider slug:</div>
              <div className="font-mono text-xs">{leagueProvider.providerSlug}</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{leagueProvider.isActive ? "Ano" : "Ne"}</div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Name Mappings */}
      {nameMappings.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-sm">Name mappingy ({nameMappings.length})</CardTitle>
              <Link href="/mappings">
                <Button variant="outline" size="sm">
                  <ExternalLink className="w-3 h-3 mr-1" />
                  Spravovat
                </Button>
              </Link>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {nameMappings.map((m) => (
                <div
                  key={m.id}
                  className={`p-2 rounded border text-sm ${
                    m.isActive ? "bg-green-50 border-green-200" : "bg-gray-50 border-gray-200"
                  }`}
                >
                  <div className="flex items-center gap-2 flex-wrap">
                    <Badge variant="outline">{m.countryCode}</Badge>
                    <span className="font-medium">{m.providerLeagueName}</span>
                    <ArrowRight className="h-3 w-3" />
                    <span className="font-mono">{m.betExplorerSlug}</span>
                    {!m.isActive && <Badge variant="secondary">Neaktivní</Badge>}
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    Provider: {m.providerCode} | Priorita: {m.priority} | Použití: {m.usageCount}x
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Hint for unresolved */}
      {!unmatchedLeague.isResolved && nameMappings.length === 0 && (
        <Card className="border-yellow-200 bg-yellow-50">
          <CardContent className="pt-4">
            <div className="text-sm text-yellow-800">
              <strong>Tip:</strong> Tato liga ještě nebyla vyřešena. Použijte tlačítka v tabulce pro namapování, ignorování nebo označení jako nedostupné.
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
