"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import Link from "next/link"
import { ArrowLeft, Database, Trash2, AlertTriangle } from "lucide-react"
import { ResetDatabaseDialog } from "@/components/ResetDatabaseDialog"

export default function AdminPage() {
  const [resetAllOpen, setResetAllOpen] = useState(false)
  const [resetDataOpen, setResetDataOpen] = useState(false)

  return (
    <div className="container mx-auto p-6">
      {/* Header */}
      <div className="mb-6">
        <Link href="/" className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4">
          <ArrowLeft className="w-4 h-4 mr-2" />
          Zpět na hlavní stránku
        </Link>
        <h1 className="text-3xl font-bold">Správa dat</h1>
        <p className="text-gray-600 mt-2">Nástroje pro správu a reset databáze</p>
      </div>

      {/* Warning Banner */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
        <div className="flex items-start">
          <AlertTriangle className="w-5 h-5 text-yellow-600 mr-3 mt-0.5" />
          <div>
            <h3 className="text-sm font-semibold text-yellow-900">Varování</h3>
            <p className="text-sm text-yellow-800 mt-1">
              Operace resetu databáze jsou nevratné. Před provedením se ujistěte, že rozumíte důsledkům.
            </p>
          </div>
        </div>
      </div>

      {/* Reset Options */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Reset All Data */}
        <Card className="p-6">
          <div className="flex items-start mb-4">
            <div className="bg-red-100 p-3 rounded-lg">
              <Database className="w-6 h-6 text-red-600" />
            </div>
            <div className="ml-4 flex-1">
              <h2 className="text-lg font-semibold text-gray-900">Reset všech dat</h2>
              <p className="text-sm text-gray-600 mt-1">
                Smaže vše kromě sportů a providerů
              </p>
            </div>
          </div>

          <div className="bg-gray-50 rounded p-3 mb-4">
            <p className="text-sm font-medium text-gray-700 mb-2">Co bude smazáno:</p>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>• Země a jejich mapování</li>
              <li>• Ligy a jejich mapování</li>
              <li>• Sezóny</li>
              <li>• Všechna importovaná data (kola, zápasy)</li>
            </ul>
            <p className="text-sm font-medium text-gray-700 mt-3 mb-1">Co zůstane:</p>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>✓ Sporty</li>
              <li>✓ Providery (BetExplorer, atd.)</li>
            </ul>
          </div>

          <Button
            variant="destructive"
            className="w-full"
            onClick={() => setResetAllOpen(true)}
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Reset všech dat
          </Button>
        </Card>

        {/* Reset Data Only */}
        <Card className="p-6">
          <div className="flex items-start mb-4">
            <div className="bg-orange-100 p-3 rounded-lg">
              <Database className="w-6 h-6 text-orange-600" />
            </div>
            <div className="ml-4 flex-1">
              <h2 className="text-lg font-semibold text-gray-900">Reset pouze dat</h2>
              <p className="text-sm text-gray-600 mt-1">
                Smaže jen importovaná data, zachová číselníky
              </p>
            </div>
          </div>

          <div className="bg-gray-50 rounded p-3 mb-4">
            <p className="text-sm font-medium text-gray-700 mb-2">Co bude smazáno:</p>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>• Kola (rounds)</li>
              <li>• Zápasy (matches)</li>
              <li>• Historie importů (import_jobs)</li>
            </ul>
            <p className="text-sm font-medium text-gray-700 mt-3 mb-1">Co zůstane:</p>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>✓ Sporty</li>
              <li>✓ Providery</li>
              <li>✓ Země</li>
              <li>✓ Ligy</li>
              <li>✓ Sezóny</li>
            </ul>
          </div>

          <Button
            variant="outline"
            className="w-full border-orange-300 text-orange-700 hover:bg-orange-50"
            onClick={() => setResetDataOpen(true)}
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Reset pouze dat
          </Button>
        </Card>
      </div>

      {/* Reset Dialogs */}
      <ResetDatabaseDialog
        open={resetAllOpen}
        onOpenChange={setResetAllOpen}
        resetType="all"
        title="Reset všech dat"
        description="Tato operace smaže všechny země, ligy, sezóny a importovaná data. Zachová pouze sporty a providery."
        dangerLevel="high"
      />

      <ResetDatabaseDialog
        open={resetDataOpen}
        onOpenChange={setResetDataOpen}
        resetType="data-only"
        title="Reset importovaných dat"
        description="Tato operace smaže všechna importovaná data (kola, zápasy, import jobs). Zachová všechny číselníky (země, ligy, sezóny)."
        dangerLevel="medium"
      />
    </div>
  )
}
