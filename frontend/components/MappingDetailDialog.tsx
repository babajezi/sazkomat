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
import { Loader2, CheckCircle2, XCircle, AlertCircle, ArrowRight, ExternalLink } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";

interface MappingDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  entityType: "country" | "league";
  entityId: string | null;
}

interface CountryMappingDetail {
  providerCountry: {
    id: string;
    providerId: string;
    providerCode: string;
    providerName: string;
    isoCode: string;
    flagEmoji: string;
    isImported: boolean;
    countryId: string | null;
    scannedAt: string;
  };
  matchedCountry: {
    id: string;
    name: string;
    nameCs: string | null;
    code: string;
    isoCode: string;
    flagEmoji: string;
    isActive: boolean;
  } | null;
  countryProvider: {
    id: string;
    providerCode: string;
    providerName: string;
    isActive: boolean;
  } | null;
  nameMappings: Array<{
    id: string;
    providerCountryName: string;
    betExplorerCode: string;
    isActive: boolean;
    priority: number;
    usageCount: number;
    lastUsedAt: string | null;
  }>;
  mappingStatus: string;
}

interface LeagueMappingDetail {
  providerLeague: {
    id: string;
    providerId: string;
    providerName: string;
    displayName: string | null;
    providerSlug: string | null;
    countryCode: string;
    mappingStatus: string;
    isImported: boolean;
    leagueId: string | null;
    scannedAt: string;
  };
  matchedLeague: {
    id: string;
    name: string;
    nameCs: string | null;
    betExplorerSlug: string;
    country: {
      id: string;
      name: string;
      code: string;
    } | null;
    isActive: boolean;
  } | null;
  leagueProvider: {
    id: string;
    providerLeagueId: string | number | null;
    providerName: string | null;
    providerSlug: string;
    isActive: boolean;
  } | null;
  nameMappings: Array<{
    id: string;
    providerLeagueName: string;
    countryCode: string;
    betExplorerSlug: string;
    isActive: boolean;
    priority: number;
    usageCount: number;
    lastUsedAt: string | null;
  }>;
}

function StatusBadge({ status }: { status: string }) {
  switch (status) {
    case "Imported":
      return <Badge className="bg-green-600"><CheckCircle2 className="w-3 h-3 mr-1" /> Importováno</Badge>;
    case "Mapped":
    case "AutoMapped":
      return <Badge className="bg-blue-600"><CheckCircle2 className="w-3 h-3 mr-1" /> Namapováno</Badge>;
    case "ManualMapped":
      return <Badge className="bg-purple-600"><CheckCircle2 className="w-3 h-3 mr-1" /> Manuální mapování</Badge>;
    case "HasNameMapping":
      return <Badge className="bg-yellow-600"><AlertCircle className="w-3 h-3 mr-1" /> Má name mapping</Badge>;
    case "Unmapped":
    default:
      return <Badge variant="destructive"><XCircle className="w-3 h-3 mr-1" /> Nenamapováno</Badge>;
  }
}

