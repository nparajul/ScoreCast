"use client";

import Link from "next/link";
import { useAuth } from "@/contexts/auth-context";
import { usePathname } from "next/navigation";

const navLinks = [
  { href: "/dashboard", label: "Predict" },
  { href: "/scores", label: "Scores" },
  { href: "/points-table", label: "Tables" },
  { href: "/teams", label: "Teams" },
  { href: "/settings", label: "Settings" },
];

export function Navbar() {
  const { user, signOut } = useAuth();
  const pathname = usePathname();

  return (
    <header className="sticky top-0 z-50 bg-[var(--sc-primary)] text-white">
      <div className="max-w-5xl mx-auto flex items-center justify-between px-4 h-14">
        <Link href="/" className="text-lg font-bold tracking-tight">
          ⚽ ScoreCast
        </Link>
        <nav className="hidden md:flex items-center gap-1">
          {user && navLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                pathname.startsWith(link.href)
                  ? "bg-white/15 text-white"
                  : "text-white/70 hover:text-white hover:bg-white/10"
              }`}
            >
              {link.label}
            </Link>
          ))}
          {user ? (
            <button onClick={signOut} className="ml-2 px-3 py-1.5 text-sm text-white/70 hover:text-white">
              Logout
            </button>
          ) : (
            <Link href="/login" className="ml-2 px-3 py-1.5 text-sm bg-[var(--sc-tertiary)] rounded-lg font-medium">
              Login
            </Link>
          )}
        </nav>
      </div>
    </header>
  );
}
