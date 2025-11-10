"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";
import { useState } from "react";
import { RefreshCw, Database } from "lucide-react";
import { SyncDialog } from "@/components/SyncDialog";
import { PaginationControls } from "@/components/PaginationControls";
import { ProviderType } from "@/lib/api/types";

interface DataProvider {
  id: string;
  name: string;
  code: string;
  baseUrl: string;
  isActive: boolean;
  priority: number;
  type: number;
  notes: string | null;
}

export default function ProvidersPage() {
  const queryClient = useQueryClient();
  const [syncDialogOpen, setSyncDialogOpen] = useState(false);
  const [selectedProvider, setSelectedProvider] = useState<DataProvider | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);

  const { data: providers, isLoading, error } = useQuery<DataProvider[]>({
    queryKey: ["providers"],
    queryFn: async () => {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/config/providers`);
      if (!response.ok) throw new Error("Failed to fetch providers");
      return response.json();
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/config/providers/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ isActive }),
      });
      if (!response.ok) throw new Error("Failed to update provider");
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["providers"] });
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

  // Pagination - client-side slice
  const paginatedProviders = providers?.slice(
    page * pageSize,
    (page + 1) * pageSize
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Datové Provideři</h1>
            <p className="text-gray-600 mt-2">
              Správa datových providerů a jejich konfigurace
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět na hlavní stránku</Button>
          </Link>
        </div>

        {/* Pagination Controls - Top */}
        {providers && providers.length > 0 && (
          <PaginationControls
            page={page}
            pageSize={pageSize}
            totalCount={providers.length}
            displayedCount={providers.length}
            itemName="providerů"
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(0);
            }}
            className="mb-6"
          />
        )}

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {paginatedProviders?.map((provider) => (
            <Card key={provider.id}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      {provider.name}
                      {provider.isActive ? (
                        <Badge variant="default" className="bg-green-500">Aktivní</Badge>
                      ) : (
                        <Badge variant="secondary">Neaktivní</Badge>
                      )}
                    </CardTitle>
                    <CardDescription className="mt-1">
                      <code className="text-xs bg-gray-100 px-2 py-1 rounded">
                        {provider.code}
                      </code>
                    </CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <div className="text-sm font-medium text-gray-700 mb-1">URL:</div>
                  <a
                    href={provider.baseUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm text-blue-600 hover:underline break-all"
                  >
                    {provider.baseUrl}
                  </a>
                </div>

                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <div className="font-medium text-gray-700">Priorita:</div>
                    <div>{provider.priority}</div>
                  </div>
                  <div>
                    <div className="font-medium text-gray-700">Typ:</div>
                    <div>{provider.type === ProviderType.Scraper ? "Scraper" : provider.type === ProviderType.API ? "API" : provider.type === ProviderType.Manual ? "Manual" : "Betting Provider"}</div>
                  </div>
                </div>

                {provider.notes && (
                  <div>
                    <div className="text-sm font-medium text-gray-700 mb-1">Poznámky:</div>
                    <div className="text-sm text-gray-600">{provider.notes}</div>
                  </div>
                )}

                <div className="pt-4 border-t space-y-2">
                  <Button
                    onClick={() => {
                      setSelectedProvider(provider);
                      setSyncDialogOpen(true);
                    }}
                    disabled={!provider.isActive}
                    variant="default"
                    className="w-full"
                  >
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Synchronizovat
                  </Button>
                  <Button
                    onClick={() =>
                      toggleActiveMutation.mutate({
                        id: provider.id,
                        isActive: !provider.isActive,
                      })
                    }
                    disabled={toggleActiveMutation.isPending}
                    variant={provider.isActive ? "outline" : "default"}
                    className="w-full"
                  >
                    {provider.isActive ? "Deaktivovat" : "Aktivovat"}
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {providers?.length === 0 && (
          <Card>
            <CardContent className="py-12 text-center text-gray-500">
              Žádní provideři nejsou nakonfigurováni
            </CardContent>
          </Card>
        )}

        <SyncDialog
          open={syncDialogOpen}
          onOpenChange={setSyncDialogOpen}
          provider={selectedProvider}
        />
      </div>
    </div>
  );
}
