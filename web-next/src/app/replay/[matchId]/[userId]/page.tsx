"use client";

import { api } from "@/lib/api";
import { PredictionReplayResult } from "@/lib/types";
import { useAuth } from "@/contexts/auth-context";
import { useAlert } from "@/contexts/alert-context";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import Link from "next/link";

const outcomeColor = (o?: string) => o === "ExactScore" ? "#2E7D32" : o === "CorrectResultAndGoalDifference" || o === "CorrectResult" ? "#1565C0" : o === "CorrectGoalDifference" ? "#FF6B35" : "#C62828";
const outcomeLabel = (o?: string) => o === "ExactScore" ? "EXACT SCORE" : o === "CorrectResultAndGoalDifference" ? "CORRECT RESULT + GD" : o === "CorrectResult" ? "CORRECT RESULT" : o === "CorrectGoalDifference" ? "CORRECT GD" : "INCORRECT";
const statusIcon = (s?: string) => s === "exact" ? "✅" : s === "alive" ? "🟢" : "💀";
const trendIcon = (t?: string) => t === "improving" ? "📈" : t === "declining" ? "📉" : "➡️";

export default function PredictionReplayPage() {
  const { matchId, userId } = useParams<{ matchId: string; userId: string }>();
  const { user } = useAuth();
  const { add } = useAlert();
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

  if (!replay) return <div className="flex justify-center p-8"><div className="text-4xl animate-pulse">⚽</div></div>;

  const playerName = isOwn ? "Your" : `${replay.displayName ?? replay.playerName}'s`;
  const playerUpper = isOwn ? "YOUR" : `${(replay.displayName ?? replay.playerName).toUpperCase()}'S`;
  const goals = replay.goalTimeline ?? replay.goals ?? [];
  const rivals = replay.leagueRivals ?? replay.rivals ?? [];
  const accuracy = replay.seasonAccuracy;

  const share = async () => {
    const url = `${window.location.origin}/replay/${matchId}/${userId}`;
    try {
      if (navigator.share) await navigator.share({ title: "My ScoreCast Prediction", url });
      else { await navigator.clipboard.writeText(url); add("Link copied to clipboard!", "success"); }
    } catch { /* cancelled */ }
  };

  return (
    <div className="max-w-lg mx-auto">
      {/* Scoreboard */}
      <div className="text-center py-6 text-white" style={{ background: "linear-gradient(to bottom, #0A1929, #37003C)" }}>
        <p className="text-xs opacity-60 mb-2">FULL TIME</p>
        <div className="flex items-center justify-center gap-4 mb-3">
          <div className="flex flex-col items-center gap-1">
            {(replay.homeLogo ?? replay.homeTeamLogo) && <img src={(replay.homeLogo ?? replay.homeTeamLogo)!} alt="" className="w-10 h-10 object-contain" />}
            <span className="text-sm font-bold">{replay.homeTeam}</span>
          </div>
          <span className="text-3xl font-black">{replay.homeScore} – {replay.awayScore}</span>
          <div className="flex flex-col items-center gap-1">
            {(replay.awayLogo ?? replay.awayTeamLogo) && <img src={(replay.awayLogo ?? replay.awayTeamLogo)!} alt="" className="w-10 h-10 object-contain" />}
            <span className="text-sm font-bold">{replay.awayTeam}</span>
          </div>
        </div>
        <div className="inline-block rounded-xl px-4 py-2 border-2" style={{ borderColor: outcomeColor(replay.outcome) }}>
          <p className="text-xs opacity-70">{playerUpper} PREDICTION</p>
          <p className="text-xl font-black">{replay.predictedHome ?? replay.predictedHomeScore} – {replay.predictedAway ?? replay.predictedAwayScore}</p>
          <span className="inline-block mt-1 px-3 py-0.5 rounded-full text-xs font-bold text-white" style={{ background: outcomeColor(replay.outcome) }}>
            {outcomeLabel(replay.outcome)} · {replay.pointsEarned ?? replay.pointsAwarded} pts
          </span>
        </div>
      </div>

      {/* Death minute / Exact */}
      {replay.deathMinute ? (
        <p className="text-center py-3 text-red-500 font-bold">💀 Prediction died at minute {replay.deathMinute}</p>
      ) : replay.outcome === "ExactScore" ? (
        <p className="text-center py-3 text-green-600 font-bold">🎯 Nailed it. Exact score.</p>
      ) : null}

      {/* AI Commentary */}
      {(replay.aiCommentary ?? replay.aiVerdict) && (
        <div className="mx-4 my-3 bg-gray-50 rounded-xl p-4">
          <p className="text-xs font-bold mb-1 text-[var(--sc-text-secondary)]">🎙️ THE VERDICT</p>
          <p className="text-sm italic text-[var(--sc-text-secondary)]">{replay.aiCommentary ?? replay.aiVerdict}</p>
        </div>
      )}

      {/* Goal Timeline */}
      {goals.length > 0 && (
        <div className="mx-4 my-3">
          <h3 className="font-bold text-sm mb-2">Goal Timeline</h3>
          {goals.map((g, i) => (
            <div key={i} className="flex items-start gap-3 mb-2 text-sm">
              <span className="text-[var(--sc-text-secondary)] w-8">{g.minute}&apos;</span>
              <span>{statusIcon(g.predictionStatus)}</span>
              <div>
                <p>⚽ {g.scorer} ({g.team})</p>
                <p className="text-[var(--sc-text-secondary)]">{g.runningHome} – {g.runningAway}</p>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* League Rivals */}
      {rivals.length > 0 && (
        <div className="mx-4 my-3">
          <h3 className="font-bold text-sm mb-2">League Rivals</h3>
          {rivals.map((r, i) => (
            <div key={i} className="flex items-center gap-3 mb-2 text-sm">
              <div className="w-7 h-7 rounded-full bg-gray-100 flex items-center justify-center text-xs font-bold">{r.displayName[0]}</div>
              <div className="flex-1">
                <p className="font-bold">{r.displayName}</p>
                <p className="text-[var(--sc-text-secondary)]">{r.predictedHome} – {r.predictedAway}</p>
              </div>
              <span className="font-bold" style={{ color: outcomeColor(r.outcome) }}>{r.points} pts</span>
            </div>
          ))}
        </div>
      )}

      {/* Season Accuracy */}
      {accuracy && (
        <div className="mx-4 my-3">
          <h3 className="font-bold text-sm mb-2">{playerName} Season {trendIcon(accuracy.trend)}</h3>
          <div className="grid grid-cols-4 gap-2 text-center">
            {[
              [`${accuracy.accuracyPct}%`, "Accuracy"],
              [accuracy.exactScores, "Exact"],
              [accuracy.correctResults, "Correct"],
              [accuracy.totalPredictions ?? accuracy.predictionCount, "Total"],
            ].map(([v, l], i) => (
              <div key={i}>
                <p className="text-lg font-bold text-[var(--sc-tertiary)]">{v}</p>
                <p className="text-xs text-[var(--sc-text-secondary)]">{l}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="text-center py-6 px-4">
        {isOwn ? (
          <div className="flex justify-center gap-3">
            <button onClick={share} className="px-5 py-2 bg-[var(--sc-tertiary)] text-white rounded-full text-sm font-bold">📤 Share</button>
            <Link href={`/matches/${matchId}`} className="px-5 py-2 border border-[var(--sc-border)] rounded-full text-sm font-bold">View Match</Link>
          </div>
        ) : isLoggedIn ? (
          <Link href={`/matches/${matchId}`} className="px-5 py-2 border border-[var(--sc-border)] rounded-full text-sm font-bold">View Match</Link>
        ) : (
          <div>
            <p className="font-extrabold text-lg mb-1">Think you can do better?</p>
            <p className="text-sm text-[var(--sc-text-secondary)] mb-4">Predict scores, compete with friends, and prove you know football.</p>
            <Link href="/register" className="px-6 py-2.5 bg-[var(--sc-tertiary)] text-white rounded-full font-bold">⚽ Join ScoreCast — It&apos;s Free</Link>
          </div>
        )}
      </div>
    </div>
  );
}
