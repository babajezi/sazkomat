"use client"

import { useState, useEffect } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { AlertTriangle, Loader2, CheckCircle2 } from "lucide-react"

interface EntityOption {
  id: string
  label: string
  defaultChecked: boolean
}

interface EntityCategory {
  name: string
  description?: string
  entities: EntityOption[]
}

// Static categories (non-binding entities)
const STATIC_ENTITY_CATEGORIES: EntityCategory[] = [
  {
    name: "Importovaná data",
    entities: [
      { id: "rounds", label: "Kola a zápasy", defaultChecked: true },
      { id: "import_jobs", label: "Import joby", defaultChecked: true },
      { id: "unmatched_leagues", label: "Nespárované ligy", defaultChecked: false },
    ]
  },
  {
    name: "Provider cache",
    entities: [
      { id: "provider_countries", label: "Provider země", defaultChecked: false },
      { id: "provider_leagues", label: "Provider ligy", defaultChecked: true },
      { id: "provider_seasons", label: "Provider sezóny", defaultChecked: false },
      { id: "sync_jobs", label: "Sync joby", defaultChecked: false },
    ]
  },
  {
    name: "Mapování",
    entities: [
      { id: "country_name_mappings", label: "Mapování zemí", defaultChecked: false },
      { id: "league_name_mappings", label: "Mapování lig", defaultChecked: true },
    ]
  },
  {
    name: "Konfigurace - záznamy",
    description: "Hlavní záznamy (vyžadují smazané vazby!)",
    entities: [
      { id: "leagues", label: "Ligy", defaultChecked: false },
      { id: "countries", label: "Země", defaultChecked: false },
      { id: "seasons", label: "Sezóny", defaultChecked: false },
    ]
  }
]

interface BindingsByProvider {
  [providerCode: string]: {
    league_providers?: number
    country_providers?: number
    league_seasons?: number
  }
}

interface SelectiveResetDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

// Get default checked entities (only static ones)
const getDefaultCheckedEntities = (): Set<string> => {
  const defaults = new Set<string>()
  STATIC_ENTITY_CATEGORIES.forEach(cat => {
    cat.entities.forEach(entity => {
      if (entity.defaultChecked) {
        defaults.add(entity.id)
      }
    })
  })
  return defaults
}

