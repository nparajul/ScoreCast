"use client";

import { api } from "@/lib/api";
import type { GlobalLeaderboardEntry } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";

const medal = (r: number) => r === 1 ? "🥇" : r === 2 ? "🥈" : r === 3 ? "🥉" : `#${r}`;
const rankBg = (r: number) => r === 1 ? "bg-amber-50" : r === 2 ? "bg-gray-50" : r === 3 ? "bg-orange-50" : "";

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
      <Link href="/global" className="text-sm text-[var(--sc-secondary)] font-semibold">← Back</Link>
      <h1 className="text-xl font-extrabold mb-3 mt-2">🏆 Top Predictors</h1>
      {loaded && entries.length === 0 && <p className="text-sm text-[var(--sc-text-secondary)]">No predictions yet.</p>}
      <div className="bg-[var(--sc-surface)] rounded-xl shadow-sm overflow-hidden divide-y divide-[var(--sc-border)]">
        {entries.map((e) => (
          <div key={e.rank} className={`flex items-center px-3 py-3 ${rankBg(e.rank)}`}>
            <span className="w-10 font-bold text-base">{medal(e.rank)}</span>
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-sm">{e.username ?? e.displayName}</p>
              <p className="text-[11px] text-[var(--sc-text-secondary)]">{e.exactScores} exact · {e.totalPredictions ?? e.predictionCount} predictions</p>
            </div>
            <div className="text-right">
              <span className="font-extrabold text-[var(--sc-tertiary)]">{e.totalPoints}</span>
              <span className="text-[11px] text-[var(--sc-text-secondary)] ml-0.5">pts</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
