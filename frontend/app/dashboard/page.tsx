"use client";

import { useQuery } from "@tanstack/react-query";
import { importApi } from "@/lib/api/client";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip, BarChart, Bar, XAxis, YAxis, CartesianGrid } from "recharts";
import { ImportJobStatus } from "@/lib/api/types";

export default function DashboardPage() {
  const { data: stats, isLoading, error } = useQuery({
    queryKey: ["dashboardStats"],
    queryFn: () => importApi.getDashboardStats(),
  });

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání statistik...</div>
      </div>
    );
  }

  if (error || !stats) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-destructive">Chyba</CardTitle>
          </CardHeader>
          <CardContent>
            <p>Nelze načíst dashboard statistiky</p>
            <p className="text-sm text-gray-500 mt-2">
              {error instanceof Error ? error.message : "Neznámá chyba"}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Prepare data for pie chart
  const resultsData = [
    { name: "Domácí výhry", value: stats.results.homeWins, percentage: stats.results.homeWinPercentage },
    { name: "Remízy", value: stats.results.draws, percentage: stats.results.drawPercentage },
    { name: "Výhry hostí", value: stats.results.awayWins, percentage: stats.results.awayWinPercentage },
  ];

  const COLORS = ["#10b981", "#6b7280", "#ef4444"];

  // Prepare data for top leagues bar chart
  const topLeaguesData = stats.topLeagues.map(l => ({
    name: `${l.countryFlag} ${l.leagueName}`,
    kola: l.roundsCount,
    zápasy: l.matchesCount,
  }));

  // Prepare data for seasons bar chart
  const seasonsData = stats.seasonBreakdown.map(s => ({
    name: s.season,
    kola: s.roundsCount,
    zápasy: s.matchesCount,
  }));

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("cs-CZ");
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
            <p className="text-gray-600">
              Přehled importovaných dat a statistik
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        {/* KPI Cards */}
        <div className="grid gap-4 md:grid-cols-4 mb-8">
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Ligy s daty</CardDescription>
              <CardTitle className="text-4xl">{stats.overall.totalLeagues}</CardTitle>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Celkem kol</CardDescription>
              <CardTitle className="text-4xl">{stats.overall.totalRounds.toLocaleString()}</CardTitle>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Sezóny</CardDescription>
              <CardTitle className="text-4xl">{stats.overall.totalSeasons}</CardTitle>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Celkem zápasů</CardDescription>
              <CardTitle className="text-4xl">{stats.overall.totalMatches.toLocaleString()}</CardTitle>
            </CardHeader>
          </Card>
        </div>

        {/* Charts Row */}
        <div className="grid gap-6 md:grid-cols-2 mb-8">
          {/* Pie Chart - Match Results */}
          <Card>
            <CardHeader>
              <CardTitle>Rozdělení výsledků zápasů</CardTitle>
              <CardDescription>
                Home / Draw / Away distribuce
              </CardDescription>
            </CardHeader>
            <CardContent className="h-80">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={resultsData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={(entry) => `${entry.name}: ${entry.percentage}%`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {resultsData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Bar Chart - Top Leagues */}
          <Card>
            <CardHeader>
              <CardTitle>Top 10 lig podle počtu kol</CardTitle>
              <CardDescription>
                Ligy s největším množstvím importovaných dat
              </CardDescription>
            </CardHeader>
            <CardContent className="h-80">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={topLeaguesData} layout="horizontal">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" />
                  <YAxis dataKey="name" type="category" width={150} fontSize={12} />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="kola" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>

        {/* Seasons Bar Chart */}
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Rozložení dat podle sezón</CardTitle>
            <CardDescription>
              Počet kol a zápasů v jednotlivých sezónách
            </CardDescription>
          </CardHeader>
          <CardContent className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={seasonsData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Bar dataKey="kola" fill="#10b981" name="Kola" />
                <Bar dataKey="zápasy" fill="#6366f1" name="Zápasy" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Recent Import Jobs Table */}
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Poslední importy</CardTitle>
            <CardDescription>
              Historie posledních 10 import jobů
            </CardDescription>
          </CardHeader>
          <CardContent>
            {stats.recentJobs.length === 0 ? (
              <p className="text-center text-gray-500 py-4">
                Žádné importy zatím neproběhly
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left p-2">Liga</th>
                      <th className="text-left p-2">Status</th>
                      <th className="text-right p-2">Kola</th>
                      <th className="text-right p-2">Sezóny</th>
                      <th className="text-left p-2">Zahájeno</th>
                      <th className="text-left p-2">Dokončeno</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stats.recentJobs.map((job) => (
                      <tr key={job.jobId} className="border-b hover:bg-gray-50">
                        <td className="p-2">{job.leagueName}</td>
                        <td className="p-2">
                          <span
                            className={`px-2 py-1 rounded text-xs ${
                              job.status === ImportJobStatus.Completed
                                ? "bg-green-100 text-green-800"
                                : job.status === ImportJobStatus.Failed
                                ? "bg-red-100 text-red-800"
                                : job.status === ImportJobStatus.Running
                                ? "bg-blue-100 text-blue-800"
                                : "bg-gray-100 text-gray-800"
                            }`}
                          >
                            {job.status}
                          </span>
                        </td>
                        <td className="p-2 text-right">{job.processedRounds}</td>
                        <td className="p-2 text-right">{job.totalSeasons}</td>
                        <td className="p-2 text-xs text-gray-600">
                          {formatDate(job.startedAt)}
                        </td>
                        <td className="p-2 text-xs text-gray-600">
                          {job.completedAt ? formatDate(job.completedAt) : "-"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Top Leagues Details Table */}
        <Card>
          <CardHeader>
            <CardTitle>Detaily lig</CardTitle>
            <CardDescription>
              Podrobné statistiky top lig
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-2">Liga</th>
                    <th className="text-left p-2">Sport</th>
                    <th className="text-right p-2">Kola</th>
                    <th className="text-right p-2">Sezóny</th>
                    <th className="text-right p-2">Zápasy</th>
                    <th className="text-left p-2">Poslední import</th>
                  </tr>
                </thead>
                <tbody>
                  {stats.topLeagues.map((league) => (
                    <tr key={league.leagueId} className="border-b hover:bg-gray-50">
                      <td className="p-2">
                        {league.countryFlag} {league.leagueName}
                        <div className="text-xs text-gray-500">{league.countryName}</div>
                      </td>
                      <td className="p-2">{league.sportName}</td>
                      <td className="p-2 text-right">{league.roundsCount}</td>
                      <td className="p-2 text-right">{league.seasonsCount}</td>
                      <td className="p-2 text-right">{league.matchesCount.toLocaleString()}</td>
                      <td className="p-2 text-xs text-gray-600">
                        {league.lastImport ? formatDate(league.lastImport) : "-"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
