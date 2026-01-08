import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import "flag-icons/css/flag-icons.min.css";
import { Providers } from "@/lib/providers";
import { Header } from "@/components/Header";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Sazkomat - Betting Data Analysis Platform",
  description: "Historical betting data import and analysis platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="cs">
      <body className={inter.className}>
        <Providers>
          <Header />
          {children}
        </Providers>
      </body>
    </html>
  );
}
