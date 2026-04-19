"use client";

import { useState } from "react";
import Link from "next/link";
import type { MatchDetail, MatchEventDetail } from "@/lib/types";

const eventIcon = (type: string) =>
  ({ Goal: "⚽", PenaltyGoal: "⚽", OwnGoal: "⚽", YellowCard: "🟨", RedCard: "🟥", SecondYellow: "🟨🟥", SubIn: "🔄" }[type] ?? "");

function EventLines({ events, isHome }: { events: MatchEventDetail[]; isHome: boolean }) {
  const side = events.filter((e) => e.isHome === isHome);
  if (!side.length) return null;
  return (
    <div className={`flex-1 ${isHome ? "text-right pr-2" : "text-left pl-2"}`}>
      {side.map((e, i) => (
        <div key={i} className="text-xs mb-0.5">
          {isHome && <>{eventIcon(e.eventType)} {e.playerName} {e.minute && <span className="text-[var(--sc-text-secondary)]">{e.minute}&apos;</span>}</>}
          {!isHome && <>{e.minute && <span className="text-[var(--sc-text-secondary)]">{e.minute}&apos;</span>} {e.playerName} {eventIcon(e.eventType)}</>}
        </div>
      ))}
    </div>
  );
}

export function MatchTile({ match }: { match: MatchDetail }) {
  const [expanded, setExpanded] = useState(false);
  const isLive = match.status === "Live";
  const isFinished = match.status === "Finished";
  const isPostponed = match.status === "Postponed";

  const kickoff = match.kickoffTime ? new Date(match.kickoffTime) : null;
  const timeStr = kickoff ? kickoff.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "TBD";

  return (
    <div className={`border-b border-[var(--sc-border)] ${isLive ? "border-l-[3px] border-l-green-500 bg-gradient-to-r from-green-500/5 to-transparent" : ""} ${isPostponed ? "opacity-40" : ""}`}>
      <div
        className={`flex items-center px-3 py-2.5 ${isPostponed ? "" : "cursor-pointer hover:bg-black/[0.02]"}`}
        onClick={() => !isPostponed && setExpanded(!expanded)}
      >
        {/* Home */}
        <div className="flex-1 flex items-center justify-end gap-1.5 overflow-hidden">
          <span className="font-semibold text-sm truncate">{match.homeTeamShortName}</span>
          {match.homeTeamLogo && <img src={match.homeTeamLogo} alt="" className="w-5 h-5 shrink-0" />}
        </div>

        {/* Score / Time */}
        <div className="min-w-[60px] text-center shrink-0">
          {isPostponed && <span className="font-extrabold text-sm text-red-500">PP</span>}
          {isFinished && <span className="font-bold text-[15px]">{match.homeScore} - {match.awayScore}</span>}
          {isLive && (
            <>
              <span className="font-bold text-[15px] text-green-600">{match.homeScore ?? 0} - {match.awayScore ?? 0}</span>
              <div className="flex items-center justify-center gap-1 mt-0.5">
                <span className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse" />
                <span className="text-[9px] font-bold text-red-500 tracking-wider">LIVE</span>
              </div>
            </>
          )}
          {!isLive && !isFinished && !isPostponed && (
            <span className="text-xs text-[var(--sc-text-secondary)]">{timeStr}</span>
          )}
        </div>

        {/* Away */}
        <div className="flex-1 flex items-center gap-1.5 overflow-hidden">
          {match.awayTeamLogo && <img src={match.awayTeamLogo} alt="" className="w-5 h-5 shrink-0" />}
          <span className="font-semibold text-sm truncate">{match.awayTeamShortName}</span>
        </div>
      </div>

      {/* Expanded panel */}
      {expanded && !isPostponed && (
        <div className="px-3 py-2 bg-[var(--sc-surface)] border-t border-[var(--sc-border)]">
          {match.venue && <div className="text-center text-[11px] text-[var(--sc-text-secondary)] mb-2">🏟️ {match.venue}</div>}
          {match.events?.length > 0 && (
            <div className="flex gap-3">
              <EventLines events={match.events} isHome={true} />
              <div className="w-px bg-[var(--sc-border)]" />
              <EventLines events={match.events} isHome={false} />
            </div>
          )}
          <div className="border-t border-[var(--sc-border)] mt-2 pt-2 text-center">
            <Link href={`/matches/${match.matchId}`} className="text-[var(--sc-tertiary)] text-sm font-bold hover:underline">
              Match Centre →
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
