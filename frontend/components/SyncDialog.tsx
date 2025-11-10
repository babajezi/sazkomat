"use client";

import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Checkbox } from "@/components/ui/checkbox";
import {
  CheckCircle2,
  XCircle,
  AlertCircle,
  Loader2,
  Database,
  MapPin,
  Trophy,
  Calendar
} from "lucide-react";
import type { SyncResponse } from "@/lib/api/types";
import { ProviderType, SyncType } from "@/lib/api/types";

interface DataProvider {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  type: ProviderType;
}

interface SyncDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  provider: DataProvider | null;
}

interface SyncStep {
  type: SyncType;
  label: string;
  description: string;
  icon: React.ReactNode;
  completed: boolean;
  running: boolean;
  result?: SyncResponse;
}

export function SyncDialog({ open, onOpenChange, provider }: SyncDialogProps) {
  const queryClient = useQueryClient();
  const [currentStep, setCurrentStep] = useState(0);
  const [activateCountries, setActivateCountries] = useState(false);
  const [enabledSteps, setEnabledSteps] = useState({
    Countries: true,
    Leagues: true,
    Seasons: false,
  });
  const [syncSteps, setSyncSteps] = useState<SyncStep[]>([
    {
      type: SyncType.Countries,
      label: "Synchronizace zemí",
      description: "Načítání seznamu zemí z poskytovatele",
      icon: <MapPin className="w-5 h-5" />,
      completed: false,
      running: false,
    },
    {
      type: SyncType.Leagues,
      label: "Synchronizace lig",
      description: "Načítání lig pro aktivní země",
      icon: <Trophy className="w-5 h-5" />,
      completed: false,
      running: false,
    },
    {
      type: SyncType.Seasons,
      label: "Synchronizace sezón",
      description: "Načítání dostupných sezón pro ligy",
      icon: <Calendar className="w-5 h-5" />,
      completed: false,
      running: false,
    },
  ]);

  // Auto-check "Activate Countries" for Betting Providers
  useEffect(() => {
    if (provider?.type === ProviderType.BettingProvider) {
      setActivateCountries(true);
    }
  }, [provider]);

  const resetWorkflowMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/sync/workflow/reset`, {
        method: "POST",
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Failed to reset workflow");
      }

      return response.json();
    },
    onSuccess: () => {
      // Reset local state
      setSyncSteps(prev => prev.map(step => ({
        ...step,
        completed: false,
        running: false,
        result: undefined
      })));
      setCurrentStep(0);
      // Refresh workflow state
      queryClient.invalidateQueries({ queryKey: ["workflowState"] });
    },
  });

  const syncMutation = useMutation({
    mutationFn: async ({ providerId, type }: { providerId: string; type: SyncType }) => {
      let endpoint = "";
      switch (type) {
        case SyncType.Countries:
          endpoint = "/api/sync/countries";
          break;
        case SyncType.Leagues:
          endpoint = "/api/sync/leagues";
          break;
        case SyncType.Seasons:
          // This will need to be called per league, so we skip it for now
          return { success: true, message: "Seasons sync requires manual trigger per league" } as SyncResponse;
      }

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          providerId,
          activateCountries: type === SyncType.Countries ? activateCountries : undefined
        }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Synchronization failed");
      }

      return response.json();
    },
    onSuccess: async (data, variables) => {
      // Update step status
      setSyncSteps(prev => prev.map((step, idx) => {
        if (step.type === variables.type) {
          return { ...step, completed: true, running: false, result: data };
        }
        return step;
      }));

      // Check if data has statistics (may be missing in error responses)
      if (!data?.statistics) {
        console.error(`Invalid response format for ${variables.type}, stopping automatic progression`);
        return;
      }

      // Check if there were errors - if yes, stop automatic progression
      if (data.statistics.errors > 0) {
        console.warn(`Sync completed with errors for ${variables.type}, stopping automatic progression`);
        return;
      }

      // Auto-confirm after successful sync (regardless of counts)
      // This allows workflow to progress to next step
      let confirmSuccess = true;
      if (variables.type === SyncType.Countries) {
        try {
          const confirmResponse = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/sync/workflow/confirm-countries`, {
            method: "POST",
          });
          if (!confirmResponse.ok) {
            console.error("Failed to confirm countries:", await confirmResponse.text());
            confirmSuccess = false;
          } else {
            console.log("Countries auto-confirmed after sync");
          }
        } catch (error) {
          console.error("Failed to auto-confirm countries:", error);
          confirmSuccess = false;
        }
      } else if (variables.type === SyncType.Leagues) {
        try {
          const confirmResponse = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/sync/workflow/confirm-leagues`, {
            method: "POST",
          });
          if (!confirmResponse.ok) {
            console.error("Failed to confirm leagues:", await confirmResponse.text());
            confirmSuccess = false;
          } else {
            console.log("Leagues auto-confirmed after sync");
          }
        } catch (error) {
          console.error("Failed to auto-confirm leagues:", error);
          confirmSuccess = false;
        }
      }

      // Only proceed to next step if confirmation was successful
      if (!confirmSuccess) {
        console.error("Stopping automatic progression due to confirmation failure");
        return;
      }

      // Move to next enabled step if available
      if (currentStep < syncSteps.length - 1) {
        // Find next enabled step
        let nextStepIndex = currentStep + 1;
        while (nextStepIndex < syncSteps.length) {
          const nextStep = syncSteps[nextStepIndex];
          if (enabledSteps[nextStep.type as keyof typeof enabledSteps]) {
            setCurrentStep(nextStepIndex);

            // Auto-start next enabled step (wait a bit longer to ensure confirm is processed)
            if (provider) {
              setTimeout(() => {
                setSyncSteps(prev => prev.map((step, idx) =>
                  idx === nextStepIndex ? { ...step, running: true } : step
                ));
                syncMutation.mutate({ providerId: provider.id, type: nextStep.type });
              }, 1000); // Increased from 500ms to 1000ms
            }
            break;
          }
          nextStepIndex++;
        }
      }

      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: ["countries"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      queryClient.invalidateQueries({ queryKey: ["seasons"] });
    },
    onError: (error, variables) => {
      // Mark step as failed
      setSyncSteps(prev => prev.map(step => {
        if (step.type === variables.type) {
          return { ...step, running: false };
        }
        return step;
      }));
    },
  });

  const handleStartSync = () => {
    if (!provider) return;

    // Reset state
    setSyncSteps(prev => prev.map(step => ({
      ...step,
      completed: false,
      running: false,
      result: undefined
    })));

    // Find first enabled step
    const firstEnabledIndex = syncSteps.findIndex(step =>
      enabledSteps[step.type as keyof typeof enabledSteps]
    );

    if (firstEnabledIndex === -1) return;

    setCurrentStep(firstEnabledIndex);

    // Start first enabled step
    setSyncSteps(prev => prev.map((step, idx) =>
      idx === firstEnabledIndex ? { ...step, running: true } : step
    ));
    syncMutation.mutate({ providerId: provider.id, type: syncSteps[firstEnabledIndex].type });
  };

  const handleClose = () => {
    // Reset state when closing
    setSyncSteps(prev => prev.map(step => ({
      ...step,
      completed: false,
      running: false,
      result: undefined
    })));
    setCurrentStep(0);
    setActivateCountries(false); // Reset checkbox
    syncMutation.reset();
    onOpenChange(false);
  };

  const allCompleted = syncSteps
    .filter(s => enabledSteps[s.type as keyof typeof enabledSteps])
    .every(s => s.completed);
  const hasError = syncMutation.isError;
  const hasAnyEnabled = Object.values(enabledSteps).some(v => v);

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Database className="w-5 h-5" />
            Synchronizace dat z {provider?.name}
          </DialogTitle>
          <DialogDescription>
            Vyberte, které kroky chcete synchronizovat. Při chybě se synchronizace automaticky zastaví.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {syncSteps.map((step, index) => (
            <div
              key={step.type}
              className={`border rounded-lg p-4 transition-all ${
                step.running
                  ? "border-blue-500 bg-blue-50"
                  : step.completed
                  ? "border-green-500 bg-green-50"
                  : "border-gray-200"
              }`}
            >
              <div className="flex items-start gap-3">
                <div className={`mt-1 ${
                  step.running ? "text-blue-600" :
                  step.completed ? "text-green-600" :
                  "text-gray-400"
                }`}>
                  {step.icon}
                </div>

                <div className="flex-1">
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <Checkbox
                        id={`step-${index}`}
                        checked={enabledSteps[step.type as keyof typeof enabledSteps]}
                        onChange={(e) =>
                          setEnabledSteps(prev => ({
                            ...prev,
                            [step.type]: e.target.checked
                          }))
                        }
                        disabled={syncMutation.isPending || step.running || step.completed}
                      />
                      <h4 className="font-semibold">{step.label}</h4>
                    </div>
                    {step.running && (
                      <Loader2 className="w-4 h-4 animate-spin text-blue-600" />
                    )}
                    {step.completed && (
                      <CheckCircle2 className="w-5 h-5 text-green-600" />
                    )}
                  </div>

                  <p className="text-sm text-gray-600 mb-2 ml-6">{step.description}</p>

                  {/* Activate Countries checkbox - only for Countries step */}
                  {step.type === SyncType.Countries && !step.completed && (
                    <div className="ml-6 mt-2 flex items-start gap-2 p-2 bg-blue-50 rounded">
                      <input
                        type="checkbox"
                        id="activate-countries-checkbox"
                        checked={activateCountries}
                        onChange={(e) => setActivateCountries(e.target.checked)}
                        className="h-4 w-4 mt-0.5"
                        disabled={syncMutation.isPending}
                      />
                      <label htmlFor="activate-countries-checkbox" className="text-sm cursor-pointer flex-1">
                        <span className="font-medium text-blue-900">Aktivovat země</span>
                        <span className="block text-blue-700 text-xs mt-0.5">
                          Automaticky aktivuje neaktivní země nalezené během synchronizace
                        </span>
                      </label>
                    </div>
                  )}

                  {step.result && step.result.statistics && (
                    <div className="mt-3 space-y-2">
                      <div className="flex gap-2 text-xs">
                        <Badge variant="outline" className="bg-green-50">
                          ✓ Vytvořeno: {step.result.statistics.created}
                        </Badge>
                        <Badge variant="outline" className="bg-blue-50">
                          ↻ Aktualizováno: {step.result.statistics.updated}
                        </Badge>
                        <Badge variant="outline" className="bg-gray-50">
                          ⊘ Přeskočeno: {step.result.statistics.skipped}
                        </Badge>
                        {step.result.statistics.errors > 0 && (
                          <Badge variant="outline" className="bg-red-50">
                            ✗ Chyby: {step.result.statistics.errors}
                          </Badge>
                        )}
                      </div>

                      {step.result.statistics.errorMessages?.length > 0 && (
                        <Alert variant="destructive" className="mt-2">
                          <AlertCircle className="h-4 w-4" />
                          <AlertDescription className="text-xs">
                            {step.result.statistics.errorMessages.slice(0, 3).map((msg, i) => (
                              <div key={i}>{msg}</div>
                            ))}
                            {step.result.statistics.errorMessages.length > 3 && (
                              <div className="text-gray-500">
                                ... a {step.result.statistics.errorMessages.length - 3} dalších
                              </div>
                            )}
                          </AlertDescription>
                        </Alert>
                      )}
                    </div>
                  )}

                  {step.result && !step.result.statistics && (
                    <div className="mt-3">
                      <Alert className="bg-blue-50 border-blue-200">
                        <AlertCircle className="h-4 w-4 text-blue-600" />
                        <AlertDescription className="text-sm text-blue-800">
                          {step.result.message || "Synchronizace dokončena s neočekávaným formátem odpovědi"}
                        </AlertDescription>
                      </Alert>
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))}

          {hasError && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertDescription>
                Chyba při synchronizaci: {(syncMutation.error as Error)?.message}
              </AlertDescription>
            </Alert>
          )}

          {allCompleted && (
            <Alert className="bg-green-50 border-green-200">
              <CheckCircle2 className="h-4 w-4 text-green-600" />
              <AlertDescription className="text-green-800">
                Synchronizace zemí a lig byla úspěšně dokončena!
                Sezóny lze synchronizovat individuálně pro každou ligu.
              </AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <div className="flex justify-between w-full">
            <Button
              variant="outline"
              onClick={() => resetWorkflowMutation.mutate()}
              disabled={resetWorkflowMutation.isPending || syncMutation.isPending}
            >
              {resetWorkflowMutation.isPending ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Resetuji...
                </>
              ) : (
                "Reset workflow"
              )}
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={handleClose}>
                Zavřít
              </Button>
              <Button
                onClick={handleStartSync}
                disabled={syncMutation.isPending || allCompleted || !hasAnyEnabled}
              >
                {syncMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Probíhá synchronizace...
                  </>
                ) : allCompleted ? (
                  "Synchronizace dokončena"
                ) : !hasAnyEnabled ? (
                  "Vyberte alespoň jeden krok"
                ) : (
                  "Spustit synchronizaci"
                )}
              </Button>
            </div>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
