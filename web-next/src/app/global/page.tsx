"use client";

import { api } from "@/lib/api";
import { CompetitionResult, GlobalDashboardResult } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState, useRef } from "react";

export default function GlobalPage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [selected, setSelected] = useState<CompetitionResult | null>(null);
  const [data, setData] = useState<GlobalDashboardResult | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [countdown, setCountdown] = useState("");
  const [allLocked, setAllLocked] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  useEffect(() => {
    (async () => {
      const [c, d] = await Promise.all([api.getCompetitions(), api.getDefaultCompetition()]);
      if (c.success && c.data) {
        setCompetitions(c.data);
        const def = c.data.find((x) => x.code === d.data?.code) ?? c.data[0];
        setSelected(def);
        await load(def?.code);
      }
    })();
    return () => clearInterval(timerRef.current);
  }, []);

  const load = async (code?: string) => {
    setLoaded(false);
    const r = await api.getGlobalDashboard(code);
    if (r.success && r.data) setData(r.data);
    setLoaded(true);
  };

  // Countdown timer
  useEffect(() => {
    if (!data?.countdown?.deadline) return;
    const tick = () => {
      const diff = new Date(data.countdown!.deadline!).getTime() - Date.now();
      if (diff <= 0) { setCountdown(data.countdown!.isComplete ? "Gameweek complete ✅" : "Gameweek in progress"); setAllLocked(true); return; }
      setAllLocked(false);
      const d = Math.floor(diff / 86400000), h = Math.floor((diff % 86400000) / 3600000), m = Math.floor((diff % 3600000) / 60000), s = Math.floor((diff % 60000) / 1000);
      setCountdown([d && `${d}d`, (h || d) && `${h}h`, (m || h || d) && `${m}m`, `${s}s`].filter(Boolean).join(" "));
    };
    tick();
    timerRef.current = setInterval(tick, 1000);
    return () => clearInterval(timerRef.current);
  }, [data]);

  const onComp = async (c: CompetitionResult) => { setSelected(c); await load(c.code); };

  if (!loaded) return <div className="flex justify-center p-8"><div className="text-4xl animate-pulse">⚽</div></div>;
  if (!data) return <p className="text-center py-8 text-[var(--sc-text-secondary)]">Could not load dashboard data.</p>;

  const predictions = data.upcomingPredictions ?? data.matches ?? [];
  const topPredictors = (data as Record<string, unknown>).topPredictors as { rank: number; username?: string; displayName?: string; totalPoints: number; exactScores: number; totalPredictions?: number }[] | undefined;
  const recap = data.lastGameweekRecap ?? data.recap;
  const community = data.community ?? data.stats;

  return (
    <div className="py-4 px-2 space-y-4">
      {/* Competition filter */}
      {competitions.length > 1 && (
        <div className="flex gap-2 overflow-x-auto">
          {competitions.map((c) => (
            <button key={c.code} onClick={() => onComp(c)}
              className={`px-3 py-1 rounded-full text-xs font-bold whitespace-nowrap ${c.code === selected?.code ? "bg-[var(--sc-tertiary)] text-white" : "border border-[var(--sc-border)] opacity-70"}`}>
              {c.logoUrl && <img src={c.logoUrl} alt="" className="w-4 h-4 inline mr-1 object-contain" />}
              {c.name}
            </button>
          ))}
        </div>
      )}

      {/* Countdown */}
      {data.countdown && (
        <div className="rounded-xl p-4 text-center text-white" style={{ background: "linear-gradient(135deg,#0A1929,#1a2d45)" }}>
          <p className="text-[10px] tracking-widest opacity-60">GAMEWEEK {data.countdown.gameweekNumber}</p>
          <p className="text-2xl font-extrabold my-1">{countdown}</p>
          <p className="text-xs opacity-50">{allLocked ? "Matches have kicked off" : "until next kickoff"}</p>
          <div className="flex justify-center gap-6 mt-3">
            <div><p className="text-lg font-bold text-[var(--sc-tertiary)]">{data.countdown.totalPredictions}</p><p className="text-[10px] opacity-50">predictions</p></div>
            <div><p className="text-lg font-bold text-[var(--sc-tertiary)]">{data.countdown.totalUsers}</p><p className="text-[10px] opacity-50">predictors</p></div>
          </div>
        </div>
      )}

      {/* Recap */}
      {recap && (
        <Link href="/global/recap" className="block bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-bold">📋 GW{recap.gameweekNumber} Recap</span>
            <span className="text-xs text-[var(--sc-secondary)] font-semibold">Details →</span>
          </div>
          <div className="flex flex-wrap gap-1.5">
            {recap.bestPredictor && <span className="text-xs px-2 py-0.5 rounded-full bg-orange-50 text-[var(--sc-tertiary)] font-semibold">👑 {recap.bestPredictor} — {recap.bestPredictorPoints} pts</span>}
            <span className="text-xs px-2 py-0.5 rounded-full bg-green-50 text-green-700 font-semibold">🎯 {recap.totalExactScores ?? recap.exactScores} exact scores</span>
            <span className="text-xs px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 font-semibold">👥 {recap.totalPredictors} predictors</span>
            {recap.biggestUpset && <span className="text-xs px-2 py-0.5 rounded-full bg-red-50 text-red-600 font-semibold">😱 {recap.biggestUpset}</span>}
            {(recap.boldestCorrectPrediction ?? recap.boldestCall) && <span className="text-xs px-2 py-0.5 rounded-full bg-purple-50 text-purple-700 font-semibold">🔮 {recap.boldestCorrectPrediction ?? recap.boldestCall}</span>}
          </div>
        </Link>
      )}

      {/* Community Predictions */}
      {predictions.length > 0 && (
        <>
          <Link href="/global/predictions" className="flex items-center justify-between">
            <span className="text-sm font-bold">🔮 Community Predictions</span>
            <span className="text-xs text-[var(--sc-secondary)] font-semibold">See all →</span>
          </Link>
          {predictions.slice(0, 5).map((m) => (
            <Link key={m.matchId} href={`/matches/${m.matchId}`} className="block bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-1.5 flex-1">
                  {(m.homeTeamCrest ?? m.homeTeamLogo) && <img src={m.homeTeamCrest ?? m.homeTeamLogo!} alt="" className="w-5 h-5 object-contain" />}
                  <span className="text-sm font-semibold">{m.homeTeamShortName || m.homeTeam}</span>
                </div>
                <span className="text-xs text-[var(--sc-text-secondary)]">vs</span>
                <div className="flex items-center gap-1.5 flex-1 justify-end">
                  <span className="text-sm font-semibold">{m.awayTeamShortName || m.awayTeam}</span>
                  {(m.awayTeamCrest ?? m.awayTeamLogo) && <img src={m.awayTeamCrest ?? m.awayTeamLogo!} alt="" className="w-5 h-5 object-contain" />}
                </div>
              </div>
              {m.predictionCount > 0 ? (
                <>
                  <div className="flex gap-0.5 h-1.5 rounded-full overflow-hidden mb-1">
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
        </>
      )}

      {/* Leaderboard */}
      {topPredictors && topPredictors.length > 0 && (
        <>
          <Link href="/global/leaderboard" className="flex items-center justify-between mt-2">
            <span className="text-sm font-bold">🏆 Top Predictors</span>
            <span className="text-xs text-[var(--sc-secondary)] font-semibold">See all →</span>
          </Link>
          <div className="bg-[var(--sc-surface)] rounded-xl shadow-sm overflow-hidden divide-y divide-[var(--sc-border)]">
            {topPredictors.slice(0, 5).map((e) => {
              const medal = e.rank === 1 ? "🥇" : e.rank === 2 ? "🥈" : e.rank === 3 ? "🥉" : `#${e.rank}`;
              return (
                <div key={e.rank} className="flex items-center px-3 py-2.5">
                  <span className="w-8 font-bold">{medal}</span>
                  <div className="flex-1"><p className="text-sm font-semibold">{e.username ?? e.displayName}</p><p className="text-[11px] text-[var(--sc-text-secondary)]">{e.exactScores} exact · {e.totalPredictions} predictions</p></div>
                  <span className="font-extrabold text-[var(--sc-tertiary)]">{e.totalPoints}</span><span className="text-[11px] text-[var(--sc-text-secondary)] ml-0.5">pts</span>
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* Community Stats */}
      {community && (
        <>
          <Link href="/global/stats" className="flex items-center justify-between mt-2">
            <span className="text-sm font-bold">📊 Season Stats</span>
            <span className="text-xs text-[var(--sc-secondary)] font-semibold">Details →</span>
          </Link>
          <div className="flex gap-2">
            {[
              [community.totalPredictors, "Predictors"],
              [community.totalPredictions, "Predictions"],
              [`${community.exactScorePct ?? community.exactScoreRate}%`, "Exact Score Rate"],
            ].map(([v, l], i) => (
              <div key={i} className="flex-1 bg-[var(--sc-surface)] rounded-xl p-3 text-center shadow-sm">
                <p className="text-xl font-extrabold" style={i === 2 ? { color: "var(--sc-tertiary)" } : {}}>{v}</p>
                <p className="text-[11px] text-[var(--sc-text-secondary)]">{l}</p>
              </div>
            ))}
          </div>
          {community.hardestMatch && community.hardestMatch !== "N/A" && (
            <div className="bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
              <p className="text-[10px] text-[var(--sc-text-secondary)]">Hardest to predict</p>
              <p className="font-semibold">{community.hardestMatch}</p>
              <p className="text-xs text-red-500">{community.hardestMatchAccuracy === 0 ? "Nobody got it right!" : `Only ${community.hardestMatchAccuracy}% got it right`}</p>
            </div>
          )}
          {community.mostPredictableTeam && community.mostPredictableTeam !== "N/A" && (
            <div className="bg-[var(--sc-surface)] rounded-xl p-3 shadow-sm">
              <p className="text-[10px] text-[var(--sc-text-secondary)]">Most predictable team</p>
              <p className="font-semibold">{community.mostPredictableTeam}</p>
              <p className="text-xs text-green-600">{community.mostPredictableTeamPct}% correct result rate</p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
