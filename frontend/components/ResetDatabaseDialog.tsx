"use client"

import { useState } from "react"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { AlertTriangle, Loader2, CheckCircle2 } from "lucide-react"

interface ResetDatabaseDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  resetType: "all" | "data-only"
  title: string
  description: string
  dangerLevel: "high" | "medium"
}

export function ResetDatabaseDialog({
  open,
  onOpenChange,
  resetType,
  title,
  description,
  dangerLevel
}: ResetDatabaseDialogProps) {
  const [confirmText, setConfirmText] = useState("")
  const [confirmed, setConfirmed] = useState(false)
  const [success, setSuccess] = useState(false)
  const queryClient = useQueryClient()

  const resetMutation = useMutation({
    mutationFn: async () => {
      const endpoint = resetType === "all"
        ? `${process.env.NEXT_PUBLIC_API_URL}/api/database/reset/all`
        : `${process.env.NEXT_PUBLIC_API_URL}/api/database/reset/data-only`

      const response = await fetch(endpoint, {
        method: "POST",
      })

      if (!response.ok) {
        const error = await response.json()
        throw new Error(error.error || "Reset failed")
      }

      return response.json()
    },
    onSuccess: () => {
      setSuccess(true)
      // Invalidate všechny dotazy
      queryClient.invalidateQueries()

      // Po 2 sekundách zavřít dialog a resetovat stav
      setTimeout(() => {
        handleClose()
      }, 2000)
    },
  })

  const handleClose = () => {
    onOpenChange(false)
    setConfirmText("")
    setConfirmed(false)
    setSuccess(false)
  }

  const handleReset = () => {
    if (confirmText === "RESET" && confirmed) {
      resetMutation.mutate()
    }
  }

  const canSubmit = confirmText === "RESET" && confirmed && !resetMutation.isPending

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center">
            <AlertTriangle
              className={`w-5 h-5 mr-2 ${
                dangerLevel === "high" ? "text-red-600" : "text-orange-600"
              }`}
            />
            {title}
          </DialogTitle>
          <DialogDescription>
            {description}
          </DialogDescription>
        </DialogHeader>

        {success ? (
          <div className="py-6 text-center">
            <CheckCircle2 className="w-12 h-12 text-green-600 mx-auto mb-3" />
            <p className="text-lg font-semibold text-green-900">Reset dokončen!</p>
            <p className="text-sm text-gray-600 mt-1">Dialog se automaticky zavře...</p>
          </div>
        ) : (
          <>
            <div className="space-y-4 py-4">
              {/* Warning */}
              <div
                className={`rounded-lg p-3 ${
                  dangerLevel === "high"
                    ? "bg-red-50 border border-red-200"
                    : "bg-orange-50 border border-orange-200"
                }`}
              >
                <p
                  className={`text-sm font-semibold ${
                    dangerLevel === "high" ? "text-red-900" : "text-orange-900"
                  }`}
                >
                  ⚠️ Tato akce je nevratná!
                </p>
                <p
                  className={`text-sm mt-1 ${
                    dangerLevel === "high" ? "text-red-800" : "text-orange-800"
                  }`}
                >
                  Po provedení resetu nelze data obnovit.
                </p>
              </div>

              {/* Confirmation Checkbox */}
              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="confirm-checkbox"
                  checked={confirmed}
                  onChange={(e) => setConfirmed(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <label
                  htmlFor="confirm-checkbox"
                  className="text-sm text-gray-700 cursor-pointer select-none"
                >
                  Rozumím, že tato akce je nevratná a nelze ji vrátit zpět
                </label>
              </div>

              {/* Confirm Text Input */}
              <div>
                <label htmlFor="confirm-text" className="block text-sm font-medium text-gray-700 mb-2">
                  Pro potvrzení napište <span className="font-mono font-bold text-red-600">RESET</span>:
                </label>
                <input
                  id="confirm-text"
                  type="text"
                  value={confirmText}
                  onChange={(e) => setConfirmText(e.target.value)}
                  placeholder="Zadejte RESET"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono"
                  disabled={resetMutation.isPending}
                />
              </div>
            </div>

            <DialogFooter>
              <Button
                variant="outline"
                onClick={handleClose}
                disabled={resetMutation.isPending}
              >
                Zrušit
              </Button>
              <Button
                variant="destructive"
                onClick={handleReset}
                disabled={!canSubmit}
              >
                {resetMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Probíhá reset...
                  </>
                ) : (
                  "Potvrdit reset"
                )}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
