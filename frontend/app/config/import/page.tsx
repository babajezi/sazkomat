"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import Link from "next/link";
import {
  Upload,
  FileJson,
  AlertCircle,
  CheckCircle,
  X,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

interface ImportResult {
  success: boolean;
  errorMessage?: string;
  totalCreated: number;
  totalUpdated: number;
  totalSkipped: number;
  sports: EntityResult;
  countries: EntityResult;
  providers: EntityResult;
  seasons: EntityResult;
  leagues: EntityResult;
}

interface EntityResult {
  created: number;
  updated: number;
  skipped: number;
}

export default function ImportConfigPage() {
  const [file, setFile] = useState<File | null>(null);
  const [fileContent, setFileContent] = useState<string | null>(null);
  const [importMode, setImportMode] = useState<"preserveIds" | "smartMatch">(
    "smartMatch"
  );
  const [conflictResolution, setConflictResolution] = useState<
    "skip" | "update" | "fail"
  >("update");
  const [dragActive, setDragActive] = useState(false);

  // Import mutation
  const importMutation = useMutation({
    mutationFn: async () => {
      if (!fileContent) throw new Error("No file content");

      const data = JSON.parse(fileContent);
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/config/import`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            data,
            options: {
              mode: importMode === "preserveIds" ? 0 : 1, // PreserveIds = 0, SmartMatch = 1
              conflictResolution:
                conflictResolution === "skip"
                  ? 0
                  : conflictResolution === "update"
                  ? 1
                  : 2, // Skip = 0, Update = 1, Fail = 2
            },
          }),
        }
      );

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || "Import failed");
      }

      return response.json() as Promise<ImportResult>;
    },
  });

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFile(e.target.files[0]);
    }
  };

  const handleFile = (file: File) => {
    if (!file.name.endsWith(".json")) {
      alert("Prosím vyberte JSON soubor");
      return;
    }

    setFile(file);

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      setFileContent(content);
    };
    reader.readAsText(file);
  };

  const handleReset = () => {
    setFile(null);
    setFileContent(null);
    importMutation.reset();
  };

  const getFileInfo = () => {
    if (!fileContent) return null;

    try {
      const data = JSON.parse(fileContent);
      const types = [];
      let totalCount = 0;

      if (data.sports?.length) {
        types.push(`${data.sports.length} sportů`);
        totalCount += data.sports.length;
      }
      if (data.countries?.length) {
        types.push(`${data.countries.length} zemí`);
        totalCount += data.countries.length;
      }
      if (data.providers?.length) {
        types.push(`${data.providers.length} providerů`);
        totalCount += data.providers.length;
      }
      if (data.seasons?.length) {
        types.push(`${data.seasons.length} sezón`);
        totalCount += data.seasons.length;
      }
      if (data.leagues?.length) {
        types.push(`${data.leagues.length} lig`);
        totalCount += data.leagues.length;
      }

      return { types, totalCount };
    } catch {
      return null;
    }
  };

  const fileInfo = getFileInfo();

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold mb-2">Import Konfigurace</h1>
            <p className="text-gray-600">
              Importujte konfigurační data z JSON souboru
            </p>
          </div>
          <Link href="/">
            <Button variant="outline">← Zpět</Button>
          </Link>
        </div>

        {/* File Upload */}
        {!file && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Nahrát soubor</CardTitle>
              <CardDescription>
                Vyberte JSON soubor s konfigurací k importu
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div
                className={`border-2 border-dashed rounded-lg p-12 text-center ${
                  dragActive
                    ? "border-primary bg-primary/5"
                    : "border-gray-300"
                }`}
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
              >
                <Upload className="mx-auto h-12 w-12 text-gray-400 mb-4" />
                <p className="text-lg mb-2">
                  Přetáhněte soubor sem nebo klikněte pro výběr
                </p>
                <p className="text-sm text-gray-500 mb-4">
                  Podporován pouze JSON formát
                </p>
                <Button variant="outline" asChild>
                  <label className="cursor-pointer">
                    Vybrat soubor
                    <input
                      type="file"
                      accept=".json"
                      className="hidden"
                      onChange={handleFileInput}
                    />
                  </label>
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* File Info & Preview */}
        {file && !importMutation.isSuccess && (
          <>
            <Card className="mb-6">
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle>Nahraný soubor</CardTitle>
                    <CardDescription>
                      {file.name} ({(file.size / 1024).toFixed(2)} KB)
                    </CardDescription>
                  </div>
                  <Button variant="ghost" size="sm" onClick={handleReset}>
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                {fileInfo && (
                  <Alert>
                    <FileJson className="h-4 w-4" />
                    <AlertTitle>Obsahuje</AlertTitle>
                    <AlertDescription>
                      Celkem entit: <strong>{fileInfo.totalCount}</strong>
                      <br />
                      {fileInfo.types.join(", ")}
                    </AlertDescription>
                  </Alert>
                )}
              </CardContent>
            </Card>

            {/* Import Options */}
            <Card className="mb-6">
              <CardHeader>
                <CardTitle>Nastavení importu</CardTitle>
                <CardDescription>
                  Zvolte způsob importu a řešení konfliktů
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Import Mode */}
                <div>
                  <Label className="text-base font-semibold mb-3 block">
                    Režim importu
                  </Label>
                  <div className="space-y-3">
                    <div className="flex items-start space-x-2">
                      <input
                        type="radio"
                        id="smartMatch"
                        name="importMode"
                        value="smartMatch"
                        checked={importMode === "smartMatch"}
                        onChange={(e) => setImportMode(e.target.value as "smartMatch")}
                        className="mt-1"
                      />
                      <div className="grid gap-1.5 leading-none">
                        <Label htmlFor="smartMatch" className="cursor-pointer">
                          Smart Match (doporučeno)
                        </Label>
                        <p className="text-sm text-gray-500">
                          Párování podle business key (Code). Vhodné pro sdílení
                          konfigurace mezi prostředími.
                        </p>
                      </div>
                    </div>

                    <div className="flex items-start space-x-2">
                      <input
                        type="radio"
                        id="preserveIds"
                        name="importMode"
                        value="preserveIds"
                        checked={importMode === "preserveIds"}
                        onChange={(e) => setImportMode(e.target.value as "preserveIds")}
                        className="mt-1"
                      />
                      <div className="grid gap-1.5 leading-none">
                        <Label htmlFor="preserveIds" className="cursor-pointer">
                          Preserve IDs
                        </Label>
                        <p className="text-sm text-gray-500">
                          Zachová původní GUID ID. Vhodné pro backup/restore.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Conflict Resolution */}
                <div>
                  <Label className="text-base font-semibold mb-3 block">
                    Řešení konfliktů
                  </Label>
                  <div className="space-y-3">
                    <div className="flex items-start space-x-2">
                      <input
                        type="radio"
                        id="update"
                        name="conflictResolution"
                        value="update"
                        checked={conflictResolution === "update"}
                        onChange={(e) => setConflictResolution(e.target.value as "update")}
                        className="mt-1"
                      />
                      <div className="grid gap-1.5 leading-none">
                        <Label htmlFor="update" className="cursor-pointer">
                          Aktualizovat (doporučeno)
                        </Label>
                        <p className="text-sm text-gray-500">
                          Aktualizuje existující entity novými daty.
                        </p>
                      </div>
                    </div>

                    <div className="flex items-start space-x-2">
                      <input
                        type="radio"
                        id="skip"
                        name="conflictResolution"
                        value="skip"
                        checked={conflictResolution === "skip"}
                        onChange={(e) => setConflictResolution(e.target.value as "skip")}
                        className="mt-1"
                      />
                      <div className="grid gap-1.5 leading-none">
                        <Label htmlFor="skip" className="cursor-pointer">
                          Přeskočit
                        </Label>
                        <p className="text-sm text-gray-500">
                          Importuje pouze nové entity, existující přeskočí.
                        </p>
                      </div>
                    </div>

                    <div className="flex items-start space-x-2">
                      <input
                        type="radio"
                        id="fail"
                        name="conflictResolution"
                        value="fail"
                        checked={conflictResolution === "fail"}
                        onChange={(e) => setConflictResolution(e.target.value as "fail")}
                        className="mt-1"
                      />
                      <div className="grid gap-1.5 leading-none">
                        <Label htmlFor="fail" className="cursor-pointer">
                          Selhání
                        </Label>
                        <p className="text-sm text-gray-500">
                          Vyvolá chybu při detekci konfliktu.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Error */}
            {importMutation.isError && (
              <Alert variant="destructive" className="mb-6">
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>Chyba importu</AlertTitle>
                <AlertDescription>
                  {(importMutation.error as Error).message}
                </AlertDescription>
              </Alert>
            )}

            {/* Import Button */}
            <Button
              onClick={() => importMutation.mutate()}
              disabled={importMutation.isPending}
              size="lg"
              className="w-full"
            >
              {importMutation.isPending ? "Importuji..." : "Spustit import"}
            </Button>
          </>
        )}

        {/* Import Results */}
        {importMutation.isSuccess && importMutation.data && (
          <Card className="mb-6">
            <CardHeader>
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-green-600" />
                <CardTitle>Import úspěšný</CardTitle>
              </div>
              <CardDescription>Konfigurace byla naimportována</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Summary */}
              <div className="grid grid-cols-3 gap-4 p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="text-2xl font-bold text-green-600">
                    {importMutation.data.totalCreated}
                  </div>
                  <div className="text-sm text-gray-600">Vytvořeno</div>
                </div>
                <div>
                  <div className="text-2xl font-bold text-blue-600">
                    {importMutation.data.totalUpdated}
                  </div>
                  <div className="text-sm text-gray-600">Aktualizováno</div>
                </div>
                <div>
                  <div className="text-2xl font-bold text-gray-600">
                    {importMutation.data.totalSkipped}
                  </div>
                  <div className="text-sm text-gray-600">Přeskočeno</div>
                </div>
              </div>

              {/* Details */}
              <div className="space-y-2">
                {importMutation.data.sports.created > 0 && (
                  <div className="flex justify-between text-sm">
                    <span>Sporty:</span>
                    <span>
                      {importMutation.data.sports.created} vytvořeno,{" "}
                      {importMutation.data.sports.updated} aktualizováno
                    </span>
                  </div>
                )}
                {importMutation.data.countries.created > 0 && (
                  <div className="flex justify-between text-sm">
                    <span>Země:</span>
                    <span>
                      {importMutation.data.countries.created} vytvořeno,{" "}
                      {importMutation.data.countries.updated} aktualizováno
                    </span>
                  </div>
                )}
                {importMutation.data.providers.created > 0 && (
                  <div className="flex justify-between text-sm">
                    <span>Provideři:</span>
                    <span>
                      {importMutation.data.providers.created} vytvořeno,{" "}
                      {importMutation.data.providers.updated} aktualizováno
                    </span>
                  </div>
                )}
                {importMutation.data.leagues.created > 0 && (
                  <div className="flex justify-between text-sm">
                    <span>Ligy:</span>
                    <span>
                      {importMutation.data.leagues.created} vytvořeno,{" "}
                      {importMutation.data.leagues.updated} aktualizováno
                    </span>
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex gap-2 pt-4">
                <Button onClick={handleReset} variant="outline">
                  Importovat další soubor
                </Button>
                <Button asChild>
                  <Link href="/">Zpět na hlavní stránku</Link>
                </Button>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
