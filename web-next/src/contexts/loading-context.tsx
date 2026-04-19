"use client";

import { createContext, useContext, useState, useCallback, ReactNode } from "react";
import { ScoreCastLoading } from "@/components/scorecast-loading";

interface LoadingCtx {
  loading: boolean;
  message?: string;
  whileLoading: (fn: () => Promise<void>, message?: string) => Promise<void>;
}

const Ctx = createContext<LoadingCtx | null>(null);

export function useLoading() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useLoading must be used within LoadingProvider");
  return ctx;
}

export function LoadingProvider({ children }: { children: ReactNode }) {
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | undefined>();

  const whileLoading = useCallback(async (fn: () => Promise<void>, msg?: string) => {
    setLoading(true);
    setMessage(msg);
    try { await fn(); } finally { setLoading(false); setMessage(undefined); }
  }, []);

  return (
    <Ctx.Provider value={{ loading, message, whileLoading }}>
      {children}
      {loading && (
        <div className="fixed inset-0 z-[200] bg-black/50 flex items-center justify-center">
          <ScoreCastLoading size="text-5xl" message={message} />
        </div>
      )}
    </Ctx.Provider>
  );
}
