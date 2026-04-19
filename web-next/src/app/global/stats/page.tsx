"use client";

import { api } from "@/lib/api";
import type { CommunityStats } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function GlobalStatsPage() {
  const [stats, setStats] = useState<CommunityStats | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data) setStats(r.data.community ?? r.data.stats ?? null);
      setLoaded(true);
    });
  }, []);

  if (loaded && !stats) return <div className="py-8 text-center text-sm text-[var(--sc-text-secondary)]">Could not load stats.</div>;
  if (!stats) return <div className="flex justify-center p-8"><div className="text-4xl animate-pulse">⚽</div></div>;

  return (
    <div className="py-4 px-2 space-y-3">
      <Link href="/global" className="text-sm text-[var(--sc-secondary)] font-semibold">← Back</Link>
      <h1 className="text-xl font-extrabold">📊 Season Stats</h1>

      <div className="flex gap-2">
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-4 text-center shadow-sm">
          <p className="text-3xl font-extrabold">{stats.totalPredictors}</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Predictors</p>
        </div>
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-4 text-center shadow-sm">
          <p className="text-3xl font-extrabold">{stats.totalPredictions}</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Predictions</p>
        </div>
      </div>

      <div className="flex gap-2">
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-4 text-center shadow-sm">
          <p className="text-3xl font-extrabold text-green-600">{stats.exactScores}</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Exact Scores</p>
        </div>
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-4 text-center shadow-sm">
          <p className="text-3xl font-extrabold text-[var(--sc-tertiary)]">{stats.exactScorePct ?? stats.exactScoreRate}%</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Exact Score Rate</p>
        </div>
      </div>

      {stats.hardestMatch && stats.hardestMatch !== "N/A" && (
        <div className="bg-[var(--sc-surface)] rounded-xl p-4 shadow-sm">
          <p className="text-[10px] tracking-widest text-[var(--sc-text-secondary)]">HARDEST TO PREDICT</p>
          <p className="text-lg font-bold mt-1">{stats.hardestMatch}</p>
          <p className="text-sm font-semibold text-red-500">
            {stats.hardestMatchAccuracy === 0 ? "Nobody got it right!" : `Only ${stats.hardestMatchAccuracy}% got it right`}
          </p>
        </div>
      )}

      {stats.mostPredictableTeam && stats.mostPredictableTeam !== "N/A" && (
        <div className="bg-[var(--sc-surface)] rounded-xl p-4 shadow-sm">
          <p className="text-[10px] tracking-widest text-[var(--sc-text-secondary)]">MOST PREDICTABLE TEAM</p>
          <p className="text-lg font-bold mt-1">{stats.mostPredictableTeam}</p>
          <p className="text-sm font-semibold text-green-600">{stats.mostPredictableTeamPct}% correct result rate</p>
        </div>
      )}
    </div>
  );
}
