"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";
import type {
  MatchPageResult, MatchPageEvent, MatchExtrasResult, MatchHighlightsResult,
  MatchPageLineupPlayer, PredictionReplayResult, MatchPredictionResult,
} from "@/lib/types";

/* ── helpers ── */
const eventIcon = (t: string) =>
  ({ Goal: "⚽", PenaltyGoal: "🎯", OwnGoal: "⚽", YellowCard: "🟨", RedCard: "🟥", SecondYellow: "🟨🟥", PenaltySaved: "🧤", PenaltyMissed: "❌", SubIn: "🔄" }[t] ?? "");
const isSecondHalf = (min?: string) => { const n = parseInt(min ?? ""); return !isNaN(n) && n >= 46; };
const formColor = (r: string) => ({ W: "#4caf50", D: "#ff9800", L: "#f44336" }[r] ?? "#666");
const predBg = (o?: string) =>
  ({ ExactScore: "#4caf50", CorrectResultAndGoalDifference: "#2196f3", CorrectResult: "#42a5f5", CorrectGoalDifference: "#ff9800" }[o ?? ""] ?? "#666");
const predLabel = (o?: string) =>
  (o ?? "").replace("CorrectResultAndGoalDifference", "Result + GD").replace("CorrectResult", "Result ✓").replace("ExactScore", "Exact! 🎯").replace("CorrectGoalDifference", "GD ✓").replace("Incorrect", "✗");
const lastName = (n: string) => { const p = n.split(" "); return p.length > 1 ? p[p.length - 1] : n; };

type Tab = "Summary" | "Lineups" | "Stats" | "Highlights";

