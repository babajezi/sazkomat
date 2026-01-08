"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useUser } from "@/contexts/UserContext";
import { LoginDialog } from "@/components/auth/LoginDialog";
import { RegisterDialog } from "@/components/auth/RegisterDialog";
import { LogIn, UserPlus, Loader2 } from "lucide-react";

// Landing page for unauthenticated users
function LandingPage() {
  const [showLogin, setShowLogin] = useState(false);
  const [showRegister, setShowRegister] = useState(false);

  const handleSwitchToRegister = () => {
    setShowLogin(false);
    setShowRegister(true);
  };

  const handleSwitchToLogin = () => {
    setShowRegister(false);
    setShowLogin(true);
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100 flex items-center justify-center">
      <div className="container mx-auto px-4">
        <div className="max-w-2xl mx-auto text-center">
          <div className="mb-12">
            <h1 className="text-5xl font-bold tracking-tight text-gray-900 sm:text-7xl mb-6">
              Sazkomat
            </h1>
            <p className="text-xl text-gray-600 mb-8">
              Platforma pro import a analýzu historických sázkových dat
            </p>
            <p className="text-sm text-gray-500 mb-8">
              Pro přístup k aplikaci se musíte přihlásit.
            </p>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12">
            <Button
              size="lg"
              onClick={() => setShowLogin(true)}
              className="gap-2"
            >
              <LogIn className="h-5 w-5" />
              Přihlásit se
            </Button>
            <Button
              size="lg"
              variant="outline"
              onClick={() => setShowRegister(true)}
              className="gap-2"
            >
              <UserPlus className="h-5 w-5" />
              Registrovat se
            </Button>
          </div>

          <Card className="text-left">
            <CardHeader>
              <CardTitle>O platformě</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-gray-600">
              <p>
                Sazkomat je platforma určená pro import historických dat z
                webové stránky BetExplorer.com a jejich následnou analýzu pro
                predikce sportovních výsledků.
              </p>
              <p>
                <strong>Fáze 1:</strong> Konfigurace lig, import historických
                dat, perzistence do PostgreSQL
              </p>
              <p>
                <strong>Technologie:</strong> .NET 10, PostgreSQL 16, Next.js 15,
                Docker
              </p>
            </CardContent>
          </Card>
        </div>
      </div>

      <LoginDialog
        open={showLogin}
        onOpenChange={setShowLogin}
        onSwitchToRegister={handleSwitchToRegister}
      />

      <RegisterDialog
        open={showRegister}
        onOpenChange={setShowRegister}
        onSwitchToLogin={handleSwitchToLogin}
      />
    </div>
  );
}

