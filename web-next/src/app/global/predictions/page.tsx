"use client";

import { api } from "@/lib/api";
import type { MatchPredictionSummary } from "@/lib/types";
import { useEffect, useState } from "react";

export default function GlobalPredictionsPage() {
  const [predictions, setPredictions] = useState<MatchPredictionSummary[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data) setPredictions(r.data.upcomingPredictions);
      setLoaded(true);
    });
  }, []);

  return (
    <div className="py-4 px-2 space-y-2">
      <h1 className="text-xl font-extrabold mb-3">🔮 Community Predictions</h1>
      {loaded && predictions.length === 0 && <p className="text-sm opacity-50">No upcoming predictions right now — check back closer to matchday!</p>}
      {predictions.map((m) => (
        <div key={m.matchId} className="rounded-xl border border-white/10 p-3">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2 flex-1">
              {m.homeTeamCrest && <img src={m.homeTeamCrest} alt="" className="w-5 h-5 object-contain" />}
              <span className="text-sm font-semibold">{m.homeTeamShortName || m.homeTeam}</span>
            </div>
            <span className="text-xs opacity-40">vs</span>
            <div className="flex items-center gap-2 flex-1 justify-end">
              <span className="text-sm font-semibold">{m.awayTeamShortName || m.awayTeam}</span>
              {m.awayTeamCrest && <img src={m.awayTeamCrest} alt="" className="w-5 h-5 object-contain" />}
            </div>
          </div>
          {m.predictionCount > 0 ? (
            <>
              <div className="flex gap-0.5 h-1.5 rounded-full overflow-hidden mb-1">
                <div style={{ flex: m.homePct, background: "#4CAF50" }} className="rounded-full" />
                <div style={{ flex: m.drawPct, background: "#9E9E9E" }} className="rounded-full" />
                <div style={{ flex: m.awayPct, background: "#2196F3" }} className="rounded-full" />
              </div>
              <div className="flex justify-between text-[11px]">
                <span style={{ color: "#4CAF50" }} className="font-semibold">{m.homePct}%</span>
                <span className="opacity-40">{m.drawPct}% draw</span>
                <span style={{ color: "#2196F3" }} className="font-semibold">{m.awayPct}%</span>
              </div>
              <p className="text-[11px] opacity-40 text-center mt-1">
                Most predicted: <strong className="opacity-80">{m.mostPredictedScore}</strong> ({m.mostPredictedPct}%) · {m.predictionCount} predictions
              </p>
            </>
          ) : (
            <p className="text-[11px] opacity-30 text-center">No predictions yet — be the first!</p>
          )}
        </div>
      ))}
    </div>
  );
}
