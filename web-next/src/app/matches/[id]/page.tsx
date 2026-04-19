"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";
import type { MatchPageResult, MatchPageEvent, MatchExtrasResult, MatchHighlightsResult, MatchPageLineupPlayer } from "@/lib/types";

const eventIcon = (t: string) => ({ Goal: "⚽", PenaltyGoal: "⚽", OwnGoal: "⚽", YellowCard: "🟨", RedCard: "🟥", SecondYellow: "🟨🟥", SubIn: "🔄" }[t] ?? "");
const isSecondHalf = (min?: string) => { const n = parseInt(min ?? ""); return !isNaN(n) && n >= 46; };
const formColor = (r: string) => ({ W: "#4caf50", D: "#ff9800", L: "#f44336" }[r] ?? "#666");

type Tab = "Summary" | "Lineups" | "Stats" | "Highlights";

export default function MatchDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [match, setMatch] = useState<MatchPageResult>();
  const [tab, setTab] = useState<Tab>("Summary");
  const [extras, setExtras] = useState<MatchExtrasResult>();
  const [highlights, setHighlights] = useState<MatchHighlightsResult>();
  const pollRef = useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  const loadMatch = useCallback(async () => {
    const res = await api.getMatchPage(Number(id));
    if (res.success && res.data) setMatch(res.data);
  }, [id]);

  useEffect(() => { loadMatch(); }, [loadMatch]);

  // Polling for live
  useEffect(() => {
    if (pollRef.current) clearInterval(pollRef.current);
    if (match?.status !== "Live") return;
    pollRef.current = setInterval(loadMatch, 30000);
    return () => clearInterval(pollRef.current);
  }, [match?.status, loadMatch]);

  // Lazy load tabs
  useEffect(() => {
    if (tab === "Stats" && !extras) api.getMatchExtras(Number(id)).then((r) => r.data && setExtras(r.data));
    if (tab === "Highlights" && !highlights) api.getMatchHighlights(Number(id)).then((r) => r.data && setHighlights(r.data));
  }, [tab, id, extras, highlights]);

  if (!match) return <div className="text-center py-20 text-[var(--sc-text-secondary)]">Loading...</div>;

  const isLive = match.status === "Live";
  const isFinished = match.status === "Finished";
  const tabs: Tab[] = isLive || isFinished ? ["Summary", "Lineups", "Stats", "Highlights"] : ["Summary", "Lineups", "Stats"];

  const events = match.events
    .filter((e) => e.eventType !== "SubOut")
    .sort((a, b) => isLive ? b.sortKey - a.sortKey : a.sortKey - b.sortKey);

  return (
    <div className="max-w-2xl mx-auto px-4 pt-3 pb-24">
      {/* Scoreboard */}
      <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-5 text-center">
        {isLive && (
          <div className="flex items-center justify-center gap-2 mb-2">
            <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
            <span className="text-xs font-bold text-red-500 tracking-wider">LIVE</span>
            {match.minute && <span className="text-sm font-bold tabular-nums">{match.minute}&apos;</span>}
          </div>
        )}
        {isFinished && <div className="text-xs font-bold text-[var(--sc-text-secondary)] mb-2">FULL TIME</div>}
        {match.status === "Scheduled" && match.kickoffTime && (
          <div className="text-xs text-[var(--sc-text-secondary)] mb-2">{new Date(match.kickoffTime).toLocaleString()}</div>
        )}

        <div className="flex items-center justify-center gap-4 md:gap-8">
          <Link href={`/teams/${match.homeTeamId}`} className="flex-1 text-center">
            {match.homeTeamLogo && <img src={match.homeTeamLogo} alt="" className="w-12 h-12 md:w-16 md:h-16 mx-auto mb-1" />}
            <div className="font-bold text-sm">{match.homeTeamShortName}</div>
          </Link>
          <div className="min-w-[80px]">
            {(isLive || isFinished) ? (
              <div className={`text-3xl md:text-4xl font-extrabold tabular-nums ${isLive ? "text-green-600" : ""}`}>
                {match.homeScore ?? 0} - {match.awayScore ?? 0}
              </div>
            ) : (
              <div className="text-xl font-bold text-[var(--sc-text-secondary)]">vs</div>
            )}
            {match.halfTimeHomeScore != null && (
              <div className="text-[11px] text-[var(--sc-text-secondary)]">HT: {match.halfTimeHomeScore} - {match.halfTimeAwayScore}</div>
            )}
          </div>
          <Link href={`/teams/${match.awayTeamId}`} className="flex-1 text-center">
            {match.awayTeamLogo && <img src={match.awayTeamLogo} alt="" className="w-12 h-12 md:w-16 md:h-16 mx-auto mb-1" />}
            <div className="font-bold text-sm">{match.awayTeamShortName}</div>
          </Link>
        </div>

        <div className="mt-2.5 text-xs text-[var(--sc-text-secondary)] space-y-0.5">
          {match.competitionName && (
            <div className="flex items-center justify-center gap-1">
              {match.competitionLogo && <img src={match.competitionLogo} alt="" className="w-4 h-4" />}
              <span className="font-semibold underline underline-offset-2">{match.competitionName}</span>
            </div>
          )}
          {match.venue && <div>🏟️ {match.venue}</div>}
          {match.referee && <div>🧑‍⚖️ {match.referee}</div>}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 my-3">
        {tabs.map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`flex-1 text-center py-2 rounded-full text-xs font-bold transition-colors ${
              tab === t ? "bg-[var(--sc-surface)] text-[var(--sc-primary)]" : "text-white/70 hover:text-white"
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {/* Summary */}
      {tab === "Summary" && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
          {events.length > 0 ? (
            <>
              {isFinished && <Divider text="⏱️ Kick Off" />}
              {events.map((evt, i) => {
                const showHt = i > 0 && isSecondHalf(evt.minute) !== isSecondHalf(events[i - 1]?.minute);
                return (
                  <div key={i}>
                    {showHt && <Divider text={`HT ${match.halfTimeHomeScore ?? ""} - ${match.halfTimeAwayScore ?? ""}`} />}
                    <EventRow evt={evt} />
                  </div>
                );
              })}
              {isFinished && <Divider text={`FT ${match.homeScore} - ${match.awayScore}`} />}
              {isLive && <Divider text="⏱️ Kick Off" />}
            </>
          ) : (
            <div className="py-6 text-center text-sm text-[var(--sc-text-secondary)]">
              {match.status === "Scheduled" ? "Match hasn't started yet" : "No events recorded"}
            </div>
          )}
        </div>
      )}

      {/* Lineups */}
      {tab === "Lineups" && (
        <div>
          {match.homeLineup.length > 0 ? (
            <>
              {/* Formations header */}
              <div className="flex gap-1 mb-2">
                <div className="flex-1 rounded-lg bg-[var(--sc-surface)] border border-[var(--sc-border)] p-2 text-center">
                  <span className="text-xs font-bold">{match.homeTeamShortName}</span>
                  {match.homeFormation && <span className="text-[11px] text-[var(--sc-text-secondary)] ml-1">{match.homeFormation}</span>}
                </div>
                <div className="flex-1 rounded-lg bg-[var(--sc-surface)] border border-[var(--sc-border)] p-2 text-center">
                  <span className="text-xs font-bold">{match.awayTeamShortName}</span>
                  {match.awayFormation && <span className="text-[11px] text-[var(--sc-text-secondary)] ml-1">{match.awayFormation}</span>}
                </div>
              </div>

              {/* Starters */}
              <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
                <div className="flex">
                  <PlayerList players={match.homeLineup} />
                  <div className="w-px bg-[var(--sc-border)]" />
                  <PlayerList players={match.awayLineup} />
                </div>
              </div>

              {/* Subs */}
              {(match.homeSubs.length > 0 || match.awaySubs.length > 0) && (
                <>
                  <div className="text-xs font-bold mt-3 mb-1.5 px-1">Substitutes</div>
                  <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
                    <div className="flex">
                      <PlayerList players={match.homeSubs} />
                      <div className="w-px bg-[var(--sc-border)]" />
                      <PlayerList players={match.awaySubs} />
                    </div>
                  </div>
                </>
              )}
            </>
          ) : (
            <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] py-6 text-center text-sm text-[var(--sc-text-secondary)]">
              👕 Lineups not available yet
            </div>
          )}
        </div>
      )}

      {/* Stats */}
      {tab === "Stats" && (
        <div>
          {!extras ? (
            <div className="text-center py-12 text-sm text-[var(--sc-text-secondary)]">Loading...</div>
          ) : (
            <>
              {/* H2H */}
              {extras.headToHead.length > 0 && (
                <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden mb-3">
                  <div className="px-3 py-2 font-bold text-sm">⚔️ Head to Head</div>
                  {extras.headToHead.map((h, i) => (
                    <div key={i} className="flex items-center px-3 py-1.5 border-t border-[var(--sc-border)] text-xs">
                      <span className="w-16 text-[var(--sc-text-secondary)]">{new Date(h.kickoffTime).toLocaleDateString("en-GB", { month: "short", year: "numeric" })}</span>
                      <div className="flex-1 flex items-center justify-end gap-1">
                        {h.homeLogo && <img src={h.homeLogo} alt="" className="w-4 h-4" />}
                        <span className="font-semibold">{h.homeTeam}</span>
                      </div>
                      <span className="min-w-[50px] text-center font-extrabold">{h.homeScore} - {h.awayScore}</span>
                      <div className="flex-1 flex items-center gap-1">
                        <span className="font-semibold">{h.awayTeam}</span>
                        {h.awayLogo && <img src={h.awayLogo} alt="" className="w-4 h-4" />}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* Form */}
              {(extras.homeForm.length > 0 || extras.awayForm.length > 0) && (
                <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden mb-3">
                  <div className="px-3 py-2 font-bold text-sm">📊 Form (Last 5)</div>
                  <div className="flex border-t border-[var(--sc-border)]">
                    <div className="flex-1 p-2">
                      <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1">{match.homeTeamShortName}</div>
                      <div className="flex gap-1">
                        {extras.homeForm.map((f, i) => (
                          <span key={i} className="w-6 h-6 rounded-md flex items-center justify-center text-[10px] font-extrabold text-white" style={{ background: formColor(f.result) }}>{f.result}</span>
                        ))}
                      </div>
                    </div>
                    <div className="w-px bg-[var(--sc-border)]" />
                    <div className="flex-1 p-2">
                      <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1">{match.awayTeamShortName}</div>
                      <div className="flex gap-1">
                        {extras.awayForm.map((f, i) => (
                          <span key={i} className="w-6 h-6 rounded-md flex items-center justify-center text-[10px] font-extrabold text-white" style={{ background: formColor(f.result) }}>{f.result}</span>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Player Stats */}
              {(extras.homePlayerStats.length > 0 || extras.awayPlayerStats.length > 0) && (
                <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
                  <div className="px-3 py-2 font-bold text-sm">⭐ Season Stats</div>
                  <div className="flex border-t border-[var(--sc-border)]">
                    <div className="flex-1 p-2 space-y-1">
                      {extras.homePlayerStats.slice(0, 5).map((p) => (
                        <div key={p.playerId} className="flex items-center gap-1.5">
                          {p.photoUrl ? <img src={p.photoUrl} alt="" className="w-5 h-5 rounded-full object-cover" /> : <div className="w-5 h-5 rounded-full bg-gray-400" />}
                          <div className="min-w-0 flex-1">
                            <div className="text-[11px] font-semibold truncate">{p.name}</div>
                            <div className="text-[10px] text-[var(--sc-text-secondary)]">
                              {p.goals > 0 && <span>⚽{p.goals} </span>}
                              {p.assists > 0 && <span>👟{p.assists}</span>}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                    <div className="w-px bg-[var(--sc-border)]" />
                    <div className="flex-1 p-2 space-y-1">
                      {extras.awayPlayerStats.slice(0, 5).map((p) => (
                        <div key={p.playerId} className="flex items-center gap-1.5">
                          {p.photoUrl ? <img src={p.photoUrl} alt="" className="w-5 h-5 rounded-full object-cover" /> : <div className="w-5 h-5 rounded-full bg-gray-400" />}
                          <div className="min-w-0 flex-1">
                            <div className="text-[11px] font-semibold truncate">{p.name}</div>
                            <div className="text-[10px] text-[var(--sc-text-secondary)]">
                              {p.goals > 0 && <span>⚽{p.goals} </span>}
                              {p.assists > 0 && <span>👟{p.assists}</span>}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Highlights */}
      {tab === "Highlights" && (
        <div>
          {!highlights ? (
            <div className="text-center py-12 text-sm text-[var(--sc-text-secondary)]">Loading...</div>
          ) : highlights.videos.length > 0 ? (
            highlights.videos.map((v, i) => {
              const vidId = v.embedHtml?.match(/embed\/([a-zA-Z0-9_-]{11})/)?.[1];
              return (
                <div key={i} className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden mb-3">
                  <div className="px-3 py-2 font-bold text-sm">{v.title === "Goals" ? "⚽ Goals" : `🎬 ${v.title}`}</div>
                  {vidId && (
                    <div className="relative pb-[56.25%] border-t border-[var(--sc-border)]">
                      <iframe
                        src={`https://www.youtube.com/embed/${vidId}`}
                        className="absolute inset-0 w-full h-full"
                        allowFullScreen
                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                      />
                    </div>
                  )}
                </div>
              );
            })
          ) : (
            <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] py-6 text-center text-sm text-[var(--sc-text-secondary)]">
              🎬 No highlights available yet
            </div>
          )}
        </div>
      )}
    </div>
  );
}

