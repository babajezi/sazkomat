"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { analyticsApi } from "@/lib/api/client";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
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
import { AnalyticsResultTable } from "@/components/analytics/AnalyticsResultTable";
import { AnalyticsChart } from "@/components/analytics/AnalyticsChart";
import {
  PlayCircle,
  Trash2,
  Star,
  StarOff,
  Clock,
  BarChart3,
  Table as TableIcon,
  LineChart as LineChartIcon,
  PieChart as PieChartIcon,
} from "lucide-react";
import type { AnalyticsResult, AnalyticsViewListItem } from "@/lib/api/types";

const vizIcons: Record<string, React.ReactNode> = {
  table: <TableIcon className="w-4 h-4" />,
  barChart: <BarChart3 className="w-4 h-4" />,
  lineChart: <LineChartIcon className="w-4 h-4" />,
  pieChart: <PieChartIcon className="w-4 h-4" />,
};

export default function AnalyticsPage() {
  const queryClient = useQueryClient();
  const [activeResult, setActiveResult] = useState<AnalyticsResult | null>(null);
  const [activeVizType, setActiveVizType] = useState<string>("table");
  const [deleteViewId, setDeleteViewId] = useState<string | null>(null);

  const { data: views, isLoading } = useQuery({
    queryKey: ["analytics-views"],
    queryFn: analyticsApi.getViews,
  });

  const executeMutation = useMutation({
    mutationFn: (id: string) => analyticsApi.executeView(id),
    onSuccess: (result, id) => {
      setActiveResult(result);
      const view = views?.find((v) => v.id === id);
      setActiveVizType(view?.visualizationType || "table");
      queryClient.invalidateQueries({ queryKey: ["analytics-views"] });
    },
  });

  const favoriteMutation = useMutation({
    mutationFn: (id: string) => analyticsApi.toggleFavorite(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["analytics-views"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => analyticsApi.deleteView(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["analytics-views"] });
      setDeleteViewId(null);
    },
  });

  return (
    <div className="container mx-auto px-4 py-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Analytika</h1>
          <p className="text-sm text-gray-500 mt-1">
            Uložené analytické pohledy. Nové pohledy vytváříte přes <code className="bg-gray-100 px-1 rounded">/analyze</code> v Claude Code.
          </p>
        </div>
      </div>

      {/* Active Result */}
      {activeResult && (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-lg">Výsledek</CardTitle>
              <div className="flex items-center gap-2">
                <Badge variant="outline">
                  {activeResult.totalRows} řádků
                </Badge>
                <Badge variant="outline">
                  <Clock className="w-3 h-3 mr-1" />
                  {activeResult.executionMs}ms
                </Badge>
                {/* Viz type switcher */}
                <div className="flex border rounded-md">
                  {(["table", "barChart", "lineChart", "pieChart"] as const).map((t) => (
                    <Button
                      key={t}
                      variant={activeVizType === t ? "default" : "ghost"}
                      size="sm"
                      className="h-7 px-2"
                      onClick={() => setActiveVizType(t)}
                    >
                      {vizIcons[t]}
                    </Button>
                  ))}
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {activeVizType === "table" ? (
              <AnalyticsResultTable result={activeResult} />
            ) : (
              <AnalyticsChart result={activeResult} type={activeVizType} />
            )}
          </CardContent>
        </Card>
      )}

      {/* Saved Views Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(3)].map((_, i) => (
            <Card key={i} className="animate-pulse">
              <CardHeader>
                <div className="h-5 bg-gray-200 rounded w-3/4" />
                <div className="h-4 bg-gray-100 rounded w-1/2 mt-2" />
              </CardHeader>
            </Card>
          ))}
        </div>
      ) : views && views.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {views.map((view) => (
            <ViewCard
              key={view.id}
              view={view}
              isExecuting={executeMutation.isPending && executeMutation.variables === view.id}
              onRun={() => executeMutation.mutate(view.id)}
              onToggleFavorite={() => favoriteMutation.mutate(view.id)}
              onDelete={() => setDeleteViewId(view.id)}
            />
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-12 text-center text-gray-500">
            <BarChart3 className="w-12 h-12 mx-auto mb-4 text-gray-300" />
            <p className="text-lg font-medium">Žádné uložené pohledy</p>
            <p className="text-sm mt-1">
              Použijte <code className="bg-gray-100 px-1 rounded">/analyze</code> v Claude Code
              pro vytvoření analytických pohledů.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Delete confirmation dialog */}
      <AlertDialog open={!!deleteViewId} onOpenChange={() => setDeleteViewId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Smazat pohled?</AlertDialogTitle>
            <AlertDialogDescription>
              Tato akce je nevratná. Pohled bude trvale odstraněn.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Zrušit</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteViewId && deleteMutation.mutate(deleteViewId)}
              className="bg-red-600 hover:bg-red-700"
            >
              Smazat
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ViewCard({
  view,
  isExecuting,
  onRun,
  onToggleFavorite,
  onDelete,
}: {
  view: AnalyticsViewListItem;
  isExecuting: boolean;
  onRun: () => void;
  onToggleFavorite: () => void;
  onDelete: () => void;
}) {
  return (
    <Card className="group hover:shadow-md transition-shadow">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-base truncate">{view.name}</CardTitle>
            {view.description && (
              <CardDescription className="mt-1 line-clamp-2">
                {view.description}
              </CardDescription>
            )}
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 w-7 p-0 shrink-0"
            onClick={onToggleFavorite}
          >
            {view.isFavorite ? (
              <Star className="w-4 h-4 text-yellow-500 fill-yellow-500" />
            ) : (
              <StarOff className="w-4 h-4 text-gray-400" />
            )}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="flex items-center gap-2 mb-3 flex-wrap">
          <Badge variant="secondary" className="text-xs">
            {vizIcons[view.visualizationType] || vizIcons.table}
            <span className="ml-1">{view.visualizationType}</span>
          </Badge>
          {view.tags?.split(",").map((tag) => (
            <Badge key={tag.trim()} variant="outline" className="text-xs">
              {tag.trim()}
            </Badge>
          ))}
        </div>
        <div className="flex items-center justify-between text-xs text-gray-500">
          <span>
            {view.executionCount > 0
              ? `${view.executionCount}x spuštěno · ${view.lastExecutionMs}ms`
              : "Zatím nespuštěno"}
          </span>
        </div>
        <div className="flex items-center gap-2 mt-3">
          <Button
            size="sm"
            className="flex-1"
            onClick={onRun}
            disabled={isExecuting}
          >
            <PlayCircle className="w-4 h-4 mr-1" />
            {isExecuting ? "Spouštím..." : "Spustit"}
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="h-8 w-8 p-0 text-red-500 hover:text-red-700"
            onClick={onDelete}
          >
            <Trash2 className="w-4 h-4" />
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
