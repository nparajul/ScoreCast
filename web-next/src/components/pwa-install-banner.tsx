"use client";

import { useEffect, useState, useRef } from "react";

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
}

export function PwaInstallBanner() {
  const [show, setShow] = useState(false);
  const deferredRef = useRef<BeforeInstallPromptEvent | null>(null);

  useEffect(() => {
    if (localStorage.getItem("sc-pwa-dismissed")) return;
    const handler = (e: Event) => {
      e.preventDefault();
      deferredRef.current = e as BeforeInstallPromptEvent;
      setShow(true);
    };
    window.addEventListener("beforeinstallprompt", handler);
    return () => window.removeEventListener("beforeinstallprompt", handler);
  }, []);

  const install = async () => {
    await deferredRef.current?.prompt();
    setShow(false);
  };

  const dismiss = () => {
    localStorage.setItem("sc-pwa-dismissed", "1");
    setShow(false);
  };

  if (!show) return null;

  return (
    <div className="fixed bottom-0 inset-x-0 z-50 p-3 bg-[var(--sc-surface)] border-t border-[var(--sc-border)] flex items-center justify-between gap-3">
      <p className="text-sm font-semibold">📲 Install ScoreCast for the best experience</p>
      <div className="flex gap-2 shrink-0">
        <button onClick={dismiss} className="text-xs opacity-60 hover:opacity-100">Dismiss</button>
        <button onClick={install} className="px-3 py-1 rounded-lg bg-[var(--sc-tertiary)] text-white text-xs font-bold">Install</button>
      </div>
    </div>
  );
}