export function SelectiveResetDialog({ open, onOpenChange }: SelectiveResetDialogProps) {
  const [checkedEntities, setCheckedEntities] = useState<Set<string>>(new Set())
  const [checkedBindings, setCheckedBindings] = useState<Map<string, Set<string>>>(new Map()) // providerCode -> Set<bindingType>
  const [confirmText, setConfirmText] = useState("")
  const [confirmed, setConfirmed] = useState(false)
  const [success, setSuccess] = useState(false)
  const [deletedCounts, setDeletedCounts] = useState<Record<string, number>>({})
  const queryClient = useQueryClient()

  // Fetch entity counts when dialog opens
  const { data: entityCounts, isLoading: countsLoading } = useQuery({
    queryKey: ['entity-counts'],
    queryFn: async () => {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/database/counts`)
      if (!response.ok) throw new Error('Failed to fetch counts')
      return response.json() as Promise<Record<string, number>>
    },
    enabled: open,
    staleTime: 30000,
  })

  // Fetch binding counts per provider
  const { data: bindingCounts, isLoading: bindingsLoading } = useQuery({
    queryKey: ['binding-counts'],
    queryFn: async () => {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/database/counts/bindings`)
      if (!response.ok) throw new Error('Failed to fetch binding counts')
      return response.json() as Promise<BindingsByProvider>
    },
    enabled: open,
    staleTime: 30000,
  })

  // Initialize with default checked entities
  useEffect(() => {
    if (open) {
      setCheckedEntities(getDefaultCheckedEntities())
      setCheckedBindings(new Map())
    }
  }, [open])

  const resetMutation = useMutation({
    mutationFn: async ({ entities, bindings }: { entities: string[], bindings: Map<string, Set<string>> }) => {
      const allDeletedCounts: Record<string, number> = {}

      // Delete regular entities
      if (entities.length > 0) {
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/api/database/reset/selective`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ entities })
          }
        )

        if (!response.ok) {
          const error = await response.json()
          throw new Error(error.error || "Reset failed")
        }

        const data = await response.json()
        Object.assign(allDeletedCounts, data.deletedCounts || {})
      }

      // Delete bindings per provider
      for (const [providerCode, bindingTypes] of bindings.entries()) {
        if (bindingTypes.size === 0) continue

        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/api/database/reset/bindings/${providerCode}`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ bindingTypes: Array.from(bindingTypes) })
          }
        )

        if (!response.ok) {
          const error = await response.json()
          throw new Error(error.error || `Reset failed for provider ${providerCode}`)
        }

        const data = await response.json()
        // Prefix binding counts with provider code
        for (const [key, value] of Object.entries(data.deletedCounts || {})) {
          allDeletedCounts[`${providerCode}:${key}`] = value as number
        }
      }

      return { deletedCounts: allDeletedCounts }
    },
    onSuccess: (data) => {
      setSuccess(true)
      setDeletedCounts(data.deletedCounts || {})
      queryClient.invalidateQueries()

      setTimeout(() => {
        handleClose()
      }, 3000)
    },
  })

  const handleClose = () => {
    onOpenChange(false)
    setConfirmText("")
    setConfirmed(false)
    setSuccess(false)
    setDeletedCounts({})
  }

  const handleEntityToggle = (entityId: string, checked: boolean) => {
    const newChecked = new Set(checkedEntities)

    if (checked) {
      newChecked.add(entityId)
    } else {
      newChecked.delete(entityId)
    }

    setCheckedEntities(newChecked)
  }

  const handleBindingToggle = (providerCode: string, bindingType: string, checked: boolean) => {
    const newBindings = new Map(checkedBindings)

    if (!newBindings.has(providerCode)) {
      newBindings.set(providerCode, new Set())
    }

    const providerBindings = newBindings.get(providerCode)!

    if (checked) {
      providerBindings.add(bindingType)
    } else {
      providerBindings.delete(bindingType)
    }

    // Clean up empty sets
    if (providerBindings.size === 0) {
      newBindings.delete(providerCode)
    }

    setCheckedBindings(newBindings)
  }

  const handleReset = () => {
    const hasEntities = checkedEntities.size > 0
    const hasBindings = checkedBindings.size > 0

    if (confirmText === "RESET" && confirmed && (hasEntities || hasBindings)) {
      resetMutation.mutate({
        entities: Array.from(checkedEntities),
        bindings: checkedBindings
      })
    }
  }

  // Calculate total records to be deleted (entities only, bindings counted separately)
  const totalEntityRecords = Array.from(checkedEntities).reduce((sum, id) => {
    return sum + (entityCounts?.[id] ?? 0)
  }, 0)

  // Calculate total binding records
  const totalBindingRecords = Array.from(checkedBindings.entries()).reduce((sum, [providerCode, bindingTypes]) => {
    const providerCounts = bindingCounts?.[providerCode]
    if (!providerCounts) return sum
    return sum + Array.from(bindingTypes).reduce((s, bt) => s + (providerCounts[bt as keyof typeof providerCounts] ?? 0), 0)
  }, 0)

  const totalRecords = totalEntityRecords + totalBindingRecords

  const hasEntities = checkedEntities.size > 0
  const hasBindings = checkedBindings.size > 0
  const canSubmit = confirmText === "RESET" && confirmed && !resetMutation.isPending && (hasEntities || hasBindings)

  // Get providers with bindings (sorted alphabetically, exclude _league_seasons)
  const providersWithBindings = bindingCounts
    ? Object.keys(bindingCounts)
        .filter(k => k !== '_league_seasons')
        .sort()
    : []

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center">
            <AlertTriangle className="w-5 h-5 mr-2 text-orange-600" />
            Selektivní reset dat
          </DialogTitle>
          <DialogDescription>
            Vyberte, které entity chcete smazat. Čísla v závorkách ukazují počet záznamů.
          </DialogDescription>
        </DialogHeader>

        {success ? (
          <div className="py-6 text-center">
            <CheckCircle2 className="w-12 h-12 text-green-600 mx-auto mb-3" />
            <p className="text-lg font-semibold text-green-900">Reset dokončen!</p>
            <div className="mt-4 text-sm text-gray-600">
              <p className="font-medium mb-2">Smazané záznamy:</p>
              <div className="grid grid-cols-2 gap-2 max-w-md mx-auto">
                {Object.entries(deletedCounts).map(([entity, count]) => (
                  <div key={entity} className="flex justify-between bg-gray-50 px-3 py-1 rounded">
                    <span>{entity}:</span>
                    <span className="font-mono">{count}</span>
                  </div>
                ))}
              </div>
            </div>
            <p className="text-sm text-gray-500 mt-4">Dialog se automaticky zavře...</p>
          </div>
        ) : (
          <>
            <div className="space-y-6 py-4">
              {/* Static Entity Categories */}
              {STATIC_ENTITY_CATEGORIES.map((category) => (
                <div key={category.name}>
                  <h3 className="font-semibold text-sm text-gray-900 mb-1">{category.name}</h3>
                  {category.description && (
                    <p className="text-xs text-gray-500 mb-2">{category.description}</p>
                  )}
                  <div className="space-y-2 pl-2">
                    {category.entities.map((entity) => {
                      const count = entityCounts?.[entity.id]
                      const countDisplay = countsLoading ? '...' : (count ?? 0)

                      return (
                        <div key={entity.id} className="flex items-center space-x-3">
                          <input
                            type="checkbox"
                            id={entity.id}
                            checked={checkedEntities.has(entity.id)}
                            onChange={(e) => handleEntityToggle(entity.id, e.target.checked)}
                            disabled={resetMutation.isPending}
                            className="h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950"
                          />
                          <label
                            htmlFor={entity.id}
                            className="text-sm font-medium text-gray-700 cursor-pointer flex-1"
                          >
                            {entity.label}
                            <span className="ml-2 text-gray-400 font-normal">
                              ({countDisplay})
                            </span>
                          </label>
                        </div>
                      )
                    })}
                  </div>
                </div>
              ))}

              {/* Bindings per Provider */}
              {providersWithBindings.length > 0 && (
                <div>
                  <h3 className="font-semibold text-sm text-gray-900 mb-1">Konfigurace - vazby</h3>
                  <p className="text-xs text-gray-500 mb-3">Vazby mezi entitami a providery (per provider)</p>

                  <div className="space-y-4">
                    {providersWithBindings.map((providerCode) => {
                      const counts = bindingCounts?.[providerCode]
                      if (!counts) return null

                      const leagueProviderCount = counts.league_providers ?? 0
                      const countryProviderCount = counts.country_providers ?? 0

                      // Skip if no bindings
                      if (leagueProviderCount === 0 && countryProviderCount === 0) return null

                      return (
                        <div key={providerCode} className="pl-2 border-l-2 border-gray-200">
                          <h4 className="text-sm font-medium text-gray-800 mb-2 capitalize">
                            {providerCode}
                          </h4>
                          <div className="space-y-2 pl-2">
                            {leagueProviderCount > 0 && (
                              <div className="flex items-center space-x-3">
                                <input
                                  type="checkbox"
                                  id={`${providerCode}-league_providers`}
                                  checked={checkedBindings.get(providerCode)?.has('league_providers') ?? false}
                                  onChange={(e) => handleBindingToggle(providerCode, 'league_providers', e.target.checked)}
                                  disabled={resetMutation.isPending}
                                  className="h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950"
                                />
                                <label
                                  htmlFor={`${providerCode}-league_providers`}
                                  className="text-sm font-medium text-gray-700 cursor-pointer flex-1"
                                >
                                  Vazby lig
                                  <span className="ml-2 text-gray-400 font-normal">
                                    ({bindingsLoading ? '...' : leagueProviderCount})
                                  </span>
                                </label>
                              </div>
                            )}
                            {countryProviderCount > 0 && (
                              <div className="flex items-center space-x-3">
                                <input
                                  type="checkbox"
                                  id={`${providerCode}-country_providers`}
                                  checked={checkedBindings.get(providerCode)?.has('country_providers') ?? false}
                                  onChange={(e) => handleBindingToggle(providerCode, 'country_providers', e.target.checked)}
                                  disabled={resetMutation.isPending}
                                  className="h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950"
                                />
                                <label
                                  htmlFor={`${providerCode}-country_providers`}
                                  className="text-sm font-medium text-gray-700 cursor-pointer flex-1"
                                >
                                  Vazby zemí
                                  <span className="ml-2 text-gray-400 font-normal">
                                    ({bindingsLoading ? '...' : countryProviderCount})
                                  </span>
                                </label>
                              </div>
                            )}
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )}

              {/* League Seasons (not per provider) */}
              {bindingCounts?.['_league_seasons']?.league_seasons && bindingCounts['_league_seasons'].league_seasons > 0 && (
                <div>
                  <h3 className="font-semibold text-sm text-gray-900 mb-1">Vazby lig-sezón</h3>
                  <p className="text-xs text-gray-500 mb-2">Vazby mezi ligami a sezónami (globální)</p>
                  <div className="space-y-2 pl-2">
                    <div className="flex items-center space-x-3">
                      <input
                        type="checkbox"
                        id="league_seasons"
                        checked={checkedEntities.has('league_seasons')}
                        onChange={(e) => handleEntityToggle('league_seasons', e.target.checked)}
                        disabled={resetMutation.isPending}
                        className="h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950"
                      />
                      <label
                        htmlFor="league_seasons"
                        className="text-sm font-medium text-gray-700 cursor-pointer flex-1"
                      >
                        Vazby lig-sezón
                        <span className="ml-2 text-gray-400 font-normal">
                          ({bindingsLoading ? '...' : bindingCounts['_league_seasons'].league_seasons})
                        </span>
                      </label>
                    </div>
                  </div>
                </div>
              )}

              {/* Selected Summary */}
              {(hasEntities || hasBindings) && (
                <div className="bg-orange-50 border border-orange-200 rounded-lg p-3">
                  <p className="text-sm font-semibold text-orange-900 mb-2">
                    Bude smazáno: ~{totalRecords} záznamů
                  </p>
                  {hasEntities && (
                    <p className="text-xs text-orange-800 font-mono mb-1">
                      Entity: {Array.from(checkedEntities).join(", ")}
                    </p>
                  )}
                  {hasBindings && (
                    <p className="text-xs text-orange-800 font-mono">
                      Vazby: {Array.from(checkedBindings.entries())
                        .map(([pc, bts]) => `${pc}:[${Array.from(bts).join(",")}]`)
                        .join(", ")}
                    </p>
                  )}
                </div>
              )}

              {/* Warning */}
              <div className="bg-red-50 border border-red-200 rounded-lg p-3">
                <p className="text-sm font-semibold text-red-900">
                  Tato akce je nevratná!
                </p>
                <p className="text-sm mt-1 text-red-800">
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
                  disabled={resetMutation.isPending}
                  className="h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950 mt-0.5"
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
                  `Smazat vybrané`
                )}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
