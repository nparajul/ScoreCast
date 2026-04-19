"use client";

import { useAuth } from "@/contexts/auth-context";
import { api } from "@/lib/api";
import type { UserSeasonResult, PredictionLeagueResult, MyPredictionStatsResult, PredictionReplayResult } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function DashboardPage() {
  const { user } = useAuth();
  const [seasons, setSeasons] = useState<UserSeasonResult[]>([]);
  const [leagues, setLeagues] = useState<PredictionLeagueResult[]>([]);
  const [stats, setStats] = useState<MyPredictionStatsResult | null>(null);
  const [replay, setReplay] = useState<PredictionReplayResult | null>(null);
  const [reordering, setReordering] = useState(false);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    Promise.all([
      api.getUserSeasons(),
      api.getMyLeagues(),
      api.getMyPredictionStats(),
    ]).then(([s, l, st]) => {
      if (s.success && s.data) setSeasons(s.data);
      if (l.success && l.data) setLeagues(l.data);
      if (st.success && st.data) setStats(st.data);
      setLoaded(true);
    });
  }, []);

  const move = async (from: number, to: number) => {
    const arr = [...seasons];
    const [item] = arr.splice(from, 1);
    arr.splice(to, 0, item);
    setSeasons(arr);
    await api.reorderUserSeasons(arr.map((s) => s.seasonId));
  };

  const gw = stats?.lastGameweek;

  return (
    <div className="py-4 max-w-2xl mx-auto space-y-6">
      {/* Hero / Stats Card */}
      {gw && stats && stats.totalPredictions > 0 && (
        <div className="rounded-xl p-5 text-center text-white" style={{ background: "linear-gradient(135deg, var(--sc-primary), #1a2d45)" }}>
          <div className="text-xs tracking-widest opacity-70 mb-1">GAMEWEEK {gw.gameweekNumber}</div>
          <div className="flex justify-around my-3">
            <div><span className="text-2xl font-extrabold">{gw.userCorrect}/{gw.userTotal}</span><br /><span className="text-xs opacity-60">You</span></div>
            <div><span className="text-2xl font-extrabold">{gw.beatPct}<span className="text-sm">%</span></span><br /><span className="text-xs opacity-60">Beaten →</span></div>
            <div><span className="text-2xl font-extrabold">{Math.round(gw.communityAvgCorrect)}/{Math.round(gw.communityAvgTotal)}</span><br /><span className="text-xs opacity-60">Average</span></div>
          </div>
          {stats.currentStreak >= 2 && <div className="text-sm">🔥 {stats.currentStreak}-match streak</div>}
          {seasons.length > 0 && (
            <Link href={`/predict/${seasons[0].seasonId}`} className="inline-block mt-3 px-5 py-2 rounded-full font-bold text-sm" style={{ background: "var(--sc-tertiary)" }}>
              ⚽ Predict Now
            </Link>
          )}
        </div>
      )}

      {/* Last Replay */}
      {replay && (
        <div className="rounded-xl border border-white/10 p-4">
          <div className="text-sm font-bold mb-2">📽️ Last Prediction</div>
          <div className="flex items-center justify-center gap-3 mb-2">
            {replay.homeLogo && <img src={replay.homeLogo} alt="" className="w-6 h-6" />}
            <span className="text-lg font-extrabold">{replay.homeScore} – {replay.awayScore}</span>
            {replay.awayLogo && <img src={replay.awayLogo} alt="" className="w-6 h-6" />}
          </div>
          <div className="text-center text-xs opacity-60">You predicted <strong>{replay.predictedHome} – {replay.predictedAway}</strong></div>
          {replay.deathMinute && <div className="text-center text-xs text-red-600 mt-1">💀 Died at minute {replay.deathMinute}</div>}
          {replay.outcome === "ExactScore" && <div className="text-center text-xs text-green-600 font-semibold mt-1">🎯 Exact score!</div>}
          {replay.aiCommentary && <div className="mt-2 text-xs italic opacity-80">🎙️ {replay.aiCommentary}</div>}
        </div>
      )}

      {/* Achievements */}
      {stats && stats.achievements.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {stats.achievements.map((a) => <span key={a} className="px-3 py-1 rounded-full text-xs font-semibold" style={{ background: "rgba(255,107,53,0.15)", color: "var(--sc-tertiary)" }}>{a}</span>)}
        </div>
      )}

      {/* Competitions */}
      <section>
        <div className="flex items-center justify-between mb-2">
          <span className="font-bold">Competitions</span>
          {seasons.length > 1 && (
            <button onClick={() => setReordering(!reordering)} className="text-xs font-semibold" style={{ color: "var(--sc-tertiary)" }}>
              {reordering ? "✓ Done" : "Reorder"}
            </button>
          )}
        </div>
        <div className="rounded-xl border border-white/10 overflow-hidden divide-y divide-white/10">
          {seasons.map((s, i) => (
            <div key={s.seasonId} className="flex items-center gap-3 px-4 py-3">
              {reordering && (
                <div className="flex flex-col gap-0.5">
                  <button disabled={i === 0} onClick={() => move(i, i - 1)} className="text-xs disabled:opacity-30">▲</button>
                  <button disabled={i === seasons.length - 1} onClick={() => move(i, i + 1)} className="text-xs disabled:opacity-30">▼</button>
                </div>
              )}
              {s.competitionLogoUrl && <img src={s.competitionLogoUrl} alt="" className="w-8 h-8 object-contain" />}
              <Link href={`/predict/${s.seasonId}`} className="flex-1 min-w-0">
                <div className="font-semibold truncate">{s.competitionName}</div>
                <div className="text-xs opacity-50">{s.seasonName} Season</div>
              </Link>
              {!reordering && <span className="opacity-30">›</span>}
            </div>
          ))}
          {!reordering && (
            <Link href="#" className="flex items-center gap-3 px-4 py-3" style={{ color: "var(--sc-tertiary)" }}>
              <span className="text-xl">+</span>
              <span className="font-semibold">Add Competition</span>
            </Link>
          )}
        </div>
      </section>

      {/* Leagues */}
      <section>
        <div className="flex items-center justify-between mb-2">
          <span className="font-bold">My Leagues</span>
        </div>
        {loaded && leagues.length === 0 ? (
          <p className="text-sm opacity-50">Create a league or join with an invite code</p>
        ) : (
          <div className="rounded-xl border border-white/10 overflow-hidden divide-y divide-white/10">
            {leagues.map((l) => (
              <Link key={l.id} href={`/dashboard/${l.id}`} className="flex items-center gap-3 px-4 py-3">
                <div className="flex-1 min-w-0">
                  <div className="font-semibold truncate">{l.name}</div>
                  <div className="text-xs opacity-50">{l.competitionName} · {l.memberCount} members</div>
                </div>
                <span className="text-xs font-mono opacity-40">{l.inviteCode}</span>
                <span className="opacity-30">›</span>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
