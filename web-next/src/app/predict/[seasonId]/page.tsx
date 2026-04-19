"use client";

import { api } from "@/lib/api";
import type { GameweekMatchesResult, MatchDetail, MyPredictionResult } from "@/lib/types";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

interface PredMatch extends MatchDetail {
  predHome: number;
  predAway: number;
  hasSaved: boolean;
  outcome?: string;
}

export default function PredictPage() {
  const { seasonId } = useParams<{ seasonId: string }>();
  const sid = Number(seasonId);
  const [gw, setGw] = useState<GameweekMatchesResult | null>(null);
  const [matches, setMatches] = useState<PredMatch[]>([]);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async (gwNum: number) => {
    const res = await api.getGameweekMatches(sid, gwNum);
    if (!res.success || !res.data) return;
    const g = res.data;
    setGw(g);
    const preds = await api.getMyPredictions(sid, g.gameweekId);
    const predMap = new Map<number, MyPredictionResult>();
    if (preds.success && preds.data) preds.data.forEach((p) => predMap.set(p.matchId, p));
    setMatches(
      g.matches.map((m) => {
        const p = predMap.get(m.matchId);
        return { ...m, predHome: p?.predictedHomeScore ?? 0, predAway: p?.predictedAwayScore ?? 0, hasSaved: !!p, outcome: p?.outcome ?? undefined };
      })
    );
  }, [sid]);

  useEffect(() => { load(0); }, [load]);

  const isLocked = (m: PredMatch) => m.status === "Finished" || m.status === "InPlay" || (m.kickoffTime && new Date(m.kickoffTime) <= new Date());

  const update = (idx: number, home: boolean, delta: number) => {
    setMatches((prev) => prev.map((m, i) => {
      if (i !== idx || isLocked(m)) return m;
      const key = home ? "predHome" : "predAway";
      return { ...m, [key]: Math.max(0, m[key] + delta) };
    }));
  };

  const save = async () => {
    if (!gw) return;
    setSaving(true);
    const predictions = matches.filter((m) => !isLocked(m)).map((m) => ({ matchId: m.matchId, predictedHomeScore: m.predHome, predictedAwayScore: m.predAway }));
    await api.submitPredictions({ seasonId: sid, predictions });
    setSaving(false);
    setMatches((prev) => prev.map((m) => (isLocked(m) ? m : { ...m, hasSaved: true })));
  };

  const deadline = gw?.matches.filter((m) => m.kickoffTime && new Date(m.kickoffTime) > new Date()).sort((a, b) => new Date(a.kickoffTime!).getTime() - new Date(b.kickoffTime!).getTime())[0]?.kickoffTime;

  return (
    <div className="py-4 max-w-xl mx-auto">
      <h1 className="text-xl font-bold mb-1">Make Predictions</h1>
      <p className="text-sm opacity-50 mb-4">Predict the score for each match</p>

      {gw && (
        <>
          <div className="flex items-center justify-center gap-4 mb-4">
            <button disabled={gw.gameweekNumber <= 1} onClick={() => load(gw.gameweekNumber - 1)} className="text-lg disabled:opacity-30">‹</button>
            <span className="text-lg font-bold">Gameweek {gw.gameweekNumber}</span>
            <button disabled={gw.gameweekNumber >= gw.totalGameweeks} onClick={() => load(gw.gameweekNumber + 1)} className="text-lg disabled:opacity-30">›</button>
          </div>

          {deadline && (
            <div className="text-center text-xs mb-3 opacity-60">
              Next kickoff: {new Date(deadline).toLocaleString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })}
            </div>
          )}

          <div className="rounded-lg border border-white/10 overflow-hidden divide-y divide-white/10">
            {matches.map((m, i) => {
              const locked = isLocked(m);
              return (
                <div key={m.matchId} className="px-2 py-2.5">
                  <div className="flex items-center">
                    <div className="flex-1 flex items-center justify-end gap-1 min-w-0">
                      <span className="text-xs font-semibold truncate text-right">{m.homeTeamShortName || m.homeTeamName}</span>
                      {m.homeTeamLogoUrl && <img src={m.homeTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                    </div>
                    <div className="flex items-center gap-1 px-1 shrink-0">
                      {locked ? (
                        <span className="text-sm font-bold opacity-50 w-20 text-center">{m.predHome} - {m.predAway}</span>
                      ) : (
                        <>
                          <div className="flex flex-col items-center w-9 rounded-lg border border-white/10 overflow-hidden">
                            <button onClick={() => update(i, true, 1)} className="w-full h-5 text-xs font-bold bg-green-600 text-white">+</button>
                            <div className="text-base font-bold py-0.5 w-full text-center">{m.predHome}</div>
                            <button onClick={() => update(i, true, -1)} className="w-full h-5 text-xs font-bold bg-red-600 text-white">−</button>
                          </div>
                          <span className="text-xs font-bold">-</span>
                          <div className="flex flex-col items-center w-9 rounded-lg border border-white/10 overflow-hidden">
                            <button onClick={() => update(i, false, 1)} className="w-full h-5 text-xs font-bold bg-green-600 text-white">+</button>
                            <div className="text-base font-bold py-0.5 w-full text-center">{m.predAway}</div>
                            <button onClick={() => update(i, false, -1)} className="w-full h-5 text-xs font-bold bg-red-600 text-white">−</button>
                          </div>
                        </>
                      )}
                    </div>
                    <div className="flex-1 flex items-center gap-1 min-w-0">
                      {m.awayTeamLogoUrl && <img src={m.awayTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                      <span className="text-xs font-semibold truncate">{m.awayTeamShortName || m.awayTeamName}</span>
                    </div>
                  </div>
                  <div className="text-center text-[11px] opacity-50 mt-1">
                    {m.status === "Finished" ? `FT ${m.homeScore}-${m.awayScore}` : m.kickoffTime ? new Date(m.kickoffTime).toLocaleString(undefined, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" }) : ""}
                  </div>
                </div>
              );
            })}
          </div>

          {matches.some((m) => !isLocked(m)) && (
            <div className="flex justify-center mt-4">
              <button onClick={save} disabled={saving} className="px-6 py-2 rounded-full text-sm font-bold text-white disabled:opacity-50" style={{ background: "var(--sc-tertiary)" }}>
                {saving ? "Saving…" : "Save Predictions"}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
