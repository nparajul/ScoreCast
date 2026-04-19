"use client";

import { api } from "@/lib/api";
import type { CommunityStats } from "@/lib/types";
import { useEffect, useState } from "react";

export default function GlobalStatsPage() {
  const [stats, setStats] = useState<CommunityStats | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data) setStats(r.data.community);
      setLoaded(true);
    });
  }, []);

  if (loaded && !stats) return <div className="py-8 text-center text-sm opacity-50">Could not load stats.</div>;
  if (!stats) return <div className="py-8 text-center opacity-50">Loading…</div>;

  return (
    <div className="py-4 px-2 space-y-3">
      <h1 className="text-xl font-extrabold">📊 Season Stats</h1>

      <div className="flex gap-2">
        <div className="flex-1 rounded-xl border border-white/10 p-4 text-center">
          <div className="text-3xl font-extrabold">{stats.totalPredictors}</div>
          <div className="text-[11px] opacity-40">Predictors</div>
        </div>
        <div className="flex-1 rounded-xl border border-white/10 p-4 text-center">
          <div className="text-3xl font-extrabold">{stats.totalPredictions}</div>
          <div className="text-[11px] opacity-40">Predictions</div>
        </div>
      </div>

      <div className="flex gap-2">
        <div className="flex-1 rounded-xl border border-white/10 p-4 text-center">
          <div className="text-3xl font-extrabold" style={{ color: "#4CAF50" }}>{stats.exactScores}</div>
          <div className="text-[11px] opacity-40">Exact Scores</div>
        </div>
        <div className="flex-1 rounded-xl border border-white/10 p-4 text-center">
          <div className="text-3xl font-extrabold" style={{ color: "var(--sc-tertiary)" }}>{stats.exactScorePct}%</div>
          <div className="text-[11px] opacity-40">Exact Score Rate</div>
        </div>
      </div>

      {stats.hardestMatch !== "N/A" && (
        <div className="rounded-xl border border-white/10 p-4">
          <div className="text-[10px] tracking-widest opacity-40">HARDEST TO PREDICT</div>
          <div className="text-lg font-bold mt-1">{stats.hardestMatch}</div>
          <div className="text-sm font-semibold" style={{ color: "#f44336" }}>
            {stats.hardestMatchAccuracy === 0 ? "Nobody got it right!" : `Only ${stats.hardestMatchAccuracy}% got it right`}
          </div>
        </div>
      )}

      {stats.mostPredictableTeam !== "N/A" && (
        <div className="rounded-xl border border-white/10 p-4">
          <div className="text-[10px] tracking-widest opacity-40">MOST PREDICTABLE TEAM</div>
          <div className="text-lg font-bold mt-1">{stats.mostPredictableTeam}</div>
          <div className="text-sm font-semibold" style={{ color: "#4CAF50" }}>{stats.mostPredictableTeamPct}% correct result rate</div>
        </div>
      )}
    </div>
  );
}
