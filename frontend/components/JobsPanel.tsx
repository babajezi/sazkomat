"use client";

import { useEffect, useRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Loader2, CheckCircle2, XCircle, Clock, Activity } from "lucide-react";
import { ProviderLogo } from "@/components/ProviderLogo";
import { SyncJob, SyncJobStatus, SyncEntityType, DataProvider } from "@/lib/api/types";
import { formatDistanceToNow } from "date-fns";
import { cs } from "date-fns/locale";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";

interface JobsPanelProps {
  providerId: string;
  maxJobs?: number;
  refreshInterval?: number;
}

const statusConfig: Record<SyncJobStatus, { icon: React.ReactNode; color: string; label: string }> = {
  [SyncJobStatus.Pending]: {
    icon: <Clock className="h-4 w-4" />,
    color: "bg-yellow-100 text-yellow-800 border-yellow-200",
    label: "Čeká"
  },
  [SyncJobStatus.Running]: {
    icon: <Loader2 className="h-4 w-4 animate-spin" />,
    color: "bg-blue-100 text-blue-800 border-blue-200",
    label: "Běží"
  },
  [SyncJobStatus.Completed]: {
    icon: <CheckCircle2 className="h-4 w-4" />,
    color: "bg-green-100 text-green-800 border-green-200",
    label: "Hotovo"
  },
  [SyncJobStatus.PartiallyCompleted]: {
    icon: <CheckCircle2 className="h-4 w-4" />,
    color: "bg-orange-100 text-orange-800 border-orange-200",
    label: "Částečně"
  },
  [SyncJobStatus.Failed]: {
    icon: <XCircle className="h-4 w-4" />,
    color: "bg-red-100 text-red-800 border-red-200",
    label: "Selhalo"
  },
  [SyncJobStatus.Cancelled]: {
    icon: <XCircle className="h-4 w-4" />,
    color: "bg-gray-100 text-gray-800 border-gray-200",
    label: "Zrušeno"
  }
};

const entityTypeLabels: Record<SyncEntityType, string> = {
  [SyncEntityType.Countries]: "Země",
  [SyncEntityType.Leagues]: "Ligy",
  [SyncEntityType.Seasons]: "Sezóny",
  [SyncEntityType.Rounds]: "Kola",
  [SyncEntityType.CountriesAndLeagues]: "Země + Ligy"
};

export function JobsPanel({ providerId, maxJobs = 5, refreshInterval = 3000 }: JobsPanelProps) {
  const queryClient = useQueryClient();
  const previousJobsRef = useRef<Map<string, SyncJobStatus>>(new Map());

  const { data: jobs = [], isLoading } = useQuery<SyncJob[]>({
    queryKey: ["sync-jobs", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/jobs/recent?providerId=${providerId}&count=${maxJobs}`);
      if (!res.ok) throw new Error("Failed to fetch jobs");
      return res.json();
    },
    refetchInterval: refreshInterval,
  });

  // Detect job completion and refresh cache tables
  useEffect(() => {
    const previousJobs = previousJobsRef.current;
    let shouldRefreshCache = false;

    for (const job of jobs) {
      const prevStatus = previousJobs.get(job.id);
      const isNowCompleted = job.status === SyncJobStatus.Completed ||
                            job.status === SyncJobStatus.PartiallyCompleted ||
                            job.status === SyncJobStatus.Failed;
      const wasRunning = prevStatus === SyncJobStatus.Running ||
                        prevStatus === SyncJobStatus.Pending;

      // If job transitioned from running to completed, trigger cache refresh
      if (wasRunning && isNowCompleted) {
        shouldRefreshCache = true;
        console.log(`Job ${job.id} completed with status ${job.status}, refreshing cache...`);
      }
    }

    // Update previous jobs map
    const newMap = new Map<string, SyncJobStatus>();
    for (const job of jobs) {
      newMap.set(job.id, job.status);
    }
    previousJobsRef.current = newMap;

    // Refresh cache tables if any job completed
    if (shouldRefreshCache) {
      queryClient.invalidateQueries({ queryKey: ["provider-countries", providerId] });
      queryClient.invalidateQueries({ queryKey: ["provider-leagues", providerId] });
      queryClient.invalidateQueries({ queryKey: ["provider-seasons", providerId] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-leagues", providerId] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries", providerId] });
      queryClient.invalidateQueries({ queryKey: ["unmatched-countries-stats"] });
    }
  }, [jobs, providerId, queryClient]);

  const { data: provider } = useQuery<DataProvider>({
    queryKey: ["provider", providerId],
    queryFn: async () => {
      const res = await fetch(`${API_URL}/api/config/providers/${providerId}`);
      if (!res.ok) throw new Error("Failed to fetch provider");
      return res.json();
    },
    enabled: !!providerId,
  });

  // Check if any job is running
  const hasRunningJob = jobs.some(job =>
    job.status === SyncJobStatus.Running || job.status === SyncJobStatus.Pending
  );

  if (isLoading) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Průběh jobů
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-4">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        </CardContent>
      </Card>
    );
  }

  if (jobs.length === 0) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Průběh jobů
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground text-center py-4">
            Žádné nedávné joby
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={hasRunningJob ? "border-blue-300 bg-blue-50/30" : ""}>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Activity className={`h-4 w-4 ${hasRunningJob ? "text-blue-600" : ""}`} />
          Průběh jobů
          {provider && (
            <div className="flex items-center gap-2 ml-2 px-2 py-1 bg-muted rounded-md">
              <ProviderLogo provider={provider} size="sm" />
              <span className="text-sm font-normal text-muted-foreground">{provider.name}</span>
            </div>
          )}
          {hasRunningJob && (
            <Badge variant="outline" className="ml-auto bg-blue-100 text-blue-800 border-blue-200">
              <Loader2 className="h-3 w-3 animate-spin mr-1" />
              Aktivní
            </Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {jobs.map((job) => {
          const config = statusConfig[job.status];
          const entityLabel = entityTypeLabels[job.entityType] || job.entityType;

          return (
            <div
              key={job.id}
              className={`flex items-center justify-between p-3 rounded-lg border ${
                job.status === SyncJobStatus.Running
                  ? "bg-blue-50 border-blue-200"
                  : "bg-muted/50"
              }`}
            >
              <div className="flex items-center gap-3">
                <div className={`p-1.5 rounded-full ${config.color}`}>
                  {config.icon}
                </div>
                <div>
                  <div className="font-medium text-sm">
                    {job.jobType} {entityLabel}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {job.startedAt
                      ? formatDistanceToNow(new Date(job.startedAt), { addSuffix: true, locale: cs })
                      : formatDistanceToNow(new Date(job.createdAt), { addSuffix: true, locale: cs })
                    }
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="outline" className={config.color}>
                  {config.label}
                </Badge>
              </div>
            </div>
          );
        })}

        {jobs.some(j => j.status === SyncJobStatus.Failed && j.errorMessage) && (
          <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-800">
            <strong>Poslední chyba:</strong>{" "}
            {jobs.find(j => j.status === SyncJobStatus.Failed)?.errorMessage}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
