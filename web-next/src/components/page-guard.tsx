"use client";

import { useAuth } from "@/contexts/auth-context";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, ReactNode } from "react";
import { ScoreCastLoading } from "./scorecast-loading";

const PUBLIC = ["/", "/login", "/register", "/forgot-password", "/verify-email", "/how-to-play", "/install", "/not-found"];
const PUBLIC_PREFIXES = ["/replay/", "/gw-replay/"];

function isPublic(path: string) {
  return PUBLIC.includes(path) || PUBLIC_PREFIXES.some((p) => path.startsWith(p));
}

export function PageGuard({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  const path = usePathname();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !user && !isPublic(path)) router.replace("/login");
  }, [loading, user, path, router]);

  if (loading && !isPublic(path)) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <ScoreCastLoading />
      </div>
    );
  }

  return <>{children}</>;
}
