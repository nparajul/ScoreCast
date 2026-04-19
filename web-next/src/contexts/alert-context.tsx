"use client";

import { createContext, useContext, useState, useCallback, ReactNode, useRef } from "react";

type Severity = "success" | "error" | "info" | "warning";
interface Alert { id: string; message: string; severity: Severity }

interface AlertCtx {
  add: (message: string, severity?: Severity) => void;
  remove: (id: string) => void;
}

const Ctx = createContext<AlertCtx | null>(null);

export function useAlert() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useAlert must be used within AlertProvider");
  return ctx;
}

export function AlertProvider({ children }: { children: ReactNode }) {
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const counterRef = useRef(0);

  const remove = useCallback((id: string) => setAlerts((a) => a.filter((x) => x.id !== id)), []);

  const add = useCallback((message: string, severity: Severity = "info") => {
    const id = String(++counterRef.current);
    setAlerts((a) => [...a.slice(-9), { id, message, severity }]);
    setTimeout(() => remove(id), 4000);
  }, [remove]);

  return (
    <Ctx.Provider value={{ add, remove }}>
      {children}
      <AlertHost alerts={alerts} remove={remove} />
    </Ctx.Provider>
  );
}

const severityStyles: Record<Severity, string> = {
  success: "bg-green-600", error: "bg-red-600", info: "bg-blue-600", warning: "bg-amber-600",
};

function AlertHost({ alerts, remove }: { alerts: Alert[]; remove: (id: string) => void }) {
  if (!alerts.length) return null;
  return (
    <div className="fixed top-4 right-4 z-[100] flex flex-col gap-2 max-w-sm">
      {alerts.map((a) => (
        <div key={a.id} className={`${severityStyles[a.severity]} text-white text-sm px-4 py-2.5 rounded-lg shadow-lg animate-slide-in flex items-center gap-2`}>
          <span className="flex-1">{a.message}</span>
          <button onClick={() => remove(a.id)} className="opacity-70 hover:opacity-100 text-lg leading-none">&times;</button>
        </div>
      ))}
    </div>
  );
}
