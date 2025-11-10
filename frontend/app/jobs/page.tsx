"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  CheckCircle2,
  Circle,
  XCircle,
  Loader2,
  AlertCircle,
  ArrowLeft,
  Clock,
  Activity,
} from "lucide-react";
import type { SyncJob, SyncJobStatus, SyncJobType } from "@/lib/api/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001";
const BET_EXPLORER_PROVIDER_ID = "a0000000-0000-0000-0000-000000000001";

export default function JobsPage() {
  const [selectedJob, setSelectedJob] = useState<string | null>(null);

  // Fetch recent jobs
  const { data: jobs = [], isLoading: loadingJobs } = useQuery<SyncJob[]>({
    queryKey: ["sync-jobs", BET_EXPLORER_PROVIDER_ID],
    queryFn: async () => {
      const res = await fetch(
        `${API_URL}/api/jobs/recent?providerId=${BET_EXPLORER_PROVIDER_ID}&count=50`
      );
      if (!res.ok) throw new Error("Failed to fetch jobs");
      return res.json();
    },
    refetchInterval: 5000, // Poll every 5 seconds
  });

  // Fetch selected job details
  const { data: jobDetails } = useQuery<SyncJob>({
    queryKey: ["sync-job", selectedJob],
    queryFn: async () => {
      if (!selectedJob) throw new Error("No job selected");
      const res = await fetch(`${API_URL}/api/jobs/${selectedJob}`);
      if (!res.ok) throw new Error("Failed to fetch job details");
      return res.json();
    },
    enabled: !!selectedJob,
    refetchInterval: (data) => {
      // Stop polling if job is completed or failed
      return data?.status === "Completed" || data?.status === "Failed" ? false : 2000;
    },
  });

  const getStatusIcon = (status: SyncJobStatus) => {
    switch (status) {
      case "Pending":
        return <Clock className="h-4 w-4 text-gray-400" />;
      case "Running":
        return <Loader2 className="h-4 w-4 text-blue-600 animate-spin" />;
      case "Completed":
        return <CheckCircle2 className="h-4 w-4 text-green-600" />;
      case "Failed":
        return <XCircle className="h-4 w-4 text-red-600" />;
      default:
        return <Circle className="h-4 w-4" />;
    }
  };

  const getStatusBadge = (status: SyncJobStatus) => {
    const variants: Record<SyncJobStatus, "default" | "secondary" | "destructive" | "outline"> = {
      Pending: "outline",
      Running: "default",
      Completed: "secondary",
      Failed: "destructive",
    };
    return <Badge variant={variants[status]}>{status}</Badge>;
  };

  const getJobTypeLabel = (type: SyncJobType) => {
    switch (type) {
      case "Scan":
        return "Scan";
      case "Import":
        return "Import";
      case "LiveSync":
        return "Live Sync";
      default:
        return type;
    }
  };

  const formatDuration = (startedAt: string | null, completedAt: string | null) => {
    if (!startedAt) return "—";
    const start = new Date(startedAt).getTime();
    const end = completedAt ? new Date(completedAt).getTime() : Date.now();
    const duration = Math.floor((end - start) / 1000);

    if (duration < 60) return `${duration}s`;
    if (duration < 3600) return `${Math.floor(duration / 60)}m ${duration % 60}s`;
    return `${Math.floor(duration / 3600)}h ${Math.floor((duration % 3600) / 60)}m`;
  };

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString("cs-CZ", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  };

  if (loadingJobs) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  const runningJobs = jobs.filter((j) => j.status === "Running");
  const completedJobs = jobs.filter((j) => j.status === "Completed");
  const failedJobs = jobs.filter((j) => j.status === "Failed");
  const pendingJobs = jobs.filter((j) => j.status === "Pending");

  return (
    <div className="container mx-auto py-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-4 mb-2">
            <Link href="/">
              <Button variant="ghost" size="sm">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Zpět na úvodní stránku
              </Button>
            </Link>
          </div>
          <h1 className="text-3xl font-bold">Job Monitoring</h1>
          <p className="text-muted-foreground">
            Sledování běžících a dokončených úloh
          </p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Běžící</CardTitle>
            <Activity className="h-4 w-4 text-blue-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{runningJobs.length}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Čekající</CardTitle>
            <Clock className="h-4 w-4 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{pendingJobs.length}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Dokončené</CardTitle>
            <CheckCircle2 className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{completedJobs.length}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Chybné</CardTitle>
            <XCircle className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{failedJobs.length}</div>
          </CardContent>
        </Card>
      </div>

      {/* Running Jobs Alert */}
      {runningJobs.length > 0 && (
        <Alert>
          <Activity className="h-4 w-4" />
          <AlertDescription>
            {runningJobs.length} {runningJobs.length === 1 ? "úloha běží" : "úlohy běží"}
          </AlertDescription>
        </Alert>
      )}

      {/* Jobs Table */}
      <Card>
        <CardHeader>
          <CardTitle>Poslední úlohy (50)</CardTitle>
          <CardDescription>
            Automatická aktualizace každých 5 sekund
          </CardDescription>
        </CardHeader>
        <CardContent>
          {jobs.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">
              Zatím nejsou žádné úlohy
            </p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12"></TableHead>
                    <TableHead>Typ</TableHead>
                    <TableHead>Entita</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Vytvořeno</TableHead>
                    <TableHead>Začátek</TableHead>
                    <TableHead>Konec</TableHead>
                    <TableHead>Trvání</TableHead>
                    <TableHead className="w-24">ID</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {jobs.map((job) => (
                    <TableRow
                      key={job.id}
                      className={`cursor-pointer ${selectedJob === job.id ? "bg-muted" : ""}`}
                      onClick={() => setSelectedJob(job.id)}
                    >
                      <TableCell>{getStatusIcon(job.status)}</TableCell>
                      <TableCell className="font-medium">
                        {getJobTypeLabel(job.jobType)}
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">{job.entityType}</Badge>
                      </TableCell>
                      <TableCell>{getStatusBadge(job.status)}</TableCell>
                      <TableCell className="text-sm">
                        {formatTimestamp(job.createdAt)}
                      </TableCell>
                      <TableCell className="text-sm">
                        {job.startedAt ? formatTimestamp(job.startedAt) : "—"}
                      </TableCell>
                      <TableCell className="text-sm">
                        {job.completedAt ? formatTimestamp(job.completedAt) : "—"}
                      </TableCell>
                      <TableCell className="text-sm font-mono">
                        {formatDuration(job.startedAt, job.completedAt)}
                      </TableCell>
                      <TableCell className="text-xs font-mono">
                        {job.id.slice(0, 8)}...
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Job Details */}
      {selectedJob && jobDetails && (
        <Card>
          <CardHeader>
            <CardTitle>Detail úlohy</CardTitle>
            <CardDescription>Job ID: {selectedJob}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Typ úlohy</p>
                <p className="text-sm">{getJobTypeLabel(jobDetails.jobType)}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Typ entity</p>
                <p className="text-sm">{jobDetails.entityType}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Status</p>
                <div className="mt-1">{getStatusBadge(jobDetails.status)}</div>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Trvání</p>
                <p className="text-sm font-mono">
                  {formatDuration(jobDetails.startedAt, jobDetails.completedAt)}
                </p>
              </div>
            </div>

            {jobDetails.entityIds && jobDetails.entityIds.length > 0 && (
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-2">
                  Entity IDs ({jobDetails.entityIds.length})
                </p>
                <div className="flex flex-wrap gap-2">
                  {jobDetails.entityIds.slice(0, 10).map((id) => (
                    <Badge key={id} variant="secondary" className="font-mono text-xs">
                      {id.slice(0, 8)}
                    </Badge>
                  ))}
                  {jobDetails.entityIds.length > 10 && (
                    <Badge variant="outline">+{jobDetails.entityIds.length - 10} dalších</Badge>
                  )}
                </div>
              </div>
            )}

            {jobDetails.errorMessage && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{jobDetails.errorMessage}</AlertDescription>
              </Alert>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
