"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { configApi } from "@/lib/api/client";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import { SportProviderDialog } from "@/components/SportProviderDialog";

export default function SportsPage() {
  const queryClient = useQueryClient();
  const [providerDialogOpen, setProviderDialogOpen] = useState(false);
  const [selectedSport, setSelectedSport] = useState<{ id: string; name: string } | null>(null);
  const [editingMapping, setEditingMapping] = useState<any>(null);

  const deleteMappingMutation = useMutation({
    mutationFn: async ({ sportId, providerId }: { sportId: string; providerId: string }) => {
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/config/sports/${sportId}/providers/${providerId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error("Failed to delete mapping");
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sports"] });
    },
  });

  const { data: sports, isLoading, error } = useQuery({
    queryKey: ["sports"],
    queryFn: () => configApi.getSports(),
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      configApi.updateSport(id, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["sports"] });
    },
  });

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Načítání...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-destructive">Chyba</CardTitle>
          </CardHeader>
          <CardContent>
            <p>Nelze načíst data: {(error as Error).message}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Správa Sportů</h1>
            <p className="text-gray-600">
              Aktivujte nebo deaktivujte sporty pro použití v systému
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {sports?.map((sport) => (
            <Card key={sport.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      {sport.name}
                      {sport.isActive && (
                        <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                          Aktivní
                        </span>
                      )}
                      {!sport.isActive && (
                        <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
                          Neaktivní
                        </span>
                      )}
                    </CardTitle>
                    <CardDescription>Kód: {sport.code}</CardDescription>
                  </div>
                  <Button
                    variant={sport.isActive ? "default" : "outline"}
                    size="sm"
                    onClick={() =>
                      toggleActiveMutation.mutate({
                        id: sport.id,
                        isActive: !sport.isActive,
                      })
                    }
                    disabled={toggleActiveMutation.isPending}
                    title={sport.isActive ? "Deaktivovat" : "Aktivovat"}
                  >
                    {sport.isActive ? "✓ Aktivní" : "○ Neaktivní"}
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="text-sm font-semibold">Provider Mappings:</div>
                  {sport.sportProviders && sport.sportProviders.length > 0 ? (
                    <div className="space-y-1">
                      {sport.sportProviders.map((sp: any) => (
                        <div
                          key={sp.id}
                          className="flex items-center justify-between text-sm p-2 bg-gray-50 rounded"
                        >
                          <div>
                            <span className="font-medium">{sp.provider?.name || "Unknown"}</span>
                            <span className="text-gray-500 ml-2">({sp.providerCode})</span>
                          </div>
                          <div className="flex gap-2 items-center">
                            {sp.isActive ? (
                              <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">
                                Active
                              </span>
                            ) : (
                              <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">
                                Inactive
                              </span>
                            )}
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setSelectedSport({ id: sport.id, name: sport.name });
                                setEditingMapping(sp);
                                setProviderDialogOpen(true);
                              }}
                            >
                              Upravit
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              onClick={() => {
                                if (confirm(`Smazat mapping pro ${sp.provider?.name}?`)) {
                                  deleteMappingMutation.mutate({
                                    sportId: sport.id,
                                    providerId: sp.providerId,
                                  });
                                }
                              }}
                              disabled={deleteMappingMutation.isPending}
                            >
                              Smazat
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-gray-500">Žádné provider mappings</p>
                  )}
                  <Button
                    size="sm"
                    variant="outline"
                    className="w-full mt-2"
                    onClick={() => {
                      setSelectedSport({ id: sport.id, name: sport.name });
                      setEditingMapping(null); // Clear edit state for create mode
                      setProviderDialogOpen(true);
                    }}
                  >
                    + Přidat Provider
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* SportProvider Dialog */}
        {selectedSport && (
          <SportProviderDialog
            open={providerDialogOpen}
            onOpenChange={(open) => {
              setProviderDialogOpen(open);
              if (!open) setEditingMapping(null); // Clear edit state on close
            }}
            sportId={selectedSport.id}
            sportName={selectedSport.name}
            editingMapping={editingMapping}
          />
        )}
      </div>
    </div>
  );
}
