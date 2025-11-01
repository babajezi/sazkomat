"use client";

import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import Link from "next/link";
import { Download, Eye, AlertCircle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

interface ExportOptions {
  includeSports: boolean;
  includeCountries: boolean;
  includeProviders: boolean;
  includeSeasons: boolean;
  includeLeagues: boolean;
  includeSportProviders: boolean;
  includeCountryProviders: boolean;
  includeLeagueProviders: boolean;
  includeLeagueSeasons: boolean;
  onlyActive: boolean;
}

interface ExportPreview {
  totalEntities: number;
  includedTypes: string[];
}

export default function ExportConfigPage() {
  const [options, setOptions] = useState<ExportOptions>({
    includeSports: false,
    includeCountries: false,
    includeProviders: false,
    includeSeasons: false,
    includeLeagues: false,
    includeSportProviders: false,
    includeCountryProviders: false,
    includeLeagueProviders: false,
    includeLeagueSeasons: false,
    onlyActive: true,
  });

  const [showPreview, setShowPreview] = useState(false);

  // Preview query
  const { data: preview, isLoading: previewLoading } = useQuery({
    queryKey: ["export-preview", options],
    queryFn: async () => {
      const params = new URLSearchParams({
        sports: options.includeSports.toString(),
        countries: options.includeCountries.toString(),
        providers: options.includeProviders.toString(),
        seasons: options.includeSeasons.toString(),
        leagues: options.includeLeagues.toString(),
        sportProviders: options.includeSportProviders.toString(),
        countryProviders: options.includeCountryProviders.toString(),
        leagueProviders: options.includeLeagueProviders.toString(),
        leagueSeasons: options.includeLeagueSeasons.toString(),
        onlyActive: options.onlyActive.toString(),
      });

      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/config/export/preview?${params}`
      );

      if (!response.ok) {
        throw new Error("Failed to get preview");
      }

      return response.json() as Promise<ExportPreview>;
    },
    enabled: showPreview,
  });

  // Export mutation
  const exportMutation = useMutation({
    mutationFn: async () => {
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/config/export`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(options),
        }
      );

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Export failed");
      }

      // Get filename from Content-Disposition header or generate one
      const contentDisposition = response.headers.get("Content-Disposition");
      let filename = "sazkomat-config.json";
      if (contentDisposition) {
        const match = contentDisposition.match(/filename="?(.+)"?/i);
        if (match) filename = match[1];
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    },
  });

  const handleCheckboxChange = (key: keyof ExportOptions, checked: boolean) => {
    const newOptions = { ...options, [key]: checked };

    // Auto-check dependencies
    if (key === "includeLeagues" && checked) {
      newOptions.includeSports = true;
      newOptions.includeCountries = true;
    }

    if (key === "includeSportProviders" && checked) {
      newOptions.includeSports = true;
      newOptions.includeProviders = true;
    }

    if (key === "includeCountryProviders" && checked) {
      newOptions.includeCountries = true;
      newOptions.includeProviders = true;
    }

    if (key === "includeLeagueProviders" && checked) {
      newOptions.includeLeagues = true;
      newOptions.includeProviders = true;
      newOptions.includeSports = true; // Leagues need Sports
      newOptions.includeCountries = true; // Leagues need Countries
    }

    if (key === "includeLeagueSeasons" && checked) {
      newOptions.includeLeagues = true;
      newOptions.includeSeasons = true;
      newOptions.includeSports = true; // Leagues need Sports
      newOptions.includeCountries = true; // Leagues need Countries
    }

    // Auto-uncheck dependents
    if (key === "includeSports" && !checked) {
      newOptions.includeLeagues = false;
      newOptions.includeSportProviders = false;
      newOptions.includeLeagueProviders = false;
      newOptions.includeLeagueSeasons = false;
    }

    if (key === "includeCountries" && !checked) {
      newOptions.includeLeagues = false;
      newOptions.includeCountryProviders = false;
      newOptions.includeLeagueProviders = false;
      newOptions.includeLeagueSeasons = false;
    }

    if (key === "includeProviders" && !checked) {
      newOptions.includeSportProviders = false;
      newOptions.includeCountryProviders = false;
      newOptions.includeLeagueProviders = false;
    }

    if (key === "includeLeagues" && !checked) {
      newOptions.includeLeagueProviders = false;
      newOptions.includeLeagueSeasons = false;
    }

    if (key === "includeSeasons" && !checked) {
      newOptions.includeLeagueSeasons = false;
    }

    setOptions(newOptions);
  };

  const hasSelectedEntities =
    options.includeSports ||
    options.includeCountries ||
    options.includeProviders ||
    options.includeSeasons ||
    options.includeLeagues ||
    options.includeSportProviders ||
    options.includeCountryProviders ||
    options.includeLeagueProviders ||
    options.includeLeagueSeasons;

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Export Konfigurace</h1>
            <p className="text-gray-600">
              Exportujte konfigurační data do JSON souboru
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět</Button>
          </Link>
        </div>

        {/* Entity Selection */}
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Vyberte entity k exportu</CardTitle>
            <CardDescription>
              Závislé entity budou automaticky vybrány
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Core Entities */}
            <div>
              <h3 className="font-semibold mb-3">Základní entity</h3>
              <div className="space-y-3 ml-4">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="sports"
                    checked={options.includeSports}
                    onChange={(e) =>
                      handleCheckboxChange("includeSports", e.target.checked)
                    }
                  />
                  <Label htmlFor="sports" className="cursor-pointer">
                    Sporty
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="countries"
                    checked={options.includeCountries}
                    onChange={(e) =>
                      handleCheckboxChange("includeCountries", e.target.checked)
                    }
                  />
                  <Label htmlFor="countries" className="cursor-pointer">
                    Země
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="providers"
                    checked={options.includeProviders}
                    onChange={(e) =>
                      handleCheckboxChange("includeProviders", e.target.checked)
                    }
                  />
                  <Label htmlFor="providers" className="cursor-pointer">
                    Poskytovatelé dat
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="seasons"
                    checked={options.includeSeasons}
                    onChange={(e) =>
                      handleCheckboxChange("includeSeasons", e.target.checked)
                    }
                  />
                  <Label htmlFor="seasons" className="cursor-pointer">
                    Sezóny
                  </Label>
                </div>
              </div>
            </div>

            {/* Dependent Entities */}
            <div>
              <h3 className="font-semibold mb-3">Ligy a mapování</h3>
              <div className="space-y-3 ml-4">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="leagues"
                    checked={options.includeLeagues}
                    disabled={
                      !options.includeSports || !options.includeCountries
                    }
                    onChange={(e) =>
                      handleCheckboxChange("includeLeagues", e.target.checked)
                    }
                  />
                  <Label
                    htmlFor="leagues"
                    className={`cursor-pointer ${
                      !options.includeSports || !options.includeCountries
                        ? "text-gray-400"
                        : ""
                    }`}
                  >
                    Ligy
                    {(!options.includeSports || !options.includeCountries) && (
                      <span className="text-xs ml-2 text-gray-400">
                        (vyžaduje Sporty + Země)
                      </span>
                    )}
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="sportProviders"
                    checked={options.includeSportProviders}
                    disabled={
                      !options.includeSports || !options.includeProviders
                    }
                    onChange={(e) =>
                      handleCheckboxChange("includeSportProviders", e.target.checked)
                    }
                  />
                  <Label
                    htmlFor="sportProviders"
                    className={`cursor-pointer ${
                      !options.includeSports || !options.includeProviders
                        ? "text-gray-400"
                        : ""
                    }`}
                  >
                    Sport-Provider mapování
                    {(!options.includeSports || !options.includeProviders) && (
                      <span className="text-xs ml-2 text-gray-400">
                        (vyžaduje Sporty + Providery)
                      </span>
                    )}
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="countryProviders"
                    checked={options.includeCountryProviders}
                    disabled={
                      !options.includeCountries || !options.includeProviders
                    }
                    onChange={(e) =>
                      handleCheckboxChange("includeCountryProviders", e.target.checked)
                    }
                  />
                  <Label
                    htmlFor="countryProviders"
                    className={`cursor-pointer ${
                      !options.includeCountries || !options.includeProviders
                        ? "text-gray-400"
                        : ""
                    }`}
                  >
                    Country-Provider mapování
                    {(!options.includeCountries ||
                      !options.includeProviders) && (
                      <span className="text-xs ml-2 text-gray-400">
                        (vyžaduje Země + Providery)
                      </span>
                    )}
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="leagueProviders"
                    checked={options.includeLeagueProviders}
                    disabled={
                      !options.includeLeagues || !options.includeProviders
                    }
                    onChange={(e) =>
                      handleCheckboxChange("includeLeagueProviders", e.target.checked)
                    }
                  />
                  <Label
                    htmlFor="leagueProviders"
                    className={`cursor-pointer ${
                      !options.includeLeagues || !options.includeProviders
                        ? "text-gray-400"
                        : ""
                    }`}
                  >
                    League-Provider mapování
                    {(!options.includeLeagues || !options.includeProviders) && (
                      <span className="text-xs ml-2 text-gray-400">
                        (vyžaduje Ligy + Providery)
                      </span>
                    )}
                  </Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="leagueSeasons"
                    checked={options.includeLeagueSeasons}
                    disabled={
                      !options.includeLeagues || !options.includeSeasons
                    }
                    onChange={(e) =>
                      handleCheckboxChange("includeLeagueSeasons", e.target.checked)
                    }
                  />
                  <Label
                    htmlFor="leagueSeasons"
                    className={`cursor-pointer ${
                      !options.includeLeagues || !options.includeSeasons
                        ? "text-gray-400"
                        : ""
                    }`}
                  >
                    League-Season mapování
                    {(!options.includeLeagues || !options.includeSeasons) && (
                      <span className="text-xs ml-2 text-gray-400">
                        (vyžaduje Ligy + Sezóny)
                      </span>
                    )}
                  </Label>
                </div>
              </div>
            </div>

            {/* Filters */}
            <div>
              <h3 className="font-semibold mb-3">Filtry</h3>
              <div className="space-y-3 ml-4">
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="onlyActive"
                    checked={options.onlyActive}
                    onChange={(e) =>
                      handleCheckboxChange("onlyActive", e.target.checked)
                    }
                  />
                  <Label htmlFor="onlyActive" className="cursor-pointer">
                    Pouze aktivní entity
                  </Label>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Preview */}
        {showPreview && preview && (
          <Alert className="mb-6">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Preview exportu</AlertTitle>
            <AlertDescription>
              Celkem entit: <strong>{preview.totalEntities}</strong>
              <br />
              Typy: {preview.includedTypes.join(", ")}
            </AlertDescription>
          </Alert>
        )}

        {/* Error */}
        {exportMutation.isError && (
          <Alert variant="destructive" className="mb-6">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Chyba</AlertTitle>
            <AlertDescription>
              {(exportMutation.error as Error).message}
            </AlertDescription>
          </Alert>
        )}

        {/* Actions */}
        <div className="flex gap-4">
          <Button
            onClick={() => setShowPreview(true)}
            disabled={!hasSelectedEntities || previewLoading}
            variant="outline"
          >
            <Eye className="mr-2 h-4 w-4" />
            {previewLoading ? "Načítám..." : "Preview"}
          </Button>

          <Button
            onClick={() => exportMutation.mutate()}
            disabled={!hasSelectedEntities || exportMutation.isPending}
          >
            <Download className="mr-2 h-4 w-4" />
            {exportMutation.isPending ? "Exportuji..." : "Stáhnout JSON"}
          </Button>
        </div>
      </div>
    </div>
  );
}
