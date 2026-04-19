"use client";

import { api } from "@/lib/api";
import type { GameweekRecap } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function GlobalRecapPage() {
  const [recap, setRecap] = useState<GameweekRecap | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data) setRecap(r.data.lastGameweekRecap ?? r.data.recap ?? null);
      setLoaded(true);
    });
  }, []);

  if (loaded && !recap) return <div className="py-8 text-center text-sm text-[var(--sc-text-secondary)]">No completed gameweek yet.</div>;
  if (!recap) return <div className="flex justify-center p-8"><div className="text-4xl animate-pulse">⚽</div></div>;

  return (
    <div className="py-4 px-2 space-y-3">
      <Link href="/global" className="text-sm text-[var(--sc-secondary)] font-semibold">← Back</Link>
      <h1 className="text-xl font-extrabold">📋 Gameweek {recap.gameweekNumber} Recap</h1>

      {/* Best Predictor Podium */}
      {recap.bestPredictor && (
        <div className="bg-[var(--sc-surface)] rounded-xl p-4 shadow-sm" style={{ background: "linear-gradient(135deg, rgba(255,107,53,0.08), transparent)" }}>
          <p className="text-[10px] tracking-widest text-[var(--sc-text-secondary)]">BEST PREDICTOR</p>
          <div className="flex items-center gap-2 mt-1">
            <span className="text-lg font-bold">👑 {recap.bestPredictor}</span>
            <span className="px-2 py-0.5 rounded-full text-xs font-bold text-white bg-[var(--sc-tertiary)]">{recap.bestPredictorPoints} pts</span>
          </div>
        </div>
      )}

      {/* Stats Grid */}
      <div className="flex gap-2">
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-3 text-center shadow-sm">
          <p className="text-2xl font-extrabold text-green-600">{recap.totalExactScores ?? recap.exactScores}</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Exact Scores</p>
        </div>
        <div className="flex-1 bg-[var(--sc-surface)] rounded-xl p-3 text-center shadow-sm">
          <p className="text-2xl font-extrabold text-blue-600">{recap.totalPredictors}</p>
          <p className="text-[11px] text-[var(--sc-text-secondary)]">Predictors</p>
        </div>
      </div>

      {recap.biggestUpset && (
        <div className="bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
          <p className="text-[10px] tracking-widest text-[var(--sc-text-secondary)]">BIGGEST UPSET</p>
          <p className="font-bold mt-1">😱 {recap.biggestUpset}</p>
        </div>
      )}

      {(recap.boldestCorrectPrediction ?? recap.boldestCall) && (
        <div className="bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
          <p className="text-[10px] tracking-widest text-[var(--sc-text-secondary)]">BOLDEST CORRECT CALL</p>
          <p className="font-bold mt-1">🔮 {recap.boldestCorrectPrediction ?? recap.boldestCall}</p>
        </div>
      )}
    </div>
  );
}
