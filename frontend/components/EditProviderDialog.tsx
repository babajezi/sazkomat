"use client";

import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { configApi } from "@/lib/api/client";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TagInput } from "@/components/ui/tag-input";
import { ProviderLogo } from "@/components/ProviderLogo";
import { Loader2, Trash2, Upload, Plus, X } from "lucide-react";
import type { DataProvider } from "@/lib/api/types";
import { ProviderType } from "@/lib/api/types";

interface EditProviderDialogProps {
  provider: DataProvider | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function EditProviderDialog({
  provider,
  open,
  onOpenChange,
}: EditProviderDialogProps) {
  const queryClient = useQueryClient();

  // Credentials state
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [sessionCookies, setSessionCookies] = useState("");

  // Configuration state
  const [timeout, setTimeout] = useState("");
  const [proxyUrl, setProxyUrl] = useState("");
  const [excludedCountryIds, setExcludedCountryIds] = useState<string[]>([]);
  const [excludedLeagueIds, setExcludedLeagueIds] = useState<string[]>([]);
  const [customSettings, setCustomSettings] = useState<Record<string, string>>({});
  const [newSettingKey, setNewSettingKey] = useState("");
  const [newSettingValue, setNewSettingValue] = useState("");

  // Logo state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  useEffect(() => {
    if (provider) {
      // Reset credentials
      setUsername("");
      setPassword("");
      setSessionCookies("");
      setSelectedFile(null);

      // Load configuration from provider.configuration JSONB
      if (provider.configuration) {
        try {
          const config = typeof provider.configuration === "string"
            ? JSON.parse(provider.configuration)
            : provider.configuration;

          setTimeout(config.timeout?.toString() || "");
          setProxyUrl(config.proxyUrl || "");
          setExcludedCountryIds(config.excludedCountryIds || []);
          setExcludedLeagueIds(config.excludedLeagueIds || []);
          setCustomSettings(config.customSettings || {});
        } catch (e) {
          console.error("Failed to parse provider configuration:", e);
          setTimeout("");
          setProxyUrl("");
          setExcludedCountryIds([]);
          setExcludedLeagueIds([]);
          setCustomSettings({});
        }
      } else {
        // Reset configuration state if no config exists
        setTimeout("");
        setProxyUrl("");
        setExcludedCountryIds([]);
        setExcludedLeagueIds([]);
        setCustomSettings({});
      }
    }
  }, [provider]);

  const credentialsMutation = useMutation({
    mutationFn: (data: { username?: string; password?: string; sessionCookies?: string }) =>
      configApi.updateProviderCredentials(provider!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      onOpenChange(false);
    },
  });

  const configMutation = useMutation({
    mutationFn: (data: {
      timeout?: number;
      proxyUrl?: string;
      excludedCountryIds?: string[];
      excludedLeagueIds?: string[];
      customSettings?: Record<string, string>;
    }) =>
      configApi.updateProviderConfiguration(provider!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      onOpenChange(false);
    },
  });

  const uploadLogoMutation = useMutation({
    mutationFn: (file: File) =>
      configApi.uploadProviderLogo(provider!.id, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      queryClient.invalidateQueries({ queryKey: ["providers"] });
      setSelectedFile(null);
    },
  });

  const deleteLogoMutation = useMutation({
    mutationFn: () =>
      configApi.deleteProviderLogo(provider!.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      queryClient.invalidateQueries({ queryKey: ["providers"] });
    },
  });

  const handleSaveCredentials = () => {
    credentialsMutation.mutate({
      username: username || undefined,
      password: password || undefined,
      sessionCookies: sessionCookies || undefined,
    });
  };

  const handleSaveConfiguration = () => {
    configMutation.mutate({
      timeout: timeout ? parseInt(timeout) : undefined,
      proxyUrl: proxyUrl || undefined,
      excludedCountryIds: excludedCountryIds.length > 0 ? excludedCountryIds : undefined,
      excludedLeagueIds: excludedLeagueIds.length > 0 ? excludedLeagueIds : undefined,
      customSettings: Object.keys(customSettings).length > 0 ? customSettings : undefined,
    });
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      // Validate file size (max 5MB)
      if (file.size > 5 * 1024 * 1024) {
        alert("File size must be less than 5MB");
        return;
      }
      // Validate file type
      const allowedTypes = ["image/jpeg", "image/png", "image/svg+xml"];
      if (!allowedTypes.includes(file.type)) {
        alert("Only JPG, PNG, and SVG files are allowed");
        return;
      }
      setSelectedFile(file);
    }
  };

  const handleUploadLogo = () => {
    if (selectedFile) {
      uploadLogoMutation.mutate(selectedFile);
    }
  };

  const handleDeleteLogo = () => {
    if (window.confirm("Opravdu chcete smazat logo providera?")) {
      deleteLogoMutation.mutate();
    }
  };

  const handleAddCustomSetting = () => {
    if (newSettingKey && newSettingValue) {
      setCustomSettings({ ...customSettings, [newSettingKey]: newSettingValue });
      setNewSettingKey("");
      setNewSettingValue("");
    }
  };

  const handleRemoveCustomSetting = (key: string) => {
    const newSettings = { ...customSettings };
    delete newSettings[key];
    setCustomSettings(newSettings);
  };

  if (!provider) return null;

  const isBettingProvider = provider.type === ProviderType.BettingProvider;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Edit Provider: {provider.name}</DialogTitle>
          <DialogDescription>
            Configure credentials and settings for {provider.name}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6">
          {/* Credentials Section */}
          <div className="space-y-4">
            <h3 className="text-sm font-medium">Credentials</h3>
            {isBettingProvider ? (
              <>
                <div className="space-y-2">
                  <Label htmlFor="username">Username</Label>
                  <Input
                    id="username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    placeholder="Enter username"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Enter password"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="sessionCookies">Session Cookies</Label>
                  <Input
                    id="sessionCookies"
                    value={sessionCookies}
                    onChange={(e) => setSessionCookies(e.target.value)}
                    placeholder="Optional: Paste session cookies"
                  />
                  <p className="text-xs text-gray-500">
                    Pro dlouhodobé sessions můžete uložit cookies z prohlížeče
                  </p>
                </div>

                <Button
                  onClick={handleSaveCredentials}
                  disabled={credentialsMutation.isPending}
                  className="w-full"
                >
                  {credentialsMutation.isPending ? "Saving..." : "Save Credentials"}
                </Button>
              </>
            ) : (
              <p className="text-sm text-gray-500">
                Credentials are only needed for betting providers
              </p>
            )}
          </div>

          <div className="border-t my-4"></div>

          {/* Logo Section */}
          <div className="space-y-4">
            <h3 className="text-sm font-medium">Logo</h3>

            <div className="flex items-start gap-4">
              {/* Current Logo Preview */}
              <div className="flex-shrink-0">
                <ProviderLogo provider={provider} size="md" />
              </div>

              <div className="flex-1 space-y-3">
                {/* Logo Info */}
                {provider.hasLogo ? (
                  <div className="text-sm text-muted-foreground">
                    Logo nahrané: {provider.logoUploadedAt ? new Date(provider.logoUploadedAt).toLocaleDateString("cs-CZ") : "N/A"}
                  </div>
                ) : (
                  <div className="text-sm text-muted-foreground">
                    Žádné logo (zobrazují se iniciály)
                  </div>
                )}

                {/* File Input */}
                <div className="space-y-2">
                  <Label htmlFor="logo-file">Nahrát nové logo</Label>
                  <Input
                    id="logo-file"
                    type="file"
                    accept=".jpg,.jpeg,.png,.svg"
                    onChange={handleFileChange}
                    disabled={uploadLogoMutation.isPending}
                  />
                  <p className="text-xs text-muted-foreground">
                    Max 5MB. Formáty: JPG, PNG, SVG. Logo bude normalizováno na 64/128/256px a převedeno do WebP.
                  </p>
                </div>

                {/* Selected File Info */}
                {selectedFile && (
                  <div className="text-sm text-green-600">
                    Vybraný soubor: {selectedFile.name} ({(selectedFile.size / 1024).toFixed(1)} KB)
                  </div>
                )}

                {/* Action Buttons */}
                <div className="flex gap-2">
                  <Button
                    onClick={handleUploadLogo}
                    disabled={!selectedFile || uploadLogoMutation.isPending}
                    size="sm"
                  >
                    {uploadLogoMutation.isPending ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Nahrávám...
                      </>
                    ) : (
                      <>
                        <Upload className="mr-2 h-4 w-4" />
                        Nahrát Logo
                      </>
                    )}
                  </Button>

                  {provider.hasLogo && (
                    <Button
                      onClick={handleDeleteLogo}
                      disabled={deleteLogoMutation.isPending}
                      variant="destructive"
                      size="sm"
                    >
                      {deleteLogoMutation.isPending ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Mažu...
                        </>
                      ) : (
                        <>
                          <Trash2 className="mr-2 h-4 w-4" />
                          Smazat Logo
                        </>
                      )}
                    </Button>
                  )}
                </div>
              </div>
            </div>
          </div>

          <div className="border-t my-4"></div>

          {/* Configuration Section */}
          <div className="space-y-4">
            <h3 className="text-sm font-medium">Configuration</h3>
            <div className="space-y-2">
              <Label htmlFor="timeout">Timeout (ms)</Label>
              <Input
                id="timeout"
                type="number"
                value={timeout}
                onChange={(e) => setTimeout(e.target.value)}
                placeholder="30000"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="proxyUrl">Proxy URL</Label>
              <Input
                id="proxyUrl"
                value={proxyUrl}
                onChange={(e) => setProxyUrl(e.target.value)}
                placeholder="http://proxy.example.com:8080"
              />
            </div>

            {isBettingProvider && (
              <>
                <div className="space-y-2">
                  <Label>Excluded Country IDs</Label>
                  <TagInput
                    value={excludedCountryIds}
                    onChange={setExcludedCountryIds}
                    placeholder="Zadejte ID soutěže k vyloučení (např. 493g)"
                  />
                  <p className="text-xs text-gray-500">
                    ID speciálních sekcí, které se nemají importovat jako země (např. &quot;Kluby UEFA&quot;, &quot;Mezinárodní&quot;)
                  </p>
                </div>

                <div className="space-y-2">
                  <Label>Excluded League IDs</Label>
                  <TagInput
                    value={excludedLeagueIds}
                    onChange={setExcludedLeagueIds}
                    placeholder="Zadejte ID ligy k vyloučení"
                  />
                  <p className="text-xs text-gray-500">
                    ID konkrétních lig, které se nemají importovat
                  </p>
                </div>

                <div className="space-y-2">
                  <Label>Custom Settings</Label>
                  {Object.keys(customSettings).length > 0 && (
                    <div className="border rounded-md overflow-hidden">
                      <table className="w-full text-sm">
                        <thead className="bg-gray-50">
                          <tr>
                            <th className="text-left p-2 font-medium">Key</th>
                            <th className="text-left p-2 font-medium">Value</th>
                            <th className="w-10"></th>
                          </tr>
                        </thead>
                        <tbody>
                          {Object.entries(customSettings).map(([key, value]) => (
                            <tr key={key} className="border-t">
                              <td className="p-2 font-mono text-xs">{key}</td>
                              <td className="p-2 text-xs">{value}</td>
                              <td className="p-2">
                                <button
                                  type="button"
                                  onClick={() => handleRemoveCustomSetting(key)}
                                  className="text-red-500 hover:text-red-700"
                                >
                                  <X className="h-4 w-4" />
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                  <div className="flex gap-2">
                    <Input
                      placeholder="Key"
                      value={newSettingKey}
                      onChange={(e) => setNewSettingKey(e.target.value)}
                      className="flex-1"
                    />
                    <Input
                      placeholder="Value"
                      value={newSettingValue}
                      onChange={(e) => setNewSettingValue(e.target.value)}
                      className="flex-1"
                    />
                    <Button
                      type="button"
                      onClick={handleAddCustomSetting}
                      disabled={!newSettingKey || !newSettingValue}
                      size="sm"
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                  <p className="text-xs text-gray-500">
                    Vlastní nastavení specifické pro tohoto providera (key-value páry)
                  </p>
                </div>
              </>
            )}

            <Button
              onClick={handleSaveConfiguration}
              disabled={configMutation.isPending}
              className="w-full"
            >
              {configMutation.isPending ? "Saving..." : "Save Configuration"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
