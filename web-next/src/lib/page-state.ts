"use client";

import { useState, useCallback } from "react";

export function saveState(key: string, value: unknown) {
  try { sessionStorage.setItem(`sc-${key}`, JSON.stringify(value)); } catch {}
}

export function restoreState<T>(key: string, defaultValue: T): T {
  try {
    const v = sessionStorage.getItem(`sc-${key}`);
    return v ? (JSON.parse(v) as T) : defaultValue;
  } catch { return defaultValue; }
}

export function useSessionState<T>(key: string, defaultValue: T) {
  const [value, setValue] = useState<T>(() => restoreState(key, defaultValue));
  const set = useCallback((v: T | ((prev: T) => T)) => {
    setValue((prev) => {
      const next = typeof v === "function" ? (v as (p: T) => T)(prev) : v;
      saveState(key, next);
      return next;
    });
  }, [key]);
  return [value, set] as const;
}
