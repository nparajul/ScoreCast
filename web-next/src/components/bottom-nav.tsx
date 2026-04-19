"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/contexts/auth-context";

const tabs = [
  { href: "/dashboard", label: "Predict", icon: "🏆" },
  { href: "/scores", label: "Scores", icon: "⚽" },
  { href: "/points-table", label: "Tables", icon: "📊" },
  { href: "/teams", label: "Teams", icon: "🛡️" },
  { href: "/settings", label: "More", icon: "⚙️" },
];

export function BottomNav() {
  const { user } = useAuth();
  const pathname = usePathname();

  if (!user) return null;

  return (
    <nav className="fixed bottom-3 left-1/2 -translate-x-1/2 z-50 md:hidden">
      <div
        className="flex items-center gap-1 px-4 py-2 rounded-[28px]"
        style={{ background: "rgba(30,30,30,0.85)", backdropFilter: "blur(20px)", WebkitBackdropFilter: "blur(20px)" }}
      >
        {tabs.map((tab) => {
          const active = pathname.startsWith(tab.href);
          return (
            <Link
              key={tab.href}
              href={tab.href}
              className={`flex flex-col items-center px-3 py-1 rounded-2xl text-[10px] font-semibold transition-colors ${
                active ? "text-white bg-white/15" : "text-white/60"
              }`}
            >
              <span className="text-base">{tab.icon}</span>
              {tab.label}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
