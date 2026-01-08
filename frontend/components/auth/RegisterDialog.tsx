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
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useUser } from "@/contexts/UserContext";
import { LanguagePreference } from "@/lib/api/types";
import { GoogleLoginButton } from "./GoogleLoginButton";
import { Clock, CheckCircle2 } from "lucide-react";

interface RegisterDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSwitchToLogin: () => void;
}

export function RegisterDialog({
  open,
  onOpenChange,
  onSwitchToLogin,
}: RegisterDialogProps) {
  const { register, isLoading, error, clearError } = useUser();
  const [formData, setFormData] = useState({
    email: "",
    password: "",
    confirmPassword: "",
    displayName: "",
    languagePreference: LanguagePreference.Czech,
  });
  const [localError, setLocalError] = useState<string | null>(null);
  const [registrationSuccess, setRegistrationSuccess] = useState<{
    isApproved: boolean;
    email: string;
  } | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError(null);
    clearError();

    // Validation
    if (!formData.email || !formData.password) {
      setLocalError("Vyplňte email a heslo");
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setLocalError("Hesla se neshodují");
      return;
    }

    if (formData.password.length < 8) {
      setLocalError("Heslo musí mít alespoň 8 znaků");
      return;
    }

    try {
      const response = await register({
        email: formData.email,
        password: formData.password,
        displayName: formData.displayName || undefined,
        languagePreference: formData.languagePreference,
      });

      // Check if user is approved
      if (response.user.isApproved) {
        // Success - close dialog (user is auto-approved, e.g., admin)
        resetForm();
        onOpenChange(false);
      } else {
        // User needs approval - show success message
        setRegistrationSuccess({
          isApproved: false,
          email: formData.email,
        });
      }
    } catch (err) {
      // Error is already handled in context
    }
  };

  const resetForm = () => {
    setFormData({
      email: "",
      password: "",
      confirmPassword: "",
      displayName: "",
      languagePreference: LanguagePreference.Czech,
    });
    setLocalError(null);
    setRegistrationSuccess(null);
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      resetForm();
      clearError();
    }
    onOpenChange(newOpen);
  };

  const handleSwitchToLogin = () => {
    resetForm();
    clearError();
    onSwitchToLogin();
  };

  const displayError = localError || error;

  // Show success message when registration needs approval
  if (registrationSuccess && !registrationSuccess.isApproved) {
    return (
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <CheckCircle2 className="w-5 h-5 text-green-600" />
              Registrace úspěšná
            </DialogTitle>
          </DialogHeader>

          <div className="py-4">
            <Alert className="bg-yellow-50 border-yellow-200">
              <Clock className="h-4 w-4 text-yellow-600" />
              <AlertTitle className="text-yellow-800">Čeká na schválení</AlertTitle>
              <AlertDescription className="text-yellow-700">
                Váš účet <strong>{registrationSuccess.email}</strong> byl vytvořen a čeká na schválení administrátorem.
                <br />
                <br />
                Jakmile bude váš účet schválen, budete se moci přihlásit.
              </AlertDescription>
            </Alert>
          </div>

          <DialogFooter>
            <Button onClick={() => handleOpenChange(false)}>
              Rozumím
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Registrace</DialogTitle>
          <DialogDescription>
            Vytvořte si nový účet
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="register-email">Email *</Label>
              <Input
                id="register-email"
                type="email"
                value={formData.email}
                onChange={(e) =>
                  setFormData({ ...formData, email: e.target.value })
                }
                placeholder="vas@email.cz"
                autoComplete="email"
                disabled={isLoading}
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="register-displayName">Jméno</Label>
              <Input
                id="register-displayName"
                type="text"
                value={formData.displayName}
                onChange={(e) =>
                  setFormData({ ...formData, displayName: e.target.value })
                }
                placeholder="Jan Novák"
                autoComplete="name"
                disabled={isLoading}
              />
              <p className="text-xs text-gray-500">
                Zobrazované jméno (volitelné)
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="register-password">Heslo *</Label>
              <Input
                id="register-password"
                type="password"
                value={formData.password}
                onChange={(e) =>
                  setFormData({ ...formData, password: e.target.value })
                }
                placeholder="Minimálně 8 znaků"
                autoComplete="new-password"
                disabled={isLoading}
                required
              />
              <p className="text-xs text-gray-500">
                Min. 8 znaků, velké/malé písmeno, číslice, speciální znak
              </p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="register-confirmPassword">Potvrdit heslo *</Label>
              <Input
                id="register-confirmPassword"
                type="password"
                value={formData.confirmPassword}
                onChange={(e) =>
                  setFormData({ ...formData, confirmPassword: e.target.value })
                }
                placeholder="Zopakujte heslo"
                autoComplete="new-password"
                disabled={isLoading}
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="register-language">Jazyk</Label>
              <select
                id="register-language"
                value={formData.languagePreference}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    languagePreference: e.target.value as LanguagePreference,
                  })
                }
                className="flex h-10 w-full rounded-md border border-gray-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={isLoading}
              >
                <option value={LanguagePreference.Czech}>Čeština</option>
                <option value={LanguagePreference.English}>English</option>
              </select>
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
              languagePreference={formData.languagePreference}
              onSuccess={() => {
                resetForm();
                onOpenChange(false);
              }}
              onPendingApproval={(email) => {
                setRegistrationSuccess({
                  isApproved: false,
                  email,
                });
              }}
            />
          </div>

          <DialogFooter>
            <div className="flex flex-col sm:flex-row gap-2 w-full items-center">
              <div className="flex-1 text-sm text-gray-500">
                Máte účet?{" "}
                <button
                  type="button"
                  onClick={handleSwitchToLogin}
                  className="text-blue-600 hover:underline"
                >
                  Přihlásit se
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
                  {isLoading ? "Registrace..." : "Registrovat"}
                </Button>
              </div>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