export default function MatchDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [match, setMatch] = useState<MatchPageResult>();
  const [tab, setTab] = useState<Tab>("Summary");
  const [extras, setExtras] = useState<MatchExtrasResult>();
  const [highlights, setHighlights] = useState<MatchHighlightsResult>();
  const [replay, setReplay] = useState<PredictionReplayResult>();
  const [prediction, setPrediction] = useState<MatchPredictionResult>();
  const [hlLoading, setHlLoading] = useState(false);
  const [muted, setMuted] = useState(true);
  const [lineupSide, setLineupSide] = useState<"home" | "away">("home");
  const pollRef = useRef<ReturnType<typeof setInterval>>(undefined);
  const [clock, setClock] = useState<string>();

  /* ── persist tab ── */
  useEffect(() => {
    const h = window.location.hash.replace("#", "") as Tab;
    if (["Summary", "Lineups", "Stats", "Highlights"].includes(h)) setTab(h);
  }, []);
  const switchTab = (t: Tab) => { setTab(t); window.location.hash = t; };

  /* ── load match ── */
  const loadMatch = useCallback(async () => {
    const r = await api.getMatchPage(Number(id));
    if (r.success && r.data) setMatch(r.data);
  }, [id]);
  useEffect(() => { loadMatch(); }, [loadMatch]);

  /* ── live clock ── */
  useEffect(() => {
    if (!match || match.status !== "Live") return;
    if (match.phase === "HalfTime") { setClock("HT"); return; }
    const tick = () => {
      const now = Date.now();
      let secs: number;
      if (match.phase === "SecondHalf" && match.secondHalfStartMillis)
        secs = Math.floor((now - match.secondHalfStartMillis) / 1000) + 2700;
      else if (match.firstHalfStartMillis)
        secs = Math.floor((now - match.firstHalfStartMillis) / 1000);
      else return;
      if (secs < 0) secs = 0;
      setClock(`${Math.floor(secs / 60)}:${String(secs % 60).padStart(2, "0")}`);
    };
    tick();
    const iv = setInterval(tick, 1000);
    return () => clearInterval(iv);
  }, [match]);

  /* ── 30s polling when live ── */
  useEffect(() => {
    if (pollRef.current) clearInterval(pollRef.current);
    if (match?.status !== "Live") return;
    pollRef.current = setInterval(loadMatch, 30000);
    return () => clearInterval(pollRef.current);
  }, [match?.status, loadMatch]);

  /* ── lazy-load tab data ── */
  useEffect(() => {
    if (tab === "Stats" && !extras) api.getMatchExtras(Number(id)).then(r => r.data && setExtras(r.data));
    if (tab === "Highlights" && !highlights && !hlLoading) {
      setHlLoading(true);
      api.getMatchHighlights(Number(id)).then(r => { setHighlights(r.data ?? { videos: [] }); setHlLoading(false); });
    }
    if (tab === "Summary" && match?.status === "Scheduled" && !prediction)
      api.getMatchPrediction(Number(id)).then(r => r.data && setPrediction(r.data));
    if (tab === "Summary" && (match?.status === "Finished" || match?.status === "Live") && !replay)
      api.getPredictionReplay(Number(id)).then(r => r.data && setReplay(r.data));
  }, [tab, id, extras, highlights, hlLoading, match?.status, prediction, replay]);

  if (!match) return <div className="text-center py-20 text-[var(--sc-text-secondary)]">Loading…</div>;

  const isLive = match.status === "Live";
  const isFinished = match.status === "Finished";
  const tabs: Tab[] = isLive || isFinished ? ["Summary", "Lineups", "Stats", "Highlights"] : ["Summary", "Lineups", "Stats"];
  const events = match.events.filter(e => e.eventType !== "SubOut").sort((a, b) => isLive ? b.sortKey - a.sortKey : a.sortKey - b.sortKey);
  const showHtScore = match.halfTimeHomeScore != null && match.phase !== "FirstHalf" && match.phase !== "HalfTime";

  return (
    <div className="max-w-2xl mx-auto px-4 pt-3 pb-24">
      {/* Back */}
      <button onClick={() => router.back()} className="text-xs text-[var(--sc-text-secondary)] mb-2 hover:underline">← Back</button>

      {/* ══ SCOREBOARD ══ */}
      <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-5 text-center">
        {isLive && (
          <div className="flex items-center justify-center gap-2 mb-2">
            <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
            <span className="text-xs font-bold text-red-500 tracking-wider">LIVE</span>
            {clock && <span className="text-sm font-bold tabular-nums">{clock}</span>}
          </div>
        )}
        {isFinished && <div className="text-xs font-bold text-[var(--sc-text-secondary)] mb-2">FULL TIME</div>}
        {match.status === "Postponed" && <div className="text-sm font-extrabold text-red-500 mb-2">POSTPONED</div>}
        {match.status === "Scheduled" && match.kickoffTime && (
          <div className="text-xs text-[var(--sc-text-secondary)] mb-2">{new Date(match.kickoffTime).toLocaleString()}</div>
        )}
        <div className="flex items-center justify-center gap-4 md:gap-8">
          <Link href={`/teams/${match.homeTeamId}`} className="flex-1 text-center no-underline">
            {match.homeTeamLogo && <img src={match.homeTeamLogo} alt="" className="w-12 h-12 md:w-16 md:h-16 mx-auto mb-1" />}
            <div className="font-bold text-xs md:text-sm">{match.homeTeamShortName}</div>
          </Link>
          <div className="min-w-[80px]">
            {match.status === "Postponed" ? (
              <div className="text-xl font-extrabold text-red-500">PP</div>
            ) : isLive || isFinished ? (
              <>
                <div className={`text-3xl md:text-4xl font-extrabold tabular-nums ${isLive ? "text-green-600" : ""}`}>
                  {match.homeScore ?? 0} - {match.awayScore ?? 0}
                </div>
                {showHtScore && <div className="text-[11px] text-[var(--sc-text-secondary)]">HT: {match.halfTimeHomeScore} - {match.halfTimeAwayScore}</div>}
              </>
            ) : (
              <div className="text-xl font-bold text-[var(--sc-text-secondary)]">vs</div>
            )}
          </div>
          <Link href={`/teams/${match.awayTeamId}`} className="flex-1 text-center no-underline">
            {match.awayTeamLogo && <img src={match.awayTeamLogo} alt="" className="w-12 h-12 md:w-16 md:h-16 mx-auto mb-1" />}
            <div className="font-bold text-xs md:text-sm">{match.awayTeamShortName}</div>
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

      {/* ══ TABS ══ */}
      <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 my-3">
        {tabs.map(t => (
          <button key={t} onClick={() => switchTab(t)}
            className={`flex-1 text-center py-2 rounded-full text-xs font-bold transition-colors ${tab === t ? "bg-[var(--sc-surface)] text-[var(--sc-primary)]" : "text-white/70 hover:text-white"}`}>
            {t}
          </button>
        ))}
      </div>

      {/* ══ SUMMARY TAB ══ */}
      {tab === "Summary" && <SummaryTab match={match} events={events} isLive={isLive} isFinished={isFinished} prediction={prediction} replay={replay} />}

      {/* ══ LINEUPS TAB ══ */}
      {tab === "Lineups" && <LineupsTab match={match} lineupSide={lineupSide} setLineupSide={setLineupSide} />}

      {/* ══ STATS TAB ══ */}
      {tab === "Stats" && <StatsTab match={match} extras={extras} />}

      {/* ══ HIGHLIGHTS TAB ══ */}
      {tab === "Highlights" && <HighlightsTab highlights={highlights} loading={hlLoading} muted={muted} setMuted={setMuted} />}

      {/* Share replay button */}
      {(isFinished || isLive) && (
        <button onClick={() => { navigator.clipboard.writeText(`${window.location.origin}/replay/${match.matchId}/me`); }}
          className="mt-4 w-full py-2.5 rounded-xl bg-[var(--sc-primary)] text-white text-xs font-bold hover:opacity-90 transition-opacity">
          📤 Share Replay Link
        </button>
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════
   SUMMARY TAB
   ═══════════════════════════════════════════════════════════════ */
function SummaryTab({ match, events, isLive, isFinished, prediction, replay }: {
  match: MatchPageResult; events: MatchPageEvent[]; isLive: boolean; isFinished: boolean;
  prediction?: MatchPredictionResult; replay?: PredictionReplayResult;
}) {
  return (
    <>
      {/* Events timeline */}
      <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
        {events.length > 0 || match.status !== "Scheduled" ? (
          <>
            {isFinished && <Divider text="⏱️ Kick Off" />}
            {events.map((evt, i) => {
              const prev = events[i - 1];
              const showHt = i > 0 && (isLive ? !isSecondHalf(evt.minute) && isSecondHalf(prev?.minute) : isSecondHalf(evt.minute) && !isSecondHalf(prev?.minute));
              return (
                <div key={i}>
                  {showHt && <Divider text={`HT ${match.halfTimeHomeScore ?? ""} - ${match.halfTimeAwayScore ?? ""}`} />}
                  <EventRow evt={evt} />
                </div>
              );
            })}
            {isLive && <Divider text="⏱️ Kick Off" />}
            {isFinished && <Divider text={`FT ${match.homeScore} - ${match.awayScore}`} />}
          </>
        ) : (
          <div className="py-6 text-center text-sm text-[var(--sc-text-secondary)]">
            {match.status === "Scheduled" ? (
              prediction ? <AIPredictionCard prediction={prediction} match={match} /> : (
                match.homeFormation ? "✅ Lineups confirmed — match hasn't kicked off yet" : "Match hasn't started yet"
              )
            ) : "No events recorded"}
          </div>
        )}
      </div>

      {/* Replay / Your prediction */}
      {replay && <ReplayCard replay={replay} />}

      {/* Community predictions (from extras loaded inline) */}
      {(isFinished || isLive) && <CommunityInline matchId={match.matchId} />}
    </>
  );
}

/* ── AI Prediction Card ── */
function AIPredictionCard({ prediction: p, match }: { prediction: MatchPredictionResult; match: MatchPageResult }) {
  return (
    <div className="text-[var(--sc-text)] px-4 py-2">
      <div className="flex items-center justify-center gap-1 mb-3">
        <span className="text-[var(--sc-primary)]">✨</span>
        <span className="font-bold text-sm">AI Match Prediction</span>
      </div>
      <div className="flex justify-around mb-3">
        {[{ pct: p.homeWinPct, label: match.homeTeamShortName }, { pct: p.drawPct, label: "Draw" }, { pct: p.awayWinPct, label: match.awayTeamShortName }].map((x, i) => (
          <div key={i} className="text-center">
            <div className="text-2xl font-extrabold">{x.pct}%</div>
            <div className="text-[11px] text-[var(--sc-text-secondary)]">{x.label}</div>
          </div>
        ))}
      </div>
      <div className="flex h-2 rounded overflow-hidden mb-4">
        <div style={{ width: `${p.homeWinPct}%` }} className="bg-[var(--sc-tertiary)]" />
        <div style={{ width: `${p.drawPct}%` }} className="bg-gray-400" />
        <div style={{ width: `${p.awayWinPct}%` }} className="bg-[var(--sc-primary)]" />
      </div>
      <div className="flex justify-around mb-3">
        <div className="text-center"><div className="text-xl font-bold">{p.homeExpectedGoals}</div><div className="text-[10px] text-[var(--sc-text-secondary)]">xG {match.homeTeamShortName}</div></div>
        <div className="text-center"><div className="text-xl font-bold">{p.awayExpectedGoals}</div><div className="text-[10px] text-[var(--sc-text-secondary)]">xG {match.awayTeamShortName}</div></div>
      </div>
      {p.topScorelines.length > 0 && (
        <>
          <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1.5 text-center">MOST LIKELY SCORES</div>
          <div className="flex flex-wrap justify-center gap-2">
            {p.topScorelines.map((s, i) => (
              <div key={i} className="bg-[var(--sc-bg)] rounded-lg px-3 py-1.5 min-w-[60px] text-center">
                <div className="text-base font-extrabold">{s.home}-{s.away}</div>
                <div className="text-[10px] text-[var(--sc-text-secondary)]">{s.pct}%</div>
              </div>
            ))}
          </div>
        </>
      )}
      <div className="text-[10px] text-[var(--sc-text-secondary)] mt-3 italic text-center">Based on Poisson model using season form, attack/defence strength &amp; head-to-head</div>
    </div>
  );
}

/* ── Replay Card ── */
function ReplayCard({ replay: r }: { replay: PredictionReplayResult }) {
  const predHome = r.predictedHome ?? r.predictedHomeScore ?? 0;
  const predAway = r.predictedAway ?? r.predictedAwayScore ?? 0;
  const pts = r.pointsEarned ?? r.pointsAwarded ?? 0;
  const goals = r.goalTimeline ?? r.goals ?? [];
  const rivals = r.leagueRivals ?? r.rivals ?? [];
  return (
    <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-3.5 mt-3">
      <div className="font-bold text-sm mb-2">🎯 Your Prediction</div>
      <div className="text-center">
        <div className="text-3xl font-extrabold">{predHome} - {predAway}</div>
        {r.outcome && (
          <div className="mt-1">
            <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-xl text-white" style={{ background: predBg(r.outcome) }}>
              {predLabel(r.outcome)} — {pts} pts
            </span>
          </div>
        )}
      </div>
      {r.deathMinute && (
        <div className="mt-3 p-2.5 rounded-lg bg-red-500/10 text-center text-sm text-red-700">
          💀 Your prediction died at minute <strong>{r.deathMinute}</strong>
        </div>
      )}
      {!r.deathMinute && r.outcome === "ExactScore" && (
        <div className="mt-3 p-2.5 rounded-lg bg-green-500/10 text-center text-sm text-green-700 font-semibold">
          🎯 You nailed it. Exact score.
        </div>
      )}
      {r.aiCommentary && (
        <div className="mt-2.5 p-2.5 rounded-lg bg-[var(--sc-bg)] border border-[var(--sc-border)]">
          <div className="text-[10px] font-bold text-[var(--sc-text-secondary)] tracking-wide mb-1">🎙️ THE VERDICT</div>
          <div className="text-sm leading-relaxed italic">{r.aiCommentary}</div>
        </div>
      )}
      {goals.length > 0 && (
        <div className="mt-2.5">
          <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1.5">GOAL TIMELINE</div>
          {goals.map((g, i) => (
            <div key={i} className="flex items-center gap-2 py-1 text-xs border-b border-black/5 last:border-0">
              <span className="min-w-[28px] text-right font-bold text-[var(--sc-text-secondary)]">{g.minute}&apos;</span>
              <span>{g.predictionStatus === "exact" ? "✅" : g.predictionStatus === "alive" ? "🟢" : "💀"}</span>
              <span className="font-semibold">⚽ {g.scorer}</span>
              <span className="ml-auto text-[var(--sc-text-secondary)]">{g.runningHome}–{g.runningAway}</span>
            </div>
          ))}
        </div>
      )}
      {rivals.length > 0 && (
        <div className="mt-2.5">
          <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1.5">LEAGUE RIVALS</div>
          {rivals.map((rv, i) => (
            <div key={i} className="flex items-center gap-2 py-1.5 text-xs border-b border-black/5 last:border-0">
              <span className="font-semibold">{rv.displayName}</span>
              <span className="text-[var(--sc-text-secondary)]">{rv.predictedHome}–{rv.predictedAway}</span>
              <span className="ml-auto font-bold" style={{ color: predBg(rv.outcome) }}>{rv.points} pts</span>
            </div>
          ))}
        </div>
      )}
      {r.seasonAccuracy && (
        <div className="mt-3 flex gap-1.5">
          {[
            { val: `${r.seasonAccuracy.accuracyPct ?? 0}%`, label: "ACCURACY" },
            { val: r.seasonAccuracy.exactScores ?? 0, label: "EXACT" },
            { val: r.seasonAccuracy.totalPredictions ?? r.seasonAccuracy.predictionCount ?? 0, label: "TOTAL" },
          ].map((s, i) => (
            <div key={i} className="flex-1 text-center bg-[var(--sc-bg)] rounded-lg py-2.5 border border-[var(--sc-border)]">
              <div className="text-lg font-extrabold text-[var(--sc-primary)]">{s.val}</div>
              <div className="text-[10px] font-semibold text-[var(--sc-text-secondary)]">{s.label}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

/* ── Community predictions (inline loader) ── */
function CommunityInline({ matchId }: { matchId: number }) {
  const [ext, setExt] = useState<MatchExtrasResult>();
  useEffect(() => { api.getMatchExtras(matchId).then(r => r.data && setExt(r.data)); }, [matchId]);
  if (!ext || ext.community.totalPredictions === 0) return null;
  const c = ext.community;
  return (
    <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-3.5 mt-3">
      <div className="font-bold text-sm mb-2">👥 Community ({c.totalPredictions} predictions)</div>
      <div className="flex justify-around text-center mb-2">
        {[{ pct: c.homeWinPct, l: "Home" }, { pct: c.drawPct, l: "Draw" }, { pct: c.awayWinPct, l: "Away" }].map((x, i) => (
          <div key={i}><div className="text-xl font-extrabold">{x.pct}%</div><div className="text-[11px] text-[var(--sc-text-secondary)]">{x.l}</div></div>
        ))}
      </div>
      {c.mostPopularScore && (
        <div className="text-center text-xs text-[var(--sc-text-secondary)]">
          Most predicted: <span className="font-bold text-[var(--sc-text)]">{c.mostPopularScore}</span> ({c.mostPopularScorePct}%)
        </div>
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════
   LINEUPS TAB
   ═══════════════════════════════════════════════════════════════ */
function LineupsTab({ match, lineupSide, setLineupSide }: {
  match: MatchPageResult; lineupSide: "home" | "away"; setLineupSide: (s: "home" | "away") => void;
}) {
  if (match.homeLineup.length === 0 && match.awayLineup.length === 0)
    return <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] py-6 text-center text-sm text-[var(--sc-text-secondary)]">👕 Lineups not available yet</div>;

  const hasFormation = match.homeFormation || match.awayFormation;
  const showHome = lineupSide === "home";
  const activeLineup = showHome ? match.homeLineup : match.awayLineup;
  const activeFormation = showHome ? match.homeFormation : match.awayFormation;

  return (
    <div>
      {hasFormation && (
        <>
          {/* Team toggle (mobile-first) */}
          <div className="flex gap-1 mb-1">
            {(["home", "away"] as const).map(side => {
              const active = lineupSide === side;
              const name = side === "home" ? match.homeTeamShortName : match.awayTeamShortName;
              const logo = side === "home" ? match.homeTeamLogo : match.awayTeamLogo;
              const formation = side === "home" ? match.homeFormation : match.awayFormation;
              return (
                <button key={side} onClick={() => setLineupSide(side)}
                  className={`flex-1 py-2 rounded-lg flex flex-col items-center gap-0.5 border-none cursor-pointer ${active ? "bg-[var(--sc-primary)] text-white" : "bg-[var(--sc-surface)]"}`}>
                  <span className="flex items-center gap-1 font-bold text-sm">
                    {logo && <img src={logo} alt="" className="w-4 h-4" />}{name}
                  </span>
                  <span className="text-[11px] font-medium opacity-60">{formation}</span>
                </button>
              );
            })}
          </div>

          {/* Pitch */}
          {activeFormation && (
            <div className="rounded-xl overflow-hidden relative" style={{ background: "linear-gradient(180deg,#1b5e20,#2e7d32)" }}>
              {/* Pitch markings */}
              <div className="absolute inset-0 pointer-events-none">
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[55%] h-[50px] border border-white/15 border-t-0" />
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[22%] h-[20px] border border-white/25 border-t-0" />
              </div>
              <div className="flex flex-col justify-evenly min-h-[380px] py-1">
                {getFormationRows(activeLineup, activeFormation, !showHome).map((row, ri) => (
                  <div key={ri} className="flex justify-evenly">
                    {row.map(p => <PitchPlayer key={p.playerId} p={p} />)}
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {/* Coaches */}
      {(match.homeCoach || match.awayCoach) && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] mt-3 overflow-hidden">
          <div className="flex p-2.5">
            <CoachSide name={match.homeCoach} photo={match.homeCoachPhoto} team={match.homeTeamShortName} align="left" />
            <div className="w-px bg-[var(--sc-border)] mx-2.5" />
            <CoachSide name={match.awayCoach} photo={match.awayCoachPhoto} team={match.awayTeamShortName} align="right" />
          </div>
        </div>
      )}

      {/* Substitutes */}
      {(match.homeSubs.length > 0 || match.awaySubs.length > 0) && (
        <>
          <div className="text-xs font-bold mt-3 mb-1.5 px-1">Substitutes</div>
          <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
            <div className="flex">
              <SubGrid players={match.homeSubs} />
              <div className="w-px bg-[var(--sc-border)]" />
              <SubGrid players={match.awaySubs} />
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function getFormationRows(players: MatchPageLineupPlayer[], formation: string, reverse: boolean) {
  if (!players.length) return [];
  const rows: MatchPageLineupPlayer[][] = [[players[0]]];
  const rest = players.slice(1);
  const counts = formation.split("-").map(Number);
  let idx = 0;
  for (const c of counts) { rows.push(rest.slice(idx, idx + c)); idx += c; }
  if (reverse) rows.reverse();
  return rows;
}

function PitchPlayer({ p }: { p: MatchPageLineupPlayer }) {
  return (
    <div className="text-center">
      <div className="relative inline-block">
        {p.photoUrl ? (
          <img src={p.photoUrl} alt="" className={`w-10 h-10 rounded-full object-cover border-2 border-white/30 bg-gray-700 ${p.subMinute ? "opacity-50" : ""}`} />
        ) : (
          <div className={`w-10 h-10 rounded-full bg-gray-600 flex items-center justify-center border-2 border-white/30 text-white font-bold text-sm ${p.subMinute ? "opacity-50" : ""}`}>
            {p.name[0]}
          </div>
        )}
        {p.subMinute && <span className="absolute -top-1 -left-1 text-[7px] bg-red-500 text-white rounded-md px-0.5 font-extrabold leading-none py-px">{p.subMinute}</span>}
        {p.isCaptain && <span className="absolute -bottom-0.5 -left-0.5 text-[8px] bg-white text-gray-800 rounded-full w-3.5 h-3.5 flex items-center justify-center font-extrabold">C</span>}
        {p.icons.filter(i => i !== "SubIn" && i !== "SubOut").length > 0 && (
          <div className="absolute -bottom-0.5 -right-1 text-[10px] leading-none flex gap-px">
            {p.icons.filter(i => i !== "SubIn" && i !== "SubOut").map((ic, j) => <span key={j} dangerouslySetInnerHTML={{ __html: playerIcon(ic) }} />)}
          </div>
        )}
      </div>
      <div className="text-[9px] text-white font-semibold mt-0.5 whitespace-nowrap overflow-hidden text-ellipsis">
        {p.shirtNumber != null && `${p.shirtNumber} `}{lastName(p.name)}
      </div>
    </div>
  );
}

function playerIcon(t: string) {
  const m: Record<string, string> = {
    Goal: '<span style="font-size:13px">⚽</span>',
    PenaltyGoal: '<span style="font-size:13px">⚽</span>',
    OwnGoal: '<span style="font-size:13px;filter:grayscale(1) brightness(0.4) sepia(1) hue-rotate(-30deg) saturate(5)">⚽</span>',
    Assist: "👟",
    YellowCard: '<span style="display:inline-block;width:8px;height:11px;background:#fdd835;border-radius:1px;vertical-align:middle"></span>',
    RedCard: '<span style="display:inline-block;width:8px;height:11px;background:#d32f2f;border-radius:1px;vertical-align:middle"></span>',
    SecondYellow: '<span style="display:inline-block;position:relative;width:12px;height:13px;vertical-align:middle"><span style="position:absolute;left:0;bottom:0;width:8px;height:11px;background:#fdd835;border-radius:1px"></span><span style="position:absolute;right:0;top:0;width:8px;height:11px;background:#d32f2f;border-radius:1px"></span></span>',
  };
  return m[t] ?? "";
}

function CoachSide({ name, photo, team, align }: { name?: string; photo?: string; team: string; align: "left" | "right" }) {
  const isRight = align === "right";
  return (
    <div className={`flex items-center gap-2 flex-1 ${isRight ? "justify-end" : ""}`}>
      {isRight && (
        <div className="text-right">
          <div className="text-[10px] font-bold text-[var(--sc-text-secondary)]">COACH</div>
          <div className="text-sm font-semibold">{name ?? "—"}</div>
          <div className="text-[10px] text-[var(--sc-text-secondary)]">{team}</div>
        </div>
      )}
      {photo && <img src={photo} alt="" className="w-9 h-9 rounded-full object-cover" />}
      {!isRight && (
        <div>
          <div className="text-[10px] font-bold text-[var(--sc-text-secondary)]">COACH</div>
          <div className="text-sm font-semibold">{name ?? "—"}</div>
          <div className="text-[10px] text-[var(--sc-text-secondary)]">{team}</div>
        </div>
      )}
    </div>
  );
}

function SubGrid({ players }: { players: MatchPageLineupPlayer[] }) {
  const sorted = [...players].sort((a, b) => (a.subMinute ? 0 : 1) - (b.subMinute ? 0 : 1));
  return (
    <div className="flex-1 p-2.5">
      <div className="grid grid-cols-2 gap-2 gap-x-1">
        {sorted.map(p => {
          const isOn = !!p.subMinute;
          return (
            <div key={p.playerId} className="text-center py-1.5">
              <div className="relative inline-block">
                {p.photoUrl ? (
                  <img src={p.photoUrl} alt="" className={`w-9 h-9 rounded-full object-cover bg-gray-700 ${isOn ? "" : "opacity-50"}`} />
                ) : (
                  <div className={`w-9 h-9 rounded-full bg-gray-600 flex items-center justify-center text-xs text-white font-bold ${isOn ? "" : "opacity-50"}`}>{p.name[0]}</div>
                )}
                {isOn && <span className="absolute -top-1 -right-1.5 text-[8px] bg-green-600 text-white rounded-lg px-1 py-px font-bold leading-tight">{p.subMinute}&apos;</span>}
              </div>
              <div className={`text-[11px] mt-0.5 whitespace-nowrap overflow-hidden text-ellipsis ${isOn ? "font-bold" : "font-semibold"}`}>
                {p.shirtNumber != null && `${p.shirtNumber} `}{lastName(p.name)}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════
   STATS TAB
   ═══════════════════════════════════════════════════════════════ */
function StatsTab({ match, extras }: { match: MatchPageResult; extras?: MatchExtrasResult }) {
  if (!extras) return <div className="text-center py-12 text-sm text-[var(--sc-text-secondary)]">Loading…</div>;
  return (
    <div className="space-y-3">
      {/* Your prediction */}
      {extras.myPrediction && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-3.5">
          <div className="font-bold text-sm mb-2">🎯 Your Prediction</div>
          <div className="text-center">
            <div className="text-3xl font-extrabold">{extras.myPrediction.predictedHome} - {extras.myPrediction.predictedAway}</div>
            {extras.myPrediction.outcome && (
              <div className="mt-1">
                <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-xl text-white" style={{ background: predBg(extras.myPrediction.outcome) }}>
                  {predLabel(extras.myPrediction.outcome)} — {extras.myPrediction.points} pts
                </span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Community */}
      {extras.community.totalPredictions > 0 && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] p-3.5">
          <div className="font-bold text-sm mb-2">👥 Community ({extras.community.totalPredictions} predictions)</div>
          <div className="flex justify-around text-center mb-2">
            {[{ pct: extras.community.homeWinPct, l: "Home" }, { pct: extras.community.drawPct, l: "Draw" }, { pct: extras.community.awayWinPct, l: "Away" }].map((x, i) => (
              <div key={i}><div className="text-xl font-extrabold">{x.pct}%</div><div className="text-[11px] text-[var(--sc-text-secondary)]">{x.l}</div></div>
            ))}
          </div>
          {extras.community.mostPopularScore && (
            <div className="text-center text-xs text-[var(--sc-text-secondary)]">
              Most predicted: <span className="font-bold text-[var(--sc-text)]">{extras.community.mostPopularScore}</span> ({extras.community.mostPopularScorePct}%)
            </div>
          )}
        </div>
      )}

      {/* H2H */}
      {extras.headToHead.length > 0 && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
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
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
          <div className="px-3 py-2 font-bold text-sm">📊 Form (Last 5)</div>
          <div className="flex border-t border-[var(--sc-border)]">
            <FormSide name={match.homeTeamShortName} form={extras.homeForm} />
            <div className="w-px bg-[var(--sc-border)]" />
            <FormSide name={match.awayTeamShortName} form={extras.awayForm} />
          </div>
        </div>
      )}

      {/* Player Stats */}
      {(extras.homePlayerStats.length > 0 || extras.awayPlayerStats.length > 0) && (
        <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden">
          <div className="px-3 py-2 font-bold text-sm">⭐ Season Stats</div>
          <div className="flex border-t border-[var(--sc-border)]">
            <PlayerStatsSide players={extras.homePlayerStats} />
            <div className="w-px bg-[var(--sc-border)]" />
            <PlayerStatsSide players={extras.awayPlayerStats} />
          </div>
        </div>
      )}
    </div>
  );
}

function FormSide({ name, form }: { name: string; form: import("@/lib/types").FormEntry[] }) {
  return (
    <div className="flex-1 p-2">
      <div className="text-[11px] font-bold text-[var(--sc-text-secondary)] mb-1">{name}</div>
      <div className="flex gap-1">
        {form.map((f, i) => (
          <span key={i} className="w-6 h-6 rounded-md flex items-center justify-center text-[10px] font-extrabold text-white" style={{ background: formColor(f.result) }}>{f.result}</span>
        ))}
      </div>
    </div>
  );
}

function PlayerStatsSide({ players }: { players: import("@/lib/types").PlayerSeasonStat[] }) {
  return (
    <div className="flex-1 p-2 space-y-1">
      {players.slice(0, 5).map(p => (
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
  );
}

/* ═══════════════════════════════════════════════════════════════
   HIGHLIGHTS TAB
   ═══════════════════════════════════════════════════════════════ */
function HighlightsTab({ highlights, loading, muted, setMuted }: {
  highlights?: MatchHighlightsResult; loading: boolean; muted: boolean; setMuted: (m: boolean) => void;
}) {
  if (loading) return <div className="text-center py-12 text-sm text-[var(--sc-text-secondary)]">Loading…</div>;
  if (!highlights || highlights.videos.length === 0)
    return <div className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] py-6 text-center text-sm text-[var(--sc-text-secondary)]">🎬 No highlights available yet</div>;

  return (
    <div>
      <div className="flex justify-end mb-2">
        <button onClick={() => setMuted(!muted)} className="text-xs px-2 py-1 rounded-lg bg-[var(--sc-surface)] border border-[var(--sc-border)]">
          {muted ? "🔇 Unmute" : "🔊 Mute"}
        </button>
      </div>
      {highlights.videos.map((v, i) => {
        const vidId = v.embedHtml?.match(/embed\/([a-zA-Z0-9_-]{11})/)?.[1]
          ?? v.embedHtml?.match(/shorts\/([a-zA-Z0-9_-]{11})/)?.[1];
        const isShort = !!v.embedHtml?.includes("/shorts/");
        if (!vidId) return null;
        return (
          <div key={i} className="rounded-xl bg-[var(--sc-surface)] border border-[var(--sc-border)] overflow-hidden mb-3">
            <div className="px-3 py-2 font-bold text-sm">
              {v.title === "Clip" ? "🎥 Clip" : v.title === "Goals" ? "⚽ Goals" : `🎬 ${v.title}`}
            </div>
            <div className={`relative border-t border-[var(--sc-border)] ${isShort ? "pb-[177%]" : "pb-[56.25%]"} h-0 overflow-hidden`}>
              <iframe
                src={`https://www.youtube-nocookie.com/embed/${vidId}?rel=0&modestbranding=1&playsinline=1${muted ? "&mute=1" : ""}`}
                className="absolute inset-0 w-full h-full"
                allowFullScreen
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              />
            </div>
          </div>
        );
      })}
      <div className="text-center text-[10px] text-[var(--sc-text-secondary)] py-1">Powered by YouTube</div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════
   SHARED SUB-COMPONENTS
   ═══════════════════════════════════════════════════════════════ */
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
  const isOG = evt.eventType === "OwnGoal";
  const icon = eventIcon(evt.eventType);
  return (
    <div className="flex items-center px-2 py-1.5 border-b border-[var(--sc-border)] last:border-b-0">
      {/* Home side */}
      <div className="flex-1 text-right pr-1.5">
        {evt.isHome && (
          isSub ? (
            <><div className="text-[11px] font-semibold text-green-600">{evt.playerName}</div><div className="text-[11px] text-red-500">{evt.playerOff}</div></>
          ) : (
            <>
              <div className={`text-xs font-semibold ${isOG ? "text-red-500" : ""}`}>
                {evt.playerName}
                {evt.runningScore && <span className="text-[var(--sc-text-secondary)] text-[10px] ml-0.5">({evt.runningScore.split(" - ").map((s, i) => i === 0 ? <strong key={i}>{s}</strong> : <span key={i}> - {s}</span>)})</span>}
              </div>
              {isOG && <div className="text-[10px] text-[var(--sc-text-secondary)]">Own goal</div>}
              {evt.eventType === "PenaltyGoal" && <div className="text-[10px] text-[var(--sc-text-secondary)]">Penalty</div>}
              {evt.assistName && <div className="text-[10px] text-[var(--sc-text-secondary)]">{evt.assistName}</div>}
            </>
          )
        )}
      </div>
      {/* Center */}
      <div className="flex flex-col items-center min-w-[44px]">
        <span className="text-sm">{isOG ? <span className="text-red-500">{icon}</span> : icon}</span>
        <span className="text-[11px] font-extrabold">{evt.minute}</span>
      </div>
      {/* Away side */}
      <div className="flex-1 pl-1.5">
        {!evt.isHome && (
          isSub ? (
            <><div className="text-[11px] font-semibold text-green-600">{evt.playerName}</div><div className="text-[11px] text-red-500">{evt.playerOff}</div></>
          ) : (
            <>
              <div className={`text-xs font-semibold ${isOG ? "text-red-500" : ""}`}>
                {evt.playerName}
                {evt.runningScore && <span className="text-[var(--sc-text-secondary)] text-[10px] ml-0.5">({evt.runningScore.split(" - ").map((s, i) => i === 1 ? <strong key={i}> - {s}</strong> : <span key={i}>{s}</span>)})</span>}
              </div>
              {isOG && <div className="text-[10px] text-[var(--sc-text-secondary)]">Own goal</div>}
              {evt.eventType === "PenaltyGoal" && <div className="text-[10px] text-[var(--sc-text-secondary)]">Penalty</div>}
              {evt.assistName && <div className="text-[10px] text-[var(--sc-text-secondary)]">{evt.assistName}</div>}
            </>
          )
        )}
      </div>
    </div>
  );
}
