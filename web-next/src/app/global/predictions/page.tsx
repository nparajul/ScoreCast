"use client";

import { api } from "@/lib/api";
import type { MatchPredictionSummary } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function GlobalPredictionsPage() {
  const [predictions, setPredictions] = useState<MatchPredictionSummary[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getGlobalDashboard().then((r) => {
      if (r.success && r.data) setPredictions(r.data.upcomingPredictions ?? r.data.matches ?? []);
      setLoaded(true);
    });
  }, []);

  return (
    <div className="py-4 px-2 space-y-2">
      <Link href="/global" className="text-sm text-[var(--sc-secondary)] font-semibold">← Back</Link>
      <h1 className="text-xl font-extrabold mb-3">🔮 Community Predictions</h1>
      {loaded && predictions.length === 0 && <p className="text-sm text-[var(--sc-text-secondary)]">🔮 No upcoming predictions right now — check back closer to matchday!</p>}
      {predictions.map((m) => (
        <Link key={m.matchId} href={`/matches/${m.matchId}`} className="block bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-1.5 flex-1">
              {(m.homeTeamCrest ?? m.homeTeamLogo) && <img src={(m.homeTeamCrest ?? m.homeTeamLogo)!} alt="" className="w-6 h-6 object-contain" />}
              <span className="text-sm font-semibold truncate">{m.homeTeamShortName || m.homeTeam}</span>
            </div>
            <span className="text-xs text-[var(--sc-text-secondary)] mx-2">vs</span>
            <div className="flex items-center gap-1.5 flex-1 justify-end">
              <span className="text-sm font-semibold truncate">{m.awayTeamShortName || m.awayTeam}</span>
              {(m.awayTeamCrest ?? m.awayTeamLogo) && <img src={(m.awayTeamCrest ?? m.awayTeamLogo)!} alt="" className="w-6 h-6 object-contain" />}
            </div>
          </div>
          {m.predictionCount > 0 ? (
            <>
              <div className="flex gap-0.5 h-2 rounded-full overflow-hidden mb-1">
                <div style={{ flex: m.homePct ?? m.homeWinPercent ?? 0 }} className="bg-green-500 rounded-full" />
                <div style={{ flex: m.drawPct ?? m.drawPercent ?? 0 }} className="bg-gray-400 rounded-full" />
                <div style={{ flex: m.awayPct ?? m.awayWinPercent ?? 0 }} className="bg-blue-500 rounded-full" />
              </div>
              <div className="flex justify-between text-[11px]">
                <span className="text-green-600 font-semibold">{m.homePct ?? m.homeWinPercent}%</span>
                <span className="text-[var(--sc-text-secondary)]">{m.drawPct ?? m.drawPercent}% draw</span>
                <span className="text-blue-600 font-semibold">{m.awayPct ?? m.awayWinPercent}%</span>
              </div>
              <p className="text-[11px] text-[var(--sc-text-secondary)] text-center mt-1">
                Most predicted: <strong>{m.mostPredictedScore}</strong> ({m.mostPredictedPct}%) · {m.predictionCount} predictions
              </p>
            </>
          ) : (
            <p className="text-[11px] text-[var(--sc-text-secondary)] text-center">No predictions yet — be the first!</p>
          )}
        </Link>
      ))}
    </div>
  );
}
