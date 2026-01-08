"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import Link from "next/link"
import { ArrowLeft, Database, Trash2, AlertTriangle, Users, ChevronRight, Settings2 } from "lucide-react"
import { SelectiveResetDialog } from "@/components/SelectiveResetDialog"
import { useUser } from "@/contexts/UserContext"
import { useRouter } from "next/navigation"

export default function AdminPage() {
  const [selectiveResetOpen, setSelectiveResetOpen] = useState(false)
  const { isAdmin, isAuthenticated, isLoading } = useUser()
  const router = useRouter()

  // Redirect if not admin
  useEffect(() => {
    if (!isLoading && (!isAuthenticated || !isAdmin)) {
      router.push("/")
    }
  }, [isAuthenticated, isAdmin, isLoading, router])

  if (isLoading) {
    return (
      <div className="container mx-auto p-6">
        <div className="animate-pulse">Loading...</div>
      </div>
    )
  }

  if (!isAdmin) {
    return null
  }

  return (
    <div className="container mx-auto p-6">
      {/* Header */}
      <div className="mb-6">
        <Link href="/" className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4">
          <ArrowLeft className="w-4 h-4 mr-2" />
          Zpět na hlavní stránku
        </Link>
        <h1 className="text-3xl font-bold">Administrace</h1>
        <p className="text-gray-600 mt-2">Nástroje pro správu systému</p>
      </div>

      {/* Admin Navigation */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold mb-4">Administrační nástroje</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/admin/users">
            <Card className="p-4 hover:bg-gray-50 transition-colors cursor-pointer">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="bg-blue-100 p-2 rounded-lg">
                    <Users className="w-5 h-5 text-blue-600" />
                  </div>
                  <div>
                    <h3 className="font-semibold">Správa uživatelů</h3>
                    <p className="text-sm text-gray-600">Schvalování a správa registrací</p>
                  </div>
                </div>
                <ChevronRight className="w-5 h-5 text-gray-400" />
              </div>
            </Card>
          </Link>

          <Link href="/admin/queues">
            <Card className="p-4 hover:bg-gray-50 transition-colors cursor-pointer">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="bg-purple-100 p-2 rounded-lg">
                    <Database className="w-5 h-5 text-purple-600" />
                  </div>
                  <div>
                    <h3 className="font-semibold">Fronty úloh</h3>
                    <p className="text-sm text-gray-600">Správa background jobů</p>
                  </div>
                </div>
                <ChevronRight className="w-5 h-5 text-gray-400" />
              </div>
            </Card>
          </Link>
        </div>
      </div>

      <hr className="my-6" />

      <h2 className="text-lg font-semibold mb-4">Reset databáze</h2>

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

      {/* Selective Reset */}
      <Card className="p-6">
        <div className="flex items-start mb-4">
          <div className="bg-orange-100 p-3 rounded-lg">
            <Settings2 className="w-6 h-6 text-orange-600" />
          </div>
          <div className="ml-4 flex-1">
            <h2 className="text-lg font-semibold text-gray-900">Selektivní reset dat</h2>
            <p className="text-sm text-gray-600 mt-1">
              Vyberte přesně, které entity chcete smazat
            </p>
          </div>
        </div>

        <div className="bg-gray-50 rounded p-3 mb-4">
          <p className="text-sm font-medium text-gray-700 mb-2">Možnosti:</p>
          <ul className="text-sm text-gray-600 space-y-1">
            <li>• <strong>Importovaná data:</strong> Kola, zápasy, import joby</li>
            <li>• <strong>Provider cache:</strong> Provider země/ligy/sezóny, sync joby</li>
            <li>• <strong>Mapování:</strong> Mapování názvů zemí a lig</li>
            <li>• <strong>Konfigurace:</strong> Ligy, země, sezóny a jejich vazby</li>
          </ul>
          <p className="text-sm text-gray-500 mt-3">
            Předvolby umožňují rychlý výběr pro běžné scénáře (např. reset pouze lig).
          </p>
        </div>

        <Button
          variant="outline"
          className="w-full border-orange-300 text-orange-700 hover:bg-orange-50"
          onClick={() => setSelectiveResetOpen(true)}
        >
          <Trash2 className="w-4 h-4 mr-2" />
          Otevřít selektivní reset
        </Button>
      </Card>

      {/* Selective Reset Dialog */}
      <SelectiveResetDialog
        open={selectiveResetOpen}
        onOpenChange={setSelectiveResetOpen}
      />
    </div>
  )
}
