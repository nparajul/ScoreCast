"use client";

import { api } from "@/lib/api";
import type { GameweekRecap } from "@/lib/types";
import { useEffect, useState } from "react";

export default function GlobalRecapPage() {
  const [recap, setRecap] = useState<GameweekRecap | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data?.lastGameweekRecap) setRecap(r.data.lastGameweekRecap);
      setLoaded(true);
    });
  }, []);

  if (loaded && !recap) return <div className="py-8 text-center text-sm opacity-50">No completed gameweek yet.</div>;
  if (!recap) return <div className="py-8 text-center opacity-50">Loading…</div>;

  return (
    <div className="py-4 px-2 space-y-3">
      <h1 className="text-xl font-extrabold">📋 Gameweek {recap.gameweekNumber} Recap</h1>

      <div className="rounded-xl p-4" style={{ background: "rgba(255,107,53,0.08)" }}>
        <div className="text-[10px] tracking-widest opacity-40 mb-1">BEST PREDICTOR</div>
        <div className="flex items-center gap-2">
          <span className="text-lg font-bold">👑 {recap.bestPredictor}</span>
          <span className="px-2 py-0.5 rounded-full text-xs font-bold text-white" style={{ background: "var(--sc-tertiary)" }}>{recap.bestPredictorPoints} pts</span>
        </div>
      </div>

      <div className="flex gap-2">
        <div className="flex-1 rounded-xl border border-white/10 p-3 text-center">
          <div className="text-2xl font-extrabold" style={{ color: "#4CAF50" }}>{recap.totalExactScores}</div>
          <div className="text-[11px] opacity-40">Exact Scores</div>
        </div>
        <div className="flex-1 rounded-xl border border-white/10 p-3 text-center">
          <div className="text-2xl font-extrabold" style={{ color: "#2196F3" }}>{recap.totalPredictors}</div>
          <div className="text-[11px] opacity-40">Predictors</div>
        </div>
      </div>

      {recap.biggestUpset && (
        <div className="rounded-xl border border-white/10 p-3">
          <div className="text-[10px] tracking-widest opacity-40">BIGGEST UPSET</div>
          <div className="font-bold mt-1">😱 {recap.biggestUpset}</div>
        </div>
      )}

      {recap.boldestCorrectPrediction && (
        <div className="rounded-xl border border-white/10 p-3">
          <div className="text-[10px] tracking-widest opacity-40">BOLDEST CORRECT CALL</div>
          <div className="font-bold mt-1">🔮 {recap.boldestCorrectPrediction}</div>
        </div>
      )}
    </div>
  );
}
