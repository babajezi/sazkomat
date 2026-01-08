"use client";

import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useUser } from "@/contexts/UserContext";
import { LanguagePreference } from "@/lib/api/types";

// Type declarations for Google Identity Services
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
            auto_select?: boolean;
          }) => void;
          renderButton: (
            element: HTMLElement,
            options: {
              theme?: "outline" | "filled_blue" | "filled_black";
              size?: "large" | "medium" | "small";
              text?: "signin_with" | "signup_with" | "continue_with" | "signin";
              shape?: "rectangular" | "pill" | "circle" | "square";
              logo_alignment?: "left" | "center";
              width?: number;
              locale?: string;
            }
          ) => void;
          prompt: () => void;
        };
      };
    };
  }
}

interface GoogleLoginButtonProps {
  languagePreference?: LanguagePreference;
  onSuccess?: () => void;
  onPendingApproval?: (email: string) => void;
  onError?: (error: string) => void;
}

export function GoogleLoginButton({
  languagePreference = LanguagePreference.Czech,
  onSuccess,
  onPendingApproval,
  onError,
}: GoogleLoginButtonProps) {
  const { googleLogin, isLoading } = useUser();
  const buttonRef = useRef<HTMLDivElement>(null);
  const [isScriptLoaded, setIsScriptLoaded] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const [googleClientId, setGoogleClientId] = useState<string | null>(null);

  // Check for Google Client ID from environment
  useEffect(() => {
    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (clientId && clientId !== "YOUR_GOOGLE_CLIENT_ID") {
      setGoogleClientId(clientId);
    }
  }, []);

  // Load Google Identity Services script
  useEffect(() => {
    if (!googleClientId) return;

    const existingScript = document.querySelector(
      'script[src="https://accounts.google.com/gsi/client"]'
    );

    if (existingScript) {
      setIsScriptLoaded(true);
      return;
    }

    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    script.onload = () => setIsScriptLoaded(true);
    script.onerror = () => {
      console.error("Failed to load Google Identity Services");
      onError?.("Nepodařilo se načíst Google přihlášení");
    };
    document.head.appendChild(script);

    return () => {
      // Don't remove script on unmount as it might be used by other components
    };
  }, [googleClientId, onError]);

  // Initialize Google Sign-In
  useEffect(() => {
    if (!isScriptLoaded || !googleClientId || !window.google || isInitialized) {
      return;
    }

    try {
      window.google.accounts.id.initialize({
        client_id: googleClientId,
        callback: async (response) => {
          if (response.credential) {
            try {
              const authResponse = await googleLogin({
                idToken: response.credential,
                languagePreference,
              });
              // Check if user is approved
              if (authResponse.user.isApproved) {
                onSuccess?.();
              } else {
                onPendingApproval?.(authResponse.user.email);
              }
            } catch (err) {
              onError?.((err as Error).message);
            }
          }
        },
      });
      setIsInitialized(true);
    } catch (err) {
      console.error("Failed to initialize Google Sign-In:", err);
      onError?.("Nepodařilo se inicializovat Google přihlášení");
    }
  }, [isScriptLoaded, googleClientId, isInitialized, googleLogin, languagePreference, onSuccess, onPendingApproval, onError]);

  // Render Google button
  useEffect(() => {
    if (!isInitialized || !buttonRef.current || !window.google) {
      return;
    }

    try {
      // Clear previous button
      buttonRef.current.innerHTML = "";

      window.google.accounts.id.renderButton(buttonRef.current, {
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "rectangular",
        logo_alignment: "left",
        width: buttonRef.current.offsetWidth || 300,
        locale: languagePreference === LanguagePreference.Czech ? "cs" : "en",
      });
    } catch (err) {
      console.error("Failed to render Google button:", err);
    }
  }, [isInitialized, languagePreference]);

  // If no Google Client ID, show placeholder button
  if (!googleClientId) {
    return (
      <Button
        type="button"
        variant="outline"
        className="w-full flex items-center justify-center gap-2"
        disabled
      >
        <GoogleIcon className="w-5 h-5" />
        <span className="text-gray-400">Google přihlášení není nakonfigurováno</span>
      </Button>
    );
  }

  // Show loading state while script loads
  if (!isInitialized) {
    return (
      <Button
        type="button"
        variant="outline"
        className="w-full flex items-center justify-center gap-2"
        disabled
      >
        <GoogleIcon className="w-5 h-5" />
        <span>Načítání...</span>
      </Button>
    );
  }

  // Google's rendered button
  return (
    <div className="w-full">
      <div
        ref={buttonRef}
        className="flex justify-center"
        style={{ minHeight: "40px" }}
      />
    </div>
  );
}

// Simple Google icon component
function GoogleIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24">
      <path
        fill="#4285F4"
        d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
      />
      <path
        fill="#34A853"
        d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
      />
      <path
        fill="#FBBC05"
        d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
      />
      <path
        fill="#EA4335"
        d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
      />
    </svg>
  );
}
