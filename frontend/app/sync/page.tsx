"use client";

import Link from "next/link";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ArrowLeft, Database, ScanLine, Download, Activity, Info } from "lucide-react";
import { ScanDialog } from "@/components/ScanDialog";
import { CacheTablesView } from "@/components/CacheTablesView";

const BET_EXPLORER_PROVIDER_ID = "a0000000-0000-0000-0000-000000000001";

export default function SyncPage() {
  return (
    <div className="container mx-auto py-8 space-y-6">
      {/* Header */}
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
          <h1 className="text-3xl font-bold">Provider Synchronization</h1>
          <p className="text-muted-foreground">
            3-step workflow: Scan → Preview → Import
          </p>
        </div>
        <div className="flex gap-2">
          <Link href="/mappings">
            <Button variant="outline">
              <Database className="mr-2 h-4 w-4" />
              Mapování Názvů
            </Button>
          </Link>
          <Link href="/jobs">
            <Button variant="outline">
              <Activity className="mr-2 h-4 w-4" />
              Monitor Jobs
            </Button>
          </Link>
        </div>
      </div>

      {/* Workflow Info */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          <div className="space-y-2">
            <p className="font-semibold">Jak synchronizace funguje:</p>
            <ol className="list-decimal list-inside space-y-1 text-sm">
              <li>
                <strong>SCAN</strong> - Načte data z BetExplorer do cache tabulek
              </li>
              <li>
                <strong>PREVIEW</strong> - Zkontroluj data před importem (v tabulkách níže)
              </li>
              <li>
                <strong>IMPORT</strong> - Vyber položky a importuj je do hlavní databáze
              </li>
            </ol>
          </div>
        </AlertDescription>
      </Alert>

      {/* Step 1: SCAN */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <ScanLine className="h-5 w-5" />
            <div>
              <CardTitle>Krok 1: Scan Provider Data</CardTitle>
              <CardDescription>
                Načte data z BetExplorer do dočasných cache tabulek
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Spusť scan pro jednotlivé typy dat. Data se uloží do cache a můžeš je
              zkontrolovat před importem.
            </p>

            <div className="flex flex-wrap gap-3">
              <ScanDialog
                entityType="Countries"
                trigger={
                  <Button variant="default">
                    <ScanLine className="mr-2 h-4 w-4" />
                    Scan Countries
                  </Button>
                }
              />

              <ScanDialog
                entityType="Leagues"
                trigger={
                  <Button variant="default">
                    <ScanLine className="mr-2 h-4 w-4" />
                    Scan Leagues
                  </Button>
                }
              />

              <ScanDialog
                entityType="Seasons"
                trigger={
                  <Button variant="default">
                    <ScanLine className="mr-2 h-4 w-4" />
                    Scan Seasons
                  </Button>
                }
              />
            </div>

            <Alert className="bg-blue-50 border-blue-200">
              <Database className="h-4 w-4 text-blue-600" />
              <AlertDescription className="text-blue-900 text-sm">
                Scan operace běží na pozadí. Můžeš sledovat průběh na stránce{" "}
                <Link href="/jobs" className="font-semibold underline">
                  Job Monitoring
                </Link>
                .
              </AlertDescription>
            </Alert>
          </div>
        </CardContent>
      </Card>

      {/* Step 2 & 3: PREVIEW + IMPORT */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Download className="h-5 w-5" />
            <div>
              <CardTitle>Krok 2 & 3: Preview a Import</CardTitle>
              <CardDescription>
                Zkontroluj nascanovaná data a vyber co chceš importovat
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <CacheTablesView providerId={BET_EXPLORER_PROVIDER_ID} />
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Links</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Link href="/jobs">
              <Button variant="outline" className="w-full">
                <Activity className="mr-2 h-4 w-4" />
                Job Monitoring
              </Button>
            </Link>
            <Link href="/countries">
              <Button variant="outline" className="w-full">
                Manage Countries
              </Button>
            </Link>
            <Link href="/leagues">
              <Button variant="outline" className="w-full">
                Manage Leagues
              </Button>
            </Link>
          </div>
        </CardContent>
      </Card>

      {/* Hangfire Dashboard Link */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription className="text-sm">
          Pro pokročilý monitoring background jobů můžeš použít{" "}
          <a
            href="http://localhost:3001/hangfire"
            target="_blank"
            rel="noopener noreferrer"
            className="font-semibold underline"
          >
            Hangfire Dashboard
          </a>
          .
        </AlertDescription>
      </Alert>
    </div>
  );
}
