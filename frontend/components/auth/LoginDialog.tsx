"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useUser } from "@/contexts/UserContext";
import { GoogleLoginButton } from "./GoogleLoginButton";

interface LoginDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSwitchToRegister: () => void;
}

export function LoginDialog({
  open,
  onOpenChange,
  onSwitchToRegister,
}: LoginDialogProps) {
  const { login, isLoading, error, clearError } = useUser();
  const [formData, setFormData] = useState({
    email: "",
    password: "",
  });
  const [localError, setLocalError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError(null);
    clearError();

    if (!formData.email || !formData.password) {
      setLocalError("Vyplňte všechna pole");
      return;
    }

    try {
      await login({
        email: formData.email,
        password: formData.password,
      });
      // Success - close dialog
      setFormData({ email: "", password: "" });
      onOpenChange(false);
    } catch (err) {
      // Error is already handled in context
    }
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setFormData({ email: "", password: "" });
      setLocalError(null);
      clearError();
    }
    onOpenChange(newOpen);
  };

  const handleSwitchToRegister = () => {
    setFormData({ email: "", password: "" });
    setLocalError(null);
    clearError();
    onSwitchToRegister();
  };

  const displayError = localError || error;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Přihlášení</DialogTitle>
          <DialogDescription>
            Přihlaste se ke svému účtu
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="login-email">Email</Label>
              <Input
                id="login-email"
                type="email"
                value={formData.email}
                onChange={(e) =>
                  setFormData({ ...formData, email: e.target.value })
                }
                placeholder="vas@email.cz"
                autoComplete="email"
                disabled={isLoading}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="login-password">Heslo</Label>
              <Input
                id="login-password"
                type="password"
                value={formData.password}
                onChange={(e) =>
                  setFormData({ ...formData, password: e.target.value })
                }
                placeholder="Vaše heslo"
                autoComplete="current-password"
                disabled={isLoading}
              />
            </div>

            {displayError && (
              <p className="text-sm text-red-600">{displayError}</p>
            )}

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <span className="w-full border-t" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-white px-2 text-gray-500">
                  nebo
                </span>
              </div>
            </div>

            <GoogleLoginButton
              onSuccess={() => {
                setFormData({ email: "", password: "" });
                onOpenChange(false);
              }}
            />
          </div>

          <DialogFooter>
            <div className="flex flex-col sm:flex-row gap-2 w-full items-center">
              <div className="flex-1 text-sm text-gray-500">
                Nemáte účet?{" "}
                <button
                  type="button"
                  onClick={handleSwitchToRegister}
                  className="text-blue-600 hover:underline"
                >
                  Registrovat se
                </button>
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => handleOpenChange(false)}
                  disabled={isLoading}
                >
                  Zrušit
                </Button>
                <Button type="submit" disabled={isLoading}>
                  {isLoading ? "Přihlašování..." : "Přihlásit se"}
                </Button>
              </div>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
