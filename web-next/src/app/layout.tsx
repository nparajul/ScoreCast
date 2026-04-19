import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/contexts/auth-context";
import { Navbar } from "@/components/navbar";
import { BottomNav } from "@/components/bottom-nav";

export const metadata: Metadata = {
  title: "ScoreCast",
  description: "Predict football scores, compete with friends",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <AuthProvider>
          <Navbar />
          <main className="max-w-5xl mx-auto px-4 pb-24 md:pb-8">{children}</main>
          <BottomNav />
        </AuthProvider>
      </body>
    </html>
  );
}
