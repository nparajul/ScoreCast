"use client";

import { api } from "@/lib/api";
import { CompetitionResult } from "@/lib/types";
import { useEffect, useState } from "react";

type Action = "syncCompetition" | "syncTeams" | "syncMatches" | "enhanceLive" | "calculatePoints" | "syncFpl" | "syncPulse";

export default function DataSyncPage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [messages, setMessages] = useState<Record<string, { text: string; ok: boolean }>>({});
  const [loading, setLoading] = useState<string | null>(null);

  useEffect(() => {
    api.getCompetitions().then(r => { if (r.success && r.data) setCompetitions(r.data); });
  }, []);

  const run = async (key: string, fn: () => Promise<{ success: boolean; message?: string }>) => {
    setLoading(key);
    try {
      const r = await fn();
      setMessages(m => ({ ...m, [key]: { text: r.message ?? (r.success ? "Done" : "Failed"), ok: r.success } }));
    } catch (e: unknown) {
      setMessages(m => ({ ...m, [key]: { text: String(e), ok: false } }));
    }
    setLoading(null);
  };

  const APP = "DATA SYNC";

  return (
    <div className="max-w-4xl mx-auto p-4 text-white">
      <h1 className="text-2xl font-bold mb-1">Data Sync</h1>
      <p className="text-sm opacity-60 mb-6">Manage live data, competition imports, and prediction scoring</p>

      {/* Quick Actions */}
      <p className="text-xs font-bold uppercase tracking-widest opacity-50 mb-2">Quick Actions</p>
      <div className="flex gap-3 flex-wrap mb-6">
        <ActionCard label="⚡ Enhance Live" desc="Pull live scores, goals, cards, subs"
          color="#2E7D32" loading={loading === "live"} msg={messages["live"]}
          onClick={() => run("live", () => api.enhanceLiveMatches())} />
        <ActionCard label="🧮 Calculate Points" desc="Score pending predictions for finished matches"
          color="#FF6B35" loading={loading === "pts"} msg={messages["pts"]}
          onClick={() => run("pts", async () => {
            for (const c of competitions) {
              const s = await api.getSeasons(c.code);
              const cur = s.data?.find(x => x.isCurrent);
              if (cur) await api.calculateOutcomes({ seasonId: cur.id });
            }
            return { success: true, message: "Points calculated" };
          })} />
      </div>

      {/* Per-competition */}
      <p className="text-xs font-bold uppercase tracking-widest opacity-50 mb-2">Competition Data</p>
      {competitions.length === 0 && <p className="opacity-60">No competitions found.</p>}
      <div className="space-y-3">
        {competitions.map(c => {
          const k = (a: Action) => `${c.code}-${a}`;
          return (
            <div key={c.code} className="bg-white/5 rounded-xl p-4">
              <div className="flex items-center gap-2 mb-3">
                {c.logoUrl && <img src={c.logoUrl} alt="" className="w-5 h-5 object-contain" />}
                <span className="font-bold">{c.name}</span>
                <span className="text-xs opacity-50 border border-white/20 rounded px-1">{c.code}</span>
              </div>
              <div className="flex flex-wrap gap-2">
                {([
                  ["syncCompetition", "Sync Competition"],
                  ["syncTeams", "Sync Teams"],
                  ["syncMatches", "Sync Matches"],
                  ["syncFpl", "Sync FPL"],
                  ["syncPulse", "Sync Pulse Events"],
                ] as [Action, string][]).map(([action, label]) => (
                  <button key={action} disabled={loading === k(action)}
                    className="px-3 py-1.5 text-xs font-bold rounded-lg bg-white/10 hover:bg-white/20 disabled:opacity-40"
                    onClick={() => run(k(action), () => {
                      const body = { competitionCode: c.code, appName: APP };
                      if (action === "syncCompetition") return api.syncCompetition(body);
                      if (action === "syncTeams") return api.syncTeams(body);
                      if (action === "syncMatches") return api.syncMatches(body);
                      if (action === "syncFpl") return api.syncFplData(body);
                      return api.syncPulseEvents({ ...body, batchSize: 50 });
                    })}>
                    {loading === k(action) ? "..." : label}
                  </button>
                ))}
              </div>
              {Object.entries(messages).filter(([mk]) => mk.startsWith(c.code)).map(([mk, mv]) => (
                <p key={mk} className={`text-xs mt-2 ${mv.ok ? "text-green-400" : "text-red-400"}`}>{mv.text}</p>
              ))}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function ActionCard({ label, desc, color, loading, msg, onClick }: {
  label: string; desc: string; color: string; loading: boolean;
  msg?: { text: string; ok: boolean }; onClick: () => void;
}) {
  return (
    <div className="flex-1 min-w-[260px] bg-white/5 rounded-xl p-4" style={{ borderTop: `3px solid ${color}` }}>
      <p className="font-bold text-sm mb-1">{label}</p>
      <p className="text-xs opacity-60 mb-3">{desc}</p>
      <button onClick={onClick} disabled={loading}
        className="w-full py-1.5 rounded-lg text-sm font-bold text-white disabled:opacity-40"
        style={{ background: color }}>
        {loading ? "Running..." : label}
      </button>
      {msg && <p className={`text-xs mt-2 ${msg.ok ? "text-green-400" : "text-red-400"}`}>{msg.text}</p>}
    </div>
  );
}
