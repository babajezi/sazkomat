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

  useEffect(() => {
    if (provider) {
      // Reset form when provider changes
      setUsername("");
      setPassword("");
      setSessionCookies("");
      setTimeout("");
      setProxyUrl("");
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
    mutationFn: (data: { timeout?: number; proxyUrl?: string }) =>
      configApi.updateProviderConfiguration(provider!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["betting-providers"] });
      onOpenChange(false);
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
    });
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
