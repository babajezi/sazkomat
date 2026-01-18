"use client";

import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Loader2,
  Globe,
  ArrowRight,
  CheckCircle2,
  AlertCircle,
  XCircle,
  Trash2,
} from "lucide-react";
import { unmatchedLeagueApi } from "@/lib/api/client";
import { GlobalRulePreview, GlobalRuleResult } from "@/lib/api/types";

interface GlobalRuleDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  leagueId: string | null;
}

export function GlobalRuleDialog({
  open,
  onOpenChange,
  leagueId,
}: GlobalRuleDialogProps) {
  const queryClient = useQueryClient();
  const [result, setResult] = useState<GlobalRuleResult | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);

  // Reset state when dialog opens/closes
  useEffect(() => {
    if (!open) {
      setResult(null);
      setMutationError(null);
    }
  }, [open]);

  const { data, isLoading, error } = useQuery({
    queryKey: ["global-rule-preview", leagueId],
    queryFn: () => unmatchedLeagueApi.getGlobalRulePreview(leagueId!),
    enabled: open && !!leagueId && !result,
  });

  const createMutation = useMutation({
    mutationFn: () => unmatchedLeagueApi.createGlobalRule(leagueId!),
    onSuccess: (data) => {
      setResult(data);
      setMutationError(null);
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues"] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues-stats"] });
      queryClient.invalidateQueries({ queryKey: ["global-rule-preview"] });
    },
    onError: (error) => {
      setMutationError((error as Error).message);
    },
  });

  const handleClose = () => {
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Globe className="h-5 w-5" />
            Vytvořit globální pravidlo
          </DialogTitle>
          <DialogDescription>
            Globální pravidlo se aplikuje na všechny betting providery.
            Dotčené záznamy v tabulce Unmatched Leagues budou smazány.
          </DialogDescription>
        </DialogHeader>

        {/* Success State */}
        {result && (
          <Card className="border-green-200 bg-green-50">
            <CardContent className="pt-4">
              <div className="flex items-center gap-2 text-green-800">
                <CheckCircle2 className="h-5 w-5" />
                <span className="font-medium">Globální pravidlo vytvořeno!</span>
              </div>
              <div className="text-sm text-green-700 mt-2">
                {result.deletedCount > 0
                  ? `Smazáno ${result.deletedCount} záznamů z Unmatched Leagues.`
                  : "Žádné záznamy nebyly smazány."}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Mutation Error */}
        {mutationError && (
          <Card className="border-red-200 bg-red-50">
            <CardContent className="pt-4">
              <div className="flex items-center gap-2 text-red-800">
                <XCircle className="h-5 w-5" />
                <span>Chyba: {mutationError}</span>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Loading State */}
        {isLoading && !result && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        )}

        {/* Query Error */}
        {error && !result && (
          <div className="text-red-600 py-4 flex items-center gap-2">
            <XCircle className="h-5 w-5" />
            Chyba: {(error as Error).message}
          </div>
        )}

        {/* Cannot Create Global Rule */}
        {data && !data.canCreateGlobalRule && !result && (
          <Card className="border-yellow-200 bg-yellow-50">
            <CardContent className="pt-4">
              <div className="flex items-center gap-2 text-yellow-800">
                <AlertCircle className="h-5 w-5" />
                <span>{data.validationMessage}</span>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Preview Content */}
        {data && data.canCreateGlobalRule && !result && (
          <>
            {/* Rule Preview */}
            <Card className="border-blue-200 bg-blue-50">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Nové pravidlo</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2 flex-wrap">
                  <Badge variant="outline" className="font-mono">
                    {data.countryCode?.toUpperCase()}
                  </Badge>
                  <span className="font-medium">{data.normalizedLeagueName}</span>
                  <ArrowRight className="h-4 w-4" />
                  <span className="font-mono text-blue-600">
                    {data.betExplorerSlug}
                  </span>
                </div>
                <div className="text-sm text-muted-foreground mt-2">
                  Cílová liga: {data.sourceLeagueName}
                </div>
              </CardContent>
            </Card>

            {/* Affected Leagues - will be deleted */}
            {data.affectedLeagues.length > 0 ? (
              <Card className="border-red-200">
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm flex items-center gap-2 text-red-700">
                    <Trash2 className="h-4 w-4" />
                    Záznamy ke smazání ({data.affectedLeagues.length})
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-0">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Provider</TableHead>
                        <TableHead>Název ligy</TableHead>
                        <TableHead>Status</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {data.affectedLeagues.map((league) => (
                        <TableRow key={league.id} className="bg-red-50/50">
                          <TableCell className="font-medium">
                            {league.providerName}
                          </TableCell>
                          <TableCell>{league.providerLeagueName}</TableCell>
                          <TableCell>
                            {league.isResolved ? (
                              <Badge
                                variant="secondary"
                                className="bg-green-100 text-green-800"
                              >
                                {league.resolutionType}
                              </Badge>
                            ) : (
                              <Badge
                                variant="secondary"
                                className="bg-yellow-100 text-yellow-800"
                              >
                                Nevyřešeno
                              </Badge>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </CardContent>
              </Card>
            ) : (
              <Card className="border-gray-200 bg-gray-50">
                <CardContent className="pt-4">
                  <div className="text-sm text-muted-foreground">
                    Žádné záznamy nebudou smazány.
                  </div>
                </CardContent>
              </Card>
            )}
          </>
        )}

        <DialogFooter>
          {result ? (
            <Button onClick={handleClose}>Zavřít</Button>
          ) : (
            <>
              <Button variant="outline" onClick={handleClose}>
                Zrušit
              </Button>
              {data?.canCreateGlobalRule && (
                <Button
                  onClick={() => createMutation.mutate()}
                  disabled={createMutation.isPending}
                  variant={data.affectedLeagues.length > 0 ? "destructive" : "default"}
                >
                  {createMutation.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Vytvářím...
                    </>
                  ) : (
                    <>
                      <CheckCircle2 className="h-4 w-4 mr-2" />
                      Vytvořit pravidlo
                    </>
                  )}
                </Button>
              )}
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
