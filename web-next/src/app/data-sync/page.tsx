"use client";

import { api } from "@/lib/api";
import { CompetitionResult } from "@/lib/types";
import { useAlert } from "@/contexts/alert-context";
import { useEffect, useState, useRef } from "react";

type LogEntry = { action: string; ok: boolean; message: string; time: string };

export default function DataSyncPage() {
  const { add } = useAlert();
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [loading, setLoading] = useState<string | null>(null);
  const [messages, setMessages] = useState<Record<string, { text: string; ok: boolean }>>({});
  const [history, setHistory] = useState<LogEntry[]>([]);
  const [newCode, setNewCode] = useState("");
  const [syncAll, setSyncAll] = useState(false);
  const [pulseProgress, setPulseProgress] = useState<{ processed: number; total: number } | null>(null);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const historyRef = useRef(history);
  historyRef.current = history;

  useEffect(() => {
    api.getCompetitions().then((r) => { if (r.success && r.data) setCompetitions(r.data); });
  }, []);

  const log = (action: string, ok: boolean, message: string) => {
    const entry: LogEntry = { action, ok, message, time: new Date().toLocaleTimeString() };
    setHistory((h) => [entry, ...h].slice(0, 10));
  };

  const run = async (key: string, label: string, fn: () => Promise<{ success: boolean; message?: string }>) => {
    setLoading(key);
    try {
      const r = await fn();
      const msg = r.message ?? (r.success ? "Done" : "Failed");
      setMessages((m) => ({ ...m, [key]: { text: msg, ok: r.success } }));
      log(label, r.success, msg);
      if (r.success) add(msg, "success"); else add(msg, "error");
    } catch (e) {
      const msg = String(e);
      setMessages((m) => ({ ...m, [key]: { text: msg, ok: false } }));
      log(label, false, msg);
    }
    setLoading(null);
  };

  const APP = "DATA SYNC";

  const toggleExpand = (code: string) => setExpanded((s) => { const n = new Set(s); n.has(code) ? n.delete(code) : n.add(code); return n; });

  const syncPulseEvents = async (c: CompetitionResult) => {
    const key = `${c.code}-pulse`;
    setLoading(key);
    setPulseProgress({ processed: 0, total: 0 });
    let totalEvents = 0;
    try {
      while (true) {
        const r = await api.syncPulseEvents({ competitionCode: c.code, batchSize: 50, appName: APP });
        if (!r.success) { setMessages((m) => ({ ...m, [key]: { text: r.message ?? "Failed", ok: false } })); break; }
        const d = r.data!;
        if (!d) break;
        setPulseProgress((p) => ({ processed: (p?.processed ?? 0) + d.processed, total: d.total }));
        totalEvents += d.eventsAdded;
        if (d.complete || d.processed === 0) break;
      }
      const msg = `Synced ${totalEvents} Pulse events for ${c.name}`;
      setMessages((m) => ({ ...m, [key]: { text: msg, ok: true } }));
      log("Sync Pulse", true, msg);
    } catch (e) {
      setMessages((m) => ({ ...m, [key]: { text: String(e), ok: false } }));
    }
    setPulseProgress(null);
    setLoading(null);
  };

  const importNew = async () => {
    if (!newCode.trim()) return;
    const code = newCode.trim().toUpperCase();
    setNewCode("");
    await run(`new-${code}`, `Import ${code}`, () => api.syncCompetition({ competitionCode: code, appName: APP }));
    const r = await api.getCompetitions();
    if (r.success && r.data) setCompetitions(r.data);
    const comp = r.data?.find((c) => c.code === code);
    if (comp) {
      await run(`${code}-teams`, `Sync Teams ${code}`, () => api.syncTeams({ competitionCode: code, appName: APP }));
      await run(`${code}-matches`, `Sync Matches ${code}`, () => api.syncMatches({ competitionCode: code, appName: APP }));
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-4">
      <h1 className="text-2xl font-bold mb-1">Data Sync</h1>
      <p className="text-sm text-[var(--sc-text-secondary)] mb-6">Manage live data, competition imports, and prediction scoring</p>

      {/* Quick Actions */}
      <p className="text-xs font-bold uppercase tracking-widest text-[var(--sc-text-secondary)] mb-2">Quick Actions</p>
      <div className="flex gap-3 flex-wrap mb-6">
        {[
          { key: "live", label: "⚡ Enhance Live", desc: "Pull live scores, goals, cards, subs", color: "#2E7D32", fn: () => api.enhanceLiveMatches() },
          { key: "pts", label: "🧮 Calculate Points", desc: "Score pending predictions", color: "#FF6B35", fn: async () => {
            for (const c of competitions) {
              const s = await api.getSeasons(c.code);
              const cur = s.data?.find((x) => x.isCurrent);
              if (cur) await api.calculateOutcomes({ seasonId: cur.id });
            }
            return { success: true, message: "Points calculated" };
          }},
        ].map((a) => (
          <div key={a.key} className="flex-1 min-w-[260px] bg-[var(--sc-surface)] rounded-xl p-4 shadow-sm" style={{ borderTop: `3px solid ${a.color}` }}>
            <p className="font-bold text-sm mb-1">{a.label}</p>
            <p className="text-xs text-[var(--sc-text-secondary)] mb-3">{a.desc}</p>
            <button onClick={() => run(a.key, a.label, a.fn)} disabled={loading === a.key}
              className="w-full py-1.5 rounded-lg text-sm font-bold text-white disabled:opacity-40" style={{ background: a.color }}>
              {loading === a.key ? "Running..." : a.label}
            </button>
            {messages[a.key] && <p className={`text-xs mt-2 ${messages[a.key].ok ? "text-green-600" : "text-red-500"}`}>{messages[a.key].text}</p>}
          </div>
        ))}
      </div>

      {/* Add Competition */}
      <p className="text-xs font-bold uppercase tracking-widest text-[var(--sc-text-secondary)] mb-2">Add Competition</p>
      <div className="bg-[var(--sc-surface)] rounded-xl p-4 mb-6 shadow-sm">
        <p className="text-xs text-[var(--sc-text-secondary)] mb-2">Enter a football-data.org competition code to import (runs Steps 1–3 automatically).</p>
        <div className="flex gap-3">
          <input value={newCode} onChange={(e) => setNewCode(e.target.value)} placeholder="e.g. PL, BL1, SA, PD..."
            className="flex-1 max-w-[200px] px-3 py-2 rounded-lg border border-[var(--sc-border)] text-sm" />
          <button onClick={importNew} disabled={!newCode.trim() || !!loading}
            className="px-4 py-2 rounded-lg bg-[var(--sc-tertiary)] text-white text-sm font-bold disabled:opacity-40">Import</button>
        </div>
      </div>

      {/* Per-competition */}
      <p className="text-xs font-bold uppercase tracking-widest text-[var(--sc-text-secondary)] mb-2">Competition Data</p>
      {competitions.length === 0 && <p className="text-[var(--sc-text-secondary)]">No competitions found.</p>}
      <div className="space-y-2 mb-6">
        {competitions.map((c) => {
          const isExpanded = expanded.has(c.code);
          const steps: { key: string; label: string; fn: () => Promise<{ success: boolean; message?: string }> }[] = [
            { key: `${c.code}-comp`, label: "Sync Competition", fn: () => api.syncCompetition({ competitionCode: c.code, appName: APP }) },
            { key: `${c.code}-teams`, label: "Sync Teams", fn: () => api.syncTeams({ competitionCode: c.code, appName: APP }) },
            { key: `${c.code}-matches`, label: "Sync Matches", fn: () => api.syncMatches({ competitionCode: c.code, syncAll: syncAll, appName: APP }) },
            { key: `${c.code}-fpl`, label: "Sync FPL", fn: () => api.syncFplData({ competitionCode: c.code, appName: APP }) },
          ];
          return (
            <div key={c.code} className="bg-[var(--sc-surface)] rounded-xl shadow-sm overflow-hidden">
              <button onClick={() => toggleExpand(c.code)} className="w-full flex items-center gap-2 p-4 text-left">
                {c.logoUrl && <img src={c.logoUrl} alt="" className="w-5 h-5 object-contain" />}
                <span className="font-bold flex-1">{c.name}</span>
                <span className="text-xs text-[var(--sc-text-secondary)] border border-[var(--sc-border)] rounded px-1">{c.code}</span>
                <span className="text-xs">{isExpanded ? "▲" : "▼"}</span>
              </button>
              {isExpanded && (
                <div className="px-4 pb-4 space-y-2">
                  <label className="flex items-center gap-2 text-xs text-[var(--sc-text-secondary)]">
                    <input type="checkbox" checked={syncAll} onChange={(e) => setSyncAll(e.target.checked)} />
                    Sync all seasons
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {steps.map((s) => (
                      <button key={s.key} disabled={loading === s.key}
                        className="px-3 py-1.5 text-xs font-bold rounded-lg bg-gray-100 hover:bg-gray-200 disabled:opacity-40"
                        onClick={() => run(s.key, `${s.label} (${c.code})`, s.fn)}>
                        {loading === s.key ? "..." : s.label}
                      </button>
                    ))}
                    <button disabled={!!loading}
                      className="px-3 py-1.5 text-xs font-bold rounded-lg bg-gray-100 hover:bg-gray-200 disabled:opacity-40"
                      onClick={() => syncPulseEvents(c)}>
                      {loading === `${c.code}-pulse` ? `Pulse (${pulseProgress?.processed ?? 0}/${pulseProgress?.total ?? "?"})` : "Sync Pulse Events"}
                    </button>
                  </div>
                  {Object.entries(messages).filter(([k]) => k.startsWith(c.code)).map(([k, v]) => (
                    <p key={k} className={`text-xs ${v.ok ? "text-green-600" : "text-red-500"}`}>{v.text}</p>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* History */}
      {history.length > 0 && (
        <div>
          <p className="text-xs font-bold uppercase tracking-widest text-[var(--sc-text-secondary)] mb-2">Recent Actions</p>
          <div className="bg-[var(--sc-surface)] rounded-xl shadow-sm divide-y divide-[var(--sc-border)]">
            {history.map((h, i) => (
              <div key={i} className="px-4 py-2 flex items-center gap-2 text-xs">
                <span className={h.ok ? "text-green-600" : "text-red-500"}>{h.ok ? "✓" : "✗"}</span>
                <span className="font-semibold flex-1">{h.action}</span>
                <span className="text-[var(--sc-text-secondary)] truncate max-w-[200px]">{h.message}</span>
                <span className="text-[var(--sc-text-secondary)]">{h.time}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
