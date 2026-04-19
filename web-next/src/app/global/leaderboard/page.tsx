"use client";

import { api } from "@/lib/api";
import type { GlobalLeaderboardEntry } from "@/lib/types";
import { useEffect, useState } from "react";

const medal = (r: number) => r === 1 ? "🥇" : r === 2 ? "🥈" : r === 3 ? "🥉" : `#${r}`;

export default function GlobalLeaderboardPage() {
  const [entries, setEntries] = useState<GlobalLeaderboardEntry[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalLeaderboard().then((r) => {
      if (r.success && r.data) setEntries(r.data.entries);
      setLoaded(true);
    });
  }, []);

  return (
    <div className="py-4 px-2">
      <h1 className="text-xl font-extrabold mb-3">🏆 Top Predictors</h1>
      {loaded && entries.length === 0 && <p className="text-sm opacity-50">No predictions yet.</p>}
      <div className="rounded-xl border border-white/10 overflow-hidden divide-y divide-white/10">
        {entries.map((e) => (
          <div key={e.rank} className="flex items-center px-3 py-3" style={e.rank <= 3 ? { background: `linear-gradient(135deg, rgba(${e.rank === 1 ? "255,215,0" : e.rank === 2 ? "192,192,192" : "205,127,50"},0.1), transparent)` } : {}}>
            <span className="w-10 font-bold text-base">{medal(e.rank)}</span>
            <div className="flex-1 min-w-0">
              <div className="font-semibold text-sm">{e.username}</div>
              <div className="text-[11px] opacity-40">{e.exactScores} exact · {e.totalPredictions} predictions</div>
            </div>
            <div className="text-right">
              <span className="font-extrabold" style={{ color: "var(--sc-tertiary)" }}>{e.totalPoints}</span>
              <span className="text-[11px] opacity-40 ml-0.5">pts</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
