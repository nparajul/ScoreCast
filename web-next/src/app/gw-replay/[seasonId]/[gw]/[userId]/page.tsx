"use client";

import { api } from "@/lib/api";
import { GameweekReplayResult } from "@/lib/types";
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
const outcomeBg = (o?: string) => {
  if (o === "ExactScore") return "rgba(46,125,50,0.15)";
  if (o === "CorrectResultAndGoalDifference" || o === "CorrectResult") return "rgba(21,101,192,0.1)";
  if (o === "CorrectGoalDifference") return "rgba(255,107,53,0.1)";
  return "rgba(198,40,40,0.08)";
};
const outcomeShort = (o?: string) => {
  if (o === "ExactScore") return "🎯 Exact";
  if (o === "CorrectResultAndGoalDifference") return "✓ Result+GD";
  if (o === "CorrectResult") return "✓ Result";
  if (o === "CorrectGoalDifference") return "~ GD";
  return "✗";
};

export default function GameweekReplayPage() {
  const { seasonId, gw, userId } = useParams<{ seasonId: string; gw: string; userId: string }>();
  const { user } = useAuth();
  const [data, setData] = useState<GameweekReplayResult | null>(null);
  const [isOwn, setIsOwn] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    (async () => {
      const r = await api.getGameweekReplay(+seasonId, +gw, +userId);
      if (r.success && r.data) setData(r.data);
      if (user) {
        setIsLoggedIn(true);
        const p = await api.getMyProfile();
        if (p.success && p.data) setIsOwn(p.data.id === +userId);
      }
    })();
  }, [seasonId, gw, userId, user]);

  if (!data) return <div className="flex justify-center p-8 text-white">Loading...</div>;

  const playerUpper = isOwn ? "YOUR" : `${data.displayName.toUpperCase()}'S`;

  const share = async () => {
    const url = `${window.location.origin}/gw-replay/${seasonId}/${gw}/${userId}`;
    if (navigator.share) await navigator.share({ title: `ScoreCast GW${gw} Replay`, url });
    else { await navigator.clipboard.writeText(url); alert("Link copied!"); }
  };

  return (
    <div className="max-w-lg mx-auto text-white">
      {/* Hero */}
      <div className="text-center py-6 bg-gradient-to-b from-[#0A1929] to-[#37003C]">
        <p className="text-xs opacity-60 mb-1">{playerUpper} GAMEWEEK {data.gameweekNumber}</p>
        {data.competitionLogo && <img src={data.competitionLogo} alt="" className="w-8 h-8 mx-auto mb-2 object-contain" />}
        <p className="text-5xl font-black">{data.totalPoints}</p>
        <p className="text-xs opacity-60 mb-2">POINTS</p>
        <p className="text-sm opacity-70">{data.matchesPredicted} predicted · {data.correctResults} correct · {data.exactScores} exact</p>
      </div>

      {/* Matches */}
      <div className="mx-4 my-3 space-y-2">
        {data.matches.map(m => (
          <div key={m.matchId} className="rounded-xl p-3" style={{ background: outcomeBg(m.outcome) }}>
            <div className="flex items-center justify-between text-sm mb-1">
              <span className="flex items-center gap-1">
                {m.homeLogo && <img src={m.homeLogo} alt="" className="w-5 h-5 object-contain" />}
                <span className="font-bold">{m.homeTeam}</span>
              </span>
              <span className="font-black">{m.homeScore} – {m.awayScore}</span>
              <span className="flex items-center gap-1">
                <span className="font-bold">{m.awayTeam}</span>
                {m.awayLogo && <img src={m.awayLogo} alt="" className="w-5 h-5 object-contain" />}
              </span>
            </div>
            <div className="flex items-center justify-between text-xs">
              <span className="opacity-60">{m.predictedHome} – {m.predictedAway}</span>
              <span className="font-bold" style={{ color: outcomeColor(m.outcome) }}>{outcomeShort(m.outcome)}</span>
              <span className="font-extrabold" style={{ color: outcomeColor(m.outcome) }}>+{m.points}</span>
            </div>
            {m.deathMinute && <p className="text-center text-[11px] text-red-500 mt-1">💀 {m.deathMinute}&apos;</p>}
          </div>
        ))}
      </div>

      {/* Actions */}
      <div className="text-center py-6">
        {isOwn ? (
          <button onClick={share} className="px-4 py-2 bg-[#FF6B35] rounded-full text-sm font-bold">📤 Share</button>
        ) : !isLoggedIn ? (
          <div>
            <p className="font-extrabold text-lg mb-1">Think you can do better?</p>
            <p className="text-sm opacity-60 mb-4">Predict scores, compete with friends, and prove you know football.</p>
            <Link href="/register" className="px-6 py-2 bg-[#FF6B35] rounded-full font-bold">⚽ Join ScoreCast — It&apos;s Free</Link>
          </div>
        ) : null}
      </div>
    </div>
  );
}