// Dashboard for authenticated users
function Dashboard() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100">
      <div className="container mx-auto px-4 py-16">
        <div className="max-w-4xl mx-auto">
          <div className="text-center mb-12">
            <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-6xl mb-4">
              Sazkomat
            </h1>
            <p className="text-lg text-gray-600">
              Platforma pro import a analýzu historických sázkových dat
            </p>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-semibold mb-4">Hlavní funkce</h2>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4 mb-6">
              <Card>
                <CardHeader>
                  <CardTitle>📊 Dashboard</CardTitle>
                  <CardDescription>
                    Přehled statistik a importovaných dat
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/dashboard">
                    <Button className="w-full">Zobrazit dashboard</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>🎯 Kola</CardTitle>
                  <CardDescription>
                    Přehled kol s agregovanými kurzy a expanzí
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/rounds">
                    <Button className="w-full">Zobrazit kola</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>⚽ Zápasy</CardTitle>
                  <CardDescription>
                    Přehled jednotlivých zápasů
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/matches">
                    <Button className="w-full">Zobrazit zápasy</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>📥 Import Dat</CardTitle>
                  <CardDescription>
                    Spusťte historický import dat z BetExplorer
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/import">
                    <Button className="w-full">Spustit import</Button>
                  </Link>
                </CardContent>
              </Card>
            </div>

            <div className="grid gap-6 md:grid-cols-2 mb-6">
              <Card>
                <CardHeader>
                  <CardTitle>🔄 Synchronizace</CardTitle>
                  <CardDescription>
                    Synchronizace zemí, lig a sezón z BetExplorer
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/sync">
                    <Button className="w-full">Spustit synchronizaci</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>📋 Job Monitoring</CardTitle>
                  <CardDescription>
                    Sledování běžících a dokončených synchronizačních úloh
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/jobs">
                    <Button className="w-full">Zobrazit joby</Button>
                  </Link>
                </CardContent>
              </Card>
            </div>

            <h2 className="text-2xl font-semibold mb-4 mt-8">Konfigurace</h2>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
              <Card>
                <CardHeader>
                  <CardTitle>🏆 Sporty</CardTitle>
                  <CardDescription>
                    Aktivujte nebo deaktivujte sporty v systému
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/sports">
                    <Button className="w-full" variant="outline">Správa sportů</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>🌐 Země</CardTitle>
                  <CardDescription>
                    Spravujte číselník zemí pro sportovní ligy
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/countries">
                    <Button className="w-full" variant="outline">Správa zemí</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>🏟️ Ligy</CardTitle>
                  <CardDescription>
                    Spravujte sportovní ligy a jejich nastavení
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/leagues">
                    <Button className="w-full" variant="outline">Správa lig</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>🔌 Provideři</CardTitle>
                  <CardDescription>
                    Správa datových providerů a jejich mapování
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/providers">
                    <Button className="w-full" variant="outline">Správa providerů</Button>
                  </Link>
                </CardContent>
              </Card>
            </div>

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4 mt-6">
              <Card>
                <CardHeader>
                  <CardTitle>🔗 Mapování Názvů Lig</CardTitle>
                  <CardDescription>
                    Manuální mapování názvů lig mezi providery a BetExplorer
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/mappings">
                    <Button className="w-full" variant="outline">Správa mapování lig</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>🌍 Mapování Názvů Zemí</CardTitle>
                  <CardDescription>
                    Manuální mapování názvů zemí mezi providery a BetExplorer
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/country-mappings">
                    <Button className="w-full" variant="outline">Správa mapování zemí</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card className="border-yellow-200">
                <CardHeader>
                  <CardTitle>⚠️ Nespárované Ligy</CardTitle>
                  <CardDescription>
                    Ligy z betting providerů bez shody v BetExploreru
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/unmatched-leagues">
                    <Button className="w-full" variant="outline">Zobrazit nespárované</Button>
                  </Link>
                </CardContent>
              </Card>
            </div>

            <h2 className="text-2xl font-semibold mb-4 mt-8">Pokročilé nástroje</h2>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
              <Card>
                <CardHeader>
                  <CardTitle>🔄 Fronty</CardTitle>
                  <CardDescription>
                    Monitoring a správa synchronizačních front
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/admin/queues">
                    <Button className="w-full" variant="outline">Zobrazit fronty</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>📤 Export</CardTitle>
                  <CardDescription>
                    Exportujte konfiguraci do JSON souboru
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/config/export">
                    <Button className="w-full" variant="outline">Export konfigurace</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>📥 Import</CardTitle>
                  <CardDescription>
                    Importujte konfiguraci z JSON souboru
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/config/import">
                    <Button className="w-full" variant="outline">Import konfigurace</Button>
                  </Link>
                </CardContent>
              </Card>

              <Card className="border-orange-200">
                <CardHeader>
                  <CardTitle className="text-orange-900">⚙️ Správa dat</CardTitle>
                  <CardDescription>
                    Reset databáze a pokročilé nástroje pro správu dat
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <Link href="/admin">
                    <Button className="w-full" variant="outline">Správa dat</Button>
                  </Link>
                </CardContent>
              </Card>
            </div>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>O platformě</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-gray-600">
              <p>
                Sazkomat je platforma určená pro import historických dat z
                webové stránky BetExplorer.com a jejich následnou analýzu pro
                predikce sportovních výsledků.
              </p>
              <p>
                <strong>Fáze 1:</strong> Konfigurace lig, import historických
                dat, perzistence do PostgreSQL
              </p>
              <p>
                <strong>Technologie:</strong> .NET 10, PostgreSQL 16, Next.js 15,
                Docker
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

export default function Home() {
  const { isAuthenticated, isLoading } = useUser();

  // Show loading state while checking auth
  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100 flex items-center justify-center">
        <div className="text-center">
          <Loader2 className="h-8 w-8 animate-spin text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">Načítání...</p>
        </div>
      </div>
    );
  }

  // Show landing page for unauthenticated users
  if (!isAuthenticated) {
    return <LandingPage />;
  }

  // Show dashboard for authenticated users
  return <Dashboard />;
}
