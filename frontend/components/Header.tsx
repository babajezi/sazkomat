"use client";

import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useUser } from "@/contexts/UserContext";
import { LoginDialog, RegisterDialog, UserMenu, LanguageSelector } from "@/components/auth";
import { Shield } from "lucide-react";

export function Header() {
  const { isAuthenticated, isLoading, isAdmin } = useUser();
  const [showLoginDialog, setShowLoginDialog] = useState(false);
  const [showRegisterDialog, setShowRegisterDialog] = useState(false);

  const handleSwitchToRegister = () => {
    setShowLoginDialog(false);
    setShowRegisterDialog(true);
  };

  const handleSwitchToLogin = () => {
    setShowRegisterDialog(false);
    setShowLoginDialog(true);
  };

  return (
    <>
      <header className="bg-white border-b border-gray-200 sticky top-0 z-40">
        <div className="container mx-auto px-4">
          <div className="flex items-center justify-between h-14">
            {/* Logo & Navigation */}
            <div className="flex items-center gap-6">
              <Link
                href="/"
                className="text-xl font-bold text-gray-900 hover:text-gray-700 transition-colors"
              >
                Sazkomat
              </Link>

              {isAuthenticated && (
                <nav className="hidden md:flex items-center gap-4">
                  <Link
                    href="/dashboard"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Dashboard
                  </Link>
                  <Link
                    href="/rounds"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Kola
                  </Link>
                  <Link
                    href="/matches"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Zápasy
                  </Link>
                  <Link
                    href="/leagues"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Ligy
                  </Link>
                  <Link
                    href="/sync"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Sync
                  </Link>
                  <Link
                    href="/unmatched-leagues"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Nespárované Ligy
                  </Link>
                  <Link
                    href="/unmatched-countries"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Nespárované Země
                  </Link>
                  <Link
                    href="/recipes"
                    className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    Recepty
                  </Link>
                  {isAdmin && (
                    <Link
                      href="/admin"
                      className="text-sm text-purple-600 hover:text-purple-800 transition-colors flex items-center gap-1"
                    >
                      <Shield className="w-3.5 h-3.5" />
                      Admin
                    </Link>
                  )}
                </nav>
              )}
            </div>

            {/* Auth section */}
            <div className="flex items-center gap-3">
              {isLoading ? (
                <div className="w-8 h-8 rounded-full bg-gray-200 animate-pulse" />
              ) : isAuthenticated ? (
                <UserMenu />
              ) : (
                <>
                  <LanguageSelector size="sm" />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowLoginDialog(true)}
                  >
                    Přihlásit se
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => setShowRegisterDialog(true)}
                    className="hidden sm:inline-flex"
                  >
                    Registrace
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Auth Dialogs */}
      <LoginDialog
        open={showLoginDialog}
        onOpenChange={setShowLoginDialog}
        onSwitchToRegister={handleSwitchToRegister}
      />
      <RegisterDialog
        open={showRegisterDialog}
        onOpenChange={setShowRegisterDialog}
        onSwitchToLogin={handleSwitchToLogin}
      />
    </>
  );
}