function formatDate(dateString: string | null) {
  if (!dateString) return "Nikdy";
  return new Date(dateString).toLocaleString("cs-CZ", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function MappingDetailDialog({
  open,
  onOpenChange,
  entityType,
  entityId,
}: MappingDetailDialogProps) {
  const endpoint = entityType === "country"
    ? `/api/provider-cache/countries/${entityId}/mapping`
    : `/api/provider-cache/leagues/${entityId}/mapping`;

  const { data, isLoading, error } = useQuery({
    queryKey: ["mapping-detail", entityType, entityId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}${endpoint}`);
      if (!res.ok) throw new Error("Failed to fetch mapping details");
      return res.json();
    },
    enabled: open && !!entityId,
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            Detail mapování - {entityType === "country" ? "Země" : "Liga"}
          </DialogTitle>
          <DialogDescription>
            Zobrazení jak se provider data mapují na BetExplorer entity
          </DialogDescription>
        </DialogHeader>

        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        )}

        {error && (
          <div className="text-red-600 py-4">
            Chyba při načítání: {(error as Error).message}
          </div>
        )}

        {data && entityType === "country" && (
          <CountryMappingContent data={data as CountryMappingDetail} />
        )}

        {data && entityType === "league" && (
          <LeagueMappingContent data={data as LeagueMappingDetail} />
        )}
      </DialogContent>
    </Dialog>
  );
}

function CountryMappingContent({ data }: { data: CountryMappingDetail }) {
  return (
    <div className="space-y-4">
      {/* Status */}
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium">Status:</span>
        <StatusBadge status={data.mappingStatus} />
      </div>

      {/* Provider Data */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">Data z Providera</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div className="grid grid-cols-2 gap-2">
            <div className="text-muted-foreground">Kód:</div>
            <div className="font-mono">{data.providerCountry.providerCode}</div>
            <div className="text-muted-foreground">Název:</div>
            <div>{data.providerCountry.flagEmoji} {data.providerCountry.providerName}</div>
            <div className="text-muted-foreground">ISO kód:</div>
            <div className="font-mono">{data.providerCountry.isoCode || "—"}</div>
            <div className="text-muted-foreground">Skenováno:</div>
            <div>{formatDate(data.providerCountry.scannedAt)}</div>
          </div>
        </CardContent>
      </Card>

      {/* Mapping Arrow */}
      <div className="flex justify-center">
        <ArrowRight className="h-6 w-6 text-muted-foreground" />
      </div>

      {/* Matched BetExplorer Country */}
      <Card className={data.matchedCountry ? "border-green-200 bg-green-50" : "border-red-200 bg-red-50"}>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">BetExplorer Země</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          {data.matchedCountry ? (
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Název:</div>
              <div>{data.matchedCountry.flagEmoji} {data.matchedCountry.name}</div>
              <div className="text-muted-foreground">Název (CZ):</div>
              <div>{data.matchedCountry.nameCs || "—"}</div>
              <div className="text-muted-foreground">Kód:</div>
              <div className="font-mono">{data.matchedCountry.code}</div>
              <div className="text-muted-foreground">ISO kód:</div>
              <div className="font-mono">{data.matchedCountry.isoCode}</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{data.matchedCountry.isActive ? "Ano" : "Ne"}</div>
            </div>
          ) : (
            <div className="text-red-600">
              Žádná odpovídající země v BetExploreru nebyla nalezena.
            </div>
          )}
        </CardContent>
      </Card>

      {/* CountryProvider Mapping */}
      {data.countryProvider && (
        <Card className="border-blue-200 bg-blue-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">CountryProvider vazba</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Provider kód:</div>
              <div className="font-mono">{data.countryProvider.providerCode}</div>
              <div className="text-muted-foreground">Provider název:</div>
              <div>{data.countryProvider.providerName}</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{data.countryProvider.isActive ? "Ano" : "Ne"}</div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Name Mappings */}
      {data.nameMappings.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-sm">Manuální mapování názvů ({data.nameMappings.length})</CardTitle>
              <Link href="/country-mappings">
                <Button variant="outline" size="sm">
                  <ExternalLink className="w-3 h-3 mr-1" />
                  Spravovat
                </Button>
              </Link>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {data.nameMappings.map((mapping) => (
                <div
                  key={mapping.id}
                  className={`p-2 rounded border text-sm ${
                    mapping.isActive ? "bg-green-50 border-green-200" : "bg-gray-50 border-gray-200"
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <span className="font-medium">{mapping.providerCountryName}</span>
                    <ArrowRight className="h-3 w-3" />
                    <span className="font-mono">{mapping.betExplorerCode}</span>
                    {!mapping.isActive && <Badge variant="secondary">Neaktivní</Badge>}
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    Priorita: {mapping.priority} | Použití: {mapping.usageCount}x
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function LeagueMappingContent({ data }: { data: LeagueMappingDetail }) {
  return (
    <div className="space-y-4">
      {/* Status */}
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium">Status:</span>
        <StatusBadge status={data.providerLeague.mappingStatus} />
      </div>

      {/* Provider Data */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">Data z Providera</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div className="grid grid-cols-2 gap-2">
            <div className="text-muted-foreground">Název:</div>
            <div>{data.providerLeague.displayName || data.providerLeague.providerName}</div>
            {data.providerLeague.displayName && (
              <>
                <div className="text-muted-foreground">Původní název:</div>
                <div className="text-xs">{data.providerLeague.providerName}</div>
              </>
            )}
            <div className="text-muted-foreground">Země:</div>
            <div className="font-mono uppercase">{data.providerLeague.countryCode}</div>
            <div className="text-muted-foreground">Slug:</div>
            <div className="font-mono">{data.providerLeague.providerSlug || "—"}</div>
            <div className="text-muted-foreground">Skenováno:</div>
            <div>{formatDate(data.providerLeague.scannedAt)}</div>
          </div>
        </CardContent>
      </Card>

      {/* Mapping Arrow */}
      <div className="flex justify-center">
        <ArrowRight className="h-6 w-6 text-muted-foreground" />
      </div>

      {/* Matched BetExplorer League */}
      <Card className={data.matchedLeague ? "border-green-200 bg-green-50" : "border-red-200 bg-red-50"}>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">BetExplorer Liga</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          {data.matchedLeague ? (
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Název:</div>
              <div>{data.matchedLeague.name}</div>
              <div className="text-muted-foreground">Název (CZ):</div>
              <div>{data.matchedLeague.nameCs || "—"}</div>
              <div className="text-muted-foreground">Slug:</div>
              <div className="font-mono">{data.matchedLeague.betExplorerSlug}</div>
              <div className="text-muted-foreground">Země:</div>
              <div>{data.matchedLeague.country?.name} ({data.matchedLeague.country?.code})</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{data.matchedLeague.isActive ? "Ano" : "Ne"}</div>
              <div className="text-muted-foreground">BetExplorer:</div>
              <div>
                <a
                  href={`https://www.betexplorer.com/football/${data.matchedLeague.country?.code?.toLowerCase()}/${data.matchedLeague.betExplorerSlug}/`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:underline flex items-center gap-1"
                >
                  Otevřít <ExternalLink className="w-3 h-3" />
                </a>
              </div>
            </div>
          ) : (
            <div className="text-red-600">
              Žádná odpovídající liga v BetExploreru nebyla nalezena.
              {data.providerLeague.providerSlug && (
                <div className="mt-2 text-sm">
                  Hledaný slug: <code className="bg-gray-100 px-1 rounded">{data.providerLeague.providerSlug}</code>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* LeagueProvider Mapping */}
      {data.leagueProvider && (
        <Card className="border-blue-200 bg-blue-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">LeagueProvider vazba</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="grid grid-cols-2 gap-2">
              <div className="text-muted-foreground">Provider Liga ID:</div>
              <div className="font-mono text-xs">{data.leagueProvider.providerLeagueId ?? "—"}</div>
              <div className="text-muted-foreground">Provider název:</div>
              <div>{data.leagueProvider.providerName ?? "—"}</div>
              <div className="text-muted-foreground">Provider slug:</div>
              <div className="font-mono">{data.leagueProvider.providerSlug}</div>
              <div className="text-muted-foreground">Aktivní:</div>
              <div>{data.leagueProvider.isActive ? "Ano" : "Ne"}</div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Name Mappings */}
      {data.nameMappings.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-sm">Manuální mapování názvů ({data.nameMappings.length})</CardTitle>
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
              {data.nameMappings.map((mapping) => (
                <div
                  key={mapping.id}
                  className={`p-2 rounded border text-sm ${
                    mapping.isActive ? "bg-green-50 border-green-200" : "bg-gray-50 border-gray-200"
                  }`}
                >
                  <div className="flex items-center gap-2 flex-wrap">
                    <Badge variant="outline">{mapping.countryCode}</Badge>
                    <span className="font-medium">{mapping.providerLeagueName}</span>
                    <ArrowRight className="h-3 w-3" />
                    <span className="font-mono">{mapping.betExplorerSlug}</span>
                    {!mapping.isActive && <Badge variant="secondary">Neaktivní</Badge>}
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    Priorita: {mapping.priority} | Použití: {mapping.usageCount}x
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* No mappings hint */}
      {data.nameMappings.length === 0 && !data.matchedLeague && (
        <Card className="border-yellow-200 bg-yellow-50">
          <CardContent className="pt-4">
            <div className="text-sm text-yellow-800">
              <strong>Tip:</strong> Tato liga nemá žádné mapování. Můžete vytvořit manuální mapování v{" "}
              <Link href="/mappings" className="text-blue-600 hover:underline">
                Mapování názvů lig
              </Link>
              .
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
