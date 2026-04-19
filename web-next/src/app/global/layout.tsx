"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ReactNode } from "react";

const tabs = [
  { label: "Dashboard", href: "/global" },
  { label: "Predictions", href: "/global/predictions" },
  { label: "Leaderboard", href: "/global/leaderboard" },
  { label: "Recap", href: "/global/recap" },
  { label: "Stats", href: "/global/stats" },
];

export default function GlobalLayout({ children }: { children: ReactNode }) {
  const path = usePathname();
  return (
    <div className="max-w-2xl mx-auto">
      <nav className="sticky top-0 z-10 flex gap-1 px-2 py-2 overflow-x-auto bg-[var(--sc-bg)] border-b border-[var(--sc-border)]">
        {tabs.map((t) => {
          const active = path === t.href;
          return (
            <Link key={t.href} href={t.href}
              className={`px-4 py-1.5 rounded-full text-sm font-semibold whitespace-nowrap transition-colors ${active ? "bg-[var(--sc-tertiary)] text-white" : "text-[var(--sc-text-secondary)] hover:bg-gray-100"}`}>
              {t.label}
            </Link>
          );
        })}
      </nav>
      {children}
    </div>
  );
}
