import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/contexts/auth-context";
import { AlertProvider } from "@/contexts/alert-context";
import { LoadingProvider } from "@/contexts/loading-context";
import { Navbar } from "@/components/navbar";
import { BottomNav } from "@/components/bottom-nav";
import { PageGuard } from "@/components/page-guard";
import { PwaInstallBanner } from "@/components/pwa-install-banner";

export const metadata: Metadata = {
  title: "ScoreCast",
  description: "Predict football scores, compete with friends",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <AuthProvider>
          <AlertProvider>
            <LoadingProvider>
              <Navbar />
              <PageGuard>
                <main className="max-w-5xl mx-auto px-4 pb-24 md:pb-8">{children}</main>
              </PageGuard>
              <BottomNav />
              <PwaInstallBanner />
            </LoadingProvider>
          </AlertProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