/* --- Sub-components --- */

function Divider({ text }: { text: string }) {
  return (
    <div className="flex items-center px-3 py-1.5 gap-2">
      <div className="flex-1 h-px bg-[var(--sc-border)]" />
      <span className="text-[11px] font-bold text-[var(--sc-text-secondary)] whitespace-nowrap">{text}</span>
      <div className="flex-1 h-px bg-[var(--sc-border)]" />
    </div>
  );
}

function EventRow({ evt }: { evt: MatchPageEvent }) {
  const isSub = evt.eventType === "SubIn";
  return (
    <div className="flex items-center px-2 py-1.5 border-b border-[var(--sc-border)] last:border-b-0">
      <div className="flex-1 text-right pr-1.5">
        {evt.isHome && (
          isSub ? (
            <><div className="text-[11px] font-semibold text-green-600">{evt.playerName}</div><div className="text-[11px] text-red-500">{evt.playerOff}</div></>
          ) : (
            <><div className="text-xs font-semibold">{evt.playerName}</div>{evt.assistName && <div className="text-[10px] text-[var(--sc-text-secondary)]">{evt.assistName}</div>}</>
          )
        )}
      </div>
      <div className="flex flex-col items-center min-w-[44px]">
        <span className="text-sm">{eventIcon(evt.eventType)}</span>
        <span className="text-[11px] font-extrabold">{evt.minute}</span>
      </div>
      <div className="flex-1 pl-1.5">
        {!evt.isHome && (
          isSub ? (
            <><div className="text-[11px] font-semibold text-green-600">{evt.playerName}</div><div className="text-[11px] text-red-500">{evt.playerOff}</div></>
          ) : (
            <><div className="text-xs font-semibold">{evt.playerName}</div>{evt.assistName && <div className="text-[10px] text-[var(--sc-text-secondary)]">{evt.assistName}</div>}</>
          )
        )}
      </div>
    </div>
  );
}

function PlayerList({ players }: { players: MatchPageLineupPlayer[] }) {
  return (
    <div className="flex-1 p-2 space-y-1">
      {players.map((p) => (
        <div key={p.playerId} className="flex items-center gap-1.5">
          {p.photoUrl ? (
            <img src={p.photoUrl} alt="" className={`w-6 h-6 rounded-full object-cover ${p.subMinute ? "opacity-50" : ""}`} />
          ) : (
            <div className={`w-6 h-6 rounded-full bg-gray-400 flex items-center justify-center text-[10px] text-white font-bold ${p.subMinute ? "opacity-50" : ""}`}>
              {p.name[0]}
            </div>
          )}
          <div className="min-w-0 flex-1">
            <span className="text-[11px] font-semibold">
              {p.shirtNumber != null && <span className="text-[var(--sc-text-secondary)] mr-0.5">{p.shirtNumber}</span>}
              {p.name.split(" ").pop()}
              {p.isCaptain && <span className="ml-0.5 text-[9px] bg-gray-200 rounded px-0.5">C</span>}
            </span>
          </div>
          {p.subMinute && <span className="text-[9px] font-bold text-red-500">{p.subMinute}&apos;</span>}
        </div>
      ))}
    </div>
  );
}
