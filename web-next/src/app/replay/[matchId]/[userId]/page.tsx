"use client";

import { api } from "@/lib/api";
import { PredictionReplayResult } from "@/lib/types";
import { useAuth } from "@/contexts/auth-context";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import Link from "next/link";

const outcomeColor = (o?: string) => {
  if (o === "ExactScore") return "#2E7D32";
  if (o === "CorrectResultAndGoalDifference" || o === "CorrectResult") return "#1565C0";
  if (o === "CorrectGoalDifference") return "#FF6B35";
  return "#C62828";
};
const outcomeLabel = (o?: string) => {
  if (o === "ExactScore") return "EXACT SCORE";
  if (o === "CorrectResultAndGoalDifference") return "CORRECT RESULT + GD";
  if (o === "CorrectResult") return "CORRECT RESULT";
  if (o === "CorrectGoalDifference") return "CORRECT GD";
  return "INCORRECT";
};
const statusIcon = (s: string) => s === "exact" ? "✅" : s === "alive" ? "🟢" : "💀";
const trendIcon = (t: string) => t === "improving" ? "📈" : t === "declining" ? "📉" : "➡️";

export default function PredictionReplayPage() {
  const { matchId, userId } = useParams<{ matchId: string; userId: string }>();
  const { user } = useAuth();
  const [replay, setReplay] = useState<PredictionReplayResult | null>(null);
  const [isOwn, setIsOwn] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    (async () => {
      const r = await api.getPublicPredictionReplay(+matchId, +userId);
      if (r.success && r.data) setReplay(r.data);
      if (user) {
        setIsLoggedIn(true);
        const p = await api.getMyProfile();
        if (p.success && p.data) setIsOwn(p.data.id === +userId);
      }
    })();
  }, [matchId, userId, user]);

  if (!replay) return <div className="flex justify-center p-8 text-white">Loading...</div>;

  const playerName = isOwn ? "Your" : `${replay.displayName}'s`;
  const playerUpper = isOwn ? "YOUR" : `${replay.displayName.toUpperCase()}'S`;

  const share = async () => {
    const url = `${window.location.origin}/replay/${matchId}/${userId}`;
    if (navigator.share) await navigator.share({ title: "My ScoreCast Prediction", url });
    else { await navigator.clipboard.writeText(url); alert("Link copied!"); }
  };

  return (
    <div className="max-w-lg mx-auto text-white">
      {/* Scoreboard */}
      <div className="text-center py-6 bg-gradient-to-b from-[#0A1929] to-[#37003C]">
        <p className="text-xs opacity-60 mb-2">FULL TIME</p>
        <div className="flex items-center justify-center gap-4 mb-3">
          <div className="flex flex-col items-center gap-1">
            {replay.homeLogo && <img src={replay.homeLogo} alt="" className="w-10 h-10 object-contain" />}
            <span className="text-sm font-bold">{replay.homeTeam}</span>
          </div>
          <span className="text-3xl font-black">{replay.homeScore} – {replay.awayScore}</span>
          <div className="flex flex-col items-center gap-1">
            {replay.awayLogo && <img src={replay.awayLogo} alt="" className="w-10 h-10 object-contain" />}
            <span className="text-sm font-bold">{replay.awayTeam}</span>
          </div>
        </div>
        <div className="inline-block rounded-xl px-4 py-2 border-2" style={{ borderColor: outcomeColor(replay.outcome) }}>
          <p className="text-xs opacity-70">{playerUpper} PREDICTION</p>
          <p className="text-xl font-black">{replay.predictedHome} – {replay.predictedAway}</p>
          <span className="inline-block mt-1 px-3 py-0.5 rounded-full text-xs font-bold text-white"
            style={{ background: outcomeColor(replay.outcome) }}>
            {outcomeLabel(replay.outcome)} · {replay.pointsEarned} pts
          </span>
        </div>
      </div>

      {/* Death minute */}
      {replay.deathMinute ? (
        <div className="text-center py-3 text-red-400 font-bold">💀 Prediction died at minute {replay.deathMinute}</div>
      ) : replay.outcome === "ExactScore" ? (
        <div className="text-center py-3 text-green-400 font-bold">🎯 Nailed it. Exact score.</div>
      ) : null}

      {/* AI Commentary */}
      {replay.aiCommentary && (
        <div className="mx-4 my-3 bg-white/5 rounded-xl p-4">
          <p className="text-xs font-bold mb-1 opacity-70">🎙️ THE VERDICT</p>
          <p className="text-sm italic opacity-80">{replay.aiCommentary}</p>
        </div>
      )}

      {/* Goal Timeline */}
      {replay.goalTimeline.length > 0 && (
        <div className="mx-4 my-3">
          <h3 className="font-bold text-sm mb-2">Goal Timeline</h3>
          {replay.goalTimeline.map((g, i) => (
            <div key={i} className="flex items-start gap-3 mb-2 text-sm">
              <span className="opacity-60 w-8">{g.minute}&apos;</span>
              <span>{statusIcon(g.predictionStatus)}</span>
              <div>
                <p>⚽ {g.scorer} ({g.team})</p>
                <p className="opacity-60">{g.runningHome} – {g.runningAway}</p>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* League Rivals */}
      {replay.leagueRivals.length > 0 && (
        <div className="mx-4 my-3">
          <h3 className="font-bold text-sm mb-2">League Rivals</h3>
          {replay.leagueRivals.map((r, i) => (
            <div key={i} className="flex items-center gap-3 mb-2 text-sm">
              <div className="w-7 h-7 rounded-full bg-white/10 flex items-center justify-center text-xs font-bold">
                {r.displayName[0]}
              </div>
              <div className="flex-1">
                <p className="font-bold">{r.displayName}</p>
                <p className="opacity-60">{r.predictedHome} – {r.predictedAway}</p>
              </div>
              <span className="font-bold" style={{ color: outcomeColor(r.outcome) }}>{r.points} pts</span>
            </div>
          ))}
        </div>
      )}

      {/* Season Accuracy */}
      <div className="mx-4 my-3">
        <h3 className="font-bold text-sm mb-2">{playerName} Season {trendIcon(replay.seasonAccuracy.trend)}</h3>
        <div className="grid grid-cols-4 gap-2 text-center">
          {[
            [replay.seasonAccuracy.accuracyPct + "%", "Accuracy"],
            [replay.seasonAccuracy.exactScores, "Exact"],
            [replay.seasonAccuracy.correctResults, "Correct"],
            [replay.seasonAccuracy.totalPredictions, "Total"],
          ].map(([v, l], i) => (
            <div key={i}>
              <p className="text-lg font-bold text-[#FF6B35]">{v}</p>
              <p className="text-xs opacity-60">{l}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Actions */}
      <div className="text-center py-6">
        {isOwn ? (
          <div className="flex justify-center gap-3">
            <button onClick={share} className="px-4 py-2 bg-[#FF6B35] rounded-full text-sm font-bold">📤 Share</button>
            <Link href={`/matches/${matchId}`} className="px-4 py-2 border border-white/30 rounded-full text-sm font-bold">View Match</Link>
          </div>
        ) : isLoggedIn ? (
          <Link href={`/matches/${matchId}`} className="px-4 py-2 border border-white/30 rounded-full text-sm font-bold">View Match</Link>
        ) : (
          <div>
            <p className="font-extrabold text-lg mb-1">Think you can do better?</p>
            <p className="text-sm opacity-60 mb-4">Predict scores, compete with friends, and prove you know football.</p>
            <Link href="/register" className="px-6 py-2 bg-[#FF6B35] rounded-full font-bold">⚽ Join ScoreCast — It&apos;s Free</Link>
          </div>
        )}
      </div>
    </div>
  );
}
