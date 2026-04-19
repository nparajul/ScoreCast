"use client";

import { api } from "@/lib/api";
import type { PlayerProfileResult, GameweekMatchesResult, MyPredictionResult, LeagueStandingsResult } from "@/lib/types";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function PlayerProfilePage() {
  const { leagueId, userId } = useParams<{ leagueId: string; userId: string }>();
  const lid = Number(leagueId), uid = Number(userId);
  const [profile, setProfile] = useState<PlayerProfileResult | null>(null);
  const [standings, setStandings] = useState<LeagueStandingsResult | null>(null);
  const [gw, setGw] = useState<GameweekMatchesResult | null>(null);
  const [preds, setPreds] = useState<Map<number, MyPredictionResult>>(new Map());
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    Promise.all([api.getPlayerProfile(uid, lid), api.getLeagueStandings(lid)]).then(([p, s]) => {
      if (p.success && p.data) setProfile(p.data);
      if (s.success && s.data) setStandings(s.data);
    });
  }, [uid, lid]);

  const loadGw = useCallback(async (gwNum: number) => {
    if (!standings) return;
    const res = await api.getGameweekMatches(standings.seasonId, gwNum);
    if (!res.success || !res.data) return;
    setGw(res.data);
    const gwRes = await api.getPlayerGameweek(uid, lid, standings.seasonId, res.data.gameweekId);
    if (gwRes.success && gwRes.data) {
      setVisible(gwRes.data.predictionsVisible);
      const m = new Map<number, MyPredictionResult>();
      if (gwRes.data.predictionsVisible) gwRes.data.predictions.forEach((p) => m.set(p.matchId, p));
      setPreds(m);
    }
  }, [uid, lid, standings]);

  useEffect(() => { if (standings) loadGw(0); }, [standings, loadGw]);

  if (!profile) return <div className="py-8 text-center opacity-50">Loading…</div>;

  const statItems = [
    { label: "Total Pts", value: profile.totalPoints },
    { label: "Best GW", value: profile.bestGameweek },
    { label: "Avg / GW", value: profile.averagePointsPerGameweek?.toFixed(1) ?? "-" },
    { label: "Exact Scores", value: profile.exactScores },
    { label: "Correct Results", value: profile.correctResults },
    { label: "MW Played", value: profile.matchweeksPlayed },
  ];

  return (
    <div className="py-4 max-w-2xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <div className="w-14 h-14 rounded-full bg-white/10 flex items-center justify-center text-2xl overflow-hidden shrink-0">
          {profile.avatarUrl ? <img src={profile.avatarUrl} alt="" className="w-full h-full object-cover" /> : "👤"}
        </div>
        <div>
          <h1 className="text-xl font-bold">{profile.displayName}</h1>
          {profile.favoriteTeam && <p className="text-sm opacity-60">❤️ {profile.favoriteTeam}</p>}
          {standings && <p className="text-xs opacity-40">{standings.leagueName} · {standings.competitionName}</p>}
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 sm:grid-cols-6 gap-2 mb-6">
        {statItems.map((s) => (
          <div key={s.label} className="rounded-lg border border-white/10 p-2.5 text-center">
            <div className="text-xl font-extrabold">{s.value}</div>
            <div className="text-[11px] opacity-50">{s.label}</div>
          </div>
        ))}
      </div>

      {/* Gameweek Nav */}
      {gw && (
        <>
          <h2 className="font-bold mb-2">Predictions</h2>
          <div className="flex items-center justify-center gap-4 mb-3">
            <button disabled={gw.gameweekNumber <= 1} onClick={() => loadGw(gw.gameweekNumber - 1)} className="text-lg disabled:opacity-30">‹</button>
            <span className="text-lg font-bold">Gameweek {gw.gameweekNumber}</span>
            <button disabled={gw.gameweekNumber >= gw.totalGameweeks} onClick={() => loadGw(gw.gameweekNumber + 1)} className="text-lg disabled:opacity-30">›</button>
          </div>

          {!visible ? (
            <div className="text-sm opacity-50 text-center py-4">🔒 Predictions are hidden until matches kick off.</div>
          ) : (
            <div className="rounded-lg border border-white/10 overflow-hidden divide-y divide-white/10">
              {gw.matches.map((m) => {
                const p = preds.get(m.matchId);
                return (
                  <div key={m.matchId} className="px-2 py-2">
                    <div className="flex items-center">
                      <div className="flex-1 flex items-center justify-end gap-1 min-w-0">
                        <span className="text-xs font-semibold truncate text-right">{m.homeTeamShortName || m.homeTeamName}</span>
                        {m.homeTeamLogoUrl && <img src={m.homeTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                      </div>
                      <div className="px-2 text-center w-20 shrink-0">
                        {p ? <span className="text-sm font-bold">{p.predictedHomeScore} - {p.predictedAwayScore}</span> : <span className="text-xs opacity-30">—</span>}
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
          )}
        </>
      )}
    </div>
  );
}
