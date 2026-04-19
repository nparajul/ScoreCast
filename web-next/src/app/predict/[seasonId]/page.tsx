"use client";

import { api } from "@/lib/api";
import type { GameweekMatchesResult, MatchDetail, MyPredictionResult, RiskPlayResult, ScoringRuleResult, RiskPlayType } from "@/lib/types";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";

const APP_NAME = "PREDICT GAMEWEEK";

interface PredMatch extends MatchDetail {
  predHome: number | null;
  predAway: number | null;
  hasSaved: boolean;
  outcome?: string;
}

interface RiskPlay {
  riskType: RiskPlayType;
  matchId?: number;
  selection?: string;
  bonusPoints?: number;
  isWon?: boolean;
  isResolved?: boolean;
}

const RISK_META: Record<RiskPlayType, { label: string; icon: string; desc: string; maxBonus: number | "max" }> = {
  DoubleDown: { label: "Double Down", icon: "⚡", desc: "Double your points on one match. Wrong = -5", maxBonus: "max" },
  ExactScoreBoost: { label: "Exact Score Boost", icon: "🎯", desc: "Confident in exact score? +15 bonus, wrong = -5", maxBonus: 15 },
  CleanSheetBet: { label: "Clean Sheet Bet", icon: "🧤", desc: "Bet a team keeps a clean sheet. +5 / -3", maxBonus: 5 },
  FirstGoalTeam: { label: "First Goal Team", icon: "⚽", desc: "Pick which team scores first. +3 / -2", maxBonus: 3 },
  OverUnderGoals: { label: "Over/Under 2.5", icon: "📊", desc: "Over or under 2.5 total goals. +3 / -2", maxBonus: 3 },
};

const OUTCOME_CSS: Record<string, string> = {
  ExactScore: "bg-green-800/25",
  CorrectResultAndGoalDifference: "bg-teal-700/25",
  CorrectResult: "bg-amber-500/25",
  CorrectGoalDifference: "bg-purple-600/20",
  Incorrect: "bg-red-700/20",
};

const isLocked = (m: PredMatch) =>
  m.status === "Finished" || m.status === "Live" || m.status === "InPlay" ||
  m.status === "Postponed" || m.status === "Cancelled" ||
  (m.kickoffTime && new Date(m.kickoffTime) <= new Date());

const isPostponed = (m: PredMatch) => m.status === "Postponed" || m.status === "Cancelled";

export default function PredictPage() {
  const { seasonId } = useParams<{ seasonId: string }>();
  const sid = Number(seasonId);
  const [gw, setGw] = useState<GameweekMatchesResult | null>(null);
  const [matches, setMatches] = useState<PredMatch[]>([]);
  const [rules, setRules] = useState<ScoringRuleResult[]>([]);
  const [riskPlays, setRiskPlays] = useState<RiskPlay[]>([]);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [showBreakdown, setShowBreakdown] = useState(false);
  const [showRules, setShowRules] = useState(false);
  const [showRisk, setShowRisk] = useState(false);
  const [expandedRules, setExpandedRules] = useState<Set<string>>(new Set());
  const listRef = useRef<HTMLDivElement>(null);

  const allLocked = matches.every(isLocked);
  const allFinished = matches.every((m) => m.status === "Finished" || isPostponed(m));
  const hasPredictions = matches.some((m) => m.predHome != null && m.predAway != null);
  const showPoints = allLocked && hasPredictions;
  const showTotal = allFinished && hasPredictions;

  const initRiskPlays = (saved?: RiskPlayResult[]) => {
    const types: RiskPlayType[] = ["DoubleDown", "ExactScoreBoost", "CleanSheetBet", "FirstGoalTeam", "OverUnderGoals"];
    const plays: RiskPlay[] = types.map((t) => ({ riskType: t }));
    if (saved) {
      for (const s of saved) {
        const rp = plays.find((p) => p.riskType === s.riskType);
        if (rp) Object.assign(rp, { matchId: s.matchId, selection: s.selection, bonusPoints: s.bonusPoints, isWon: s.isWon, isResolved: s.isResolved });
      }
    }
    setRiskPlays(plays);
    if (saved?.some((s) => s.matchId)) setShowRisk(true);
  };

  const load = useCallback(async (gwNum: number) => {
    const [res, rulesRes] = await Promise.all([
      api.getGameweekMatches(sid, gwNum),
      rules.length ? Promise.resolve(null) : api.getScoringRules(),
    ]);
    if (!res.success || !res.data) return;
    if (rulesRes?.success && rulesRes.data) setRules(rulesRes.data);
    const g = res.data;
    setGw(g);
    const [preds, riskRes] = await Promise.all([
      api.getMyPredictions(sid, g.gameweekId),
      api.getMyRiskPlays(sid, g.gameweekId),
    ]);
    const predMap = new Map<number, MyPredictionResult>();
    if (preds.success && preds.data) preds.data.forEach((p) => predMap.set(p.matchId, p));
    setMatches(
      g.matches.map((m) => {
        const p = predMap.get(m.matchId);
        return { ...m, predHome: p?.predictedHomeScore ?? null, predAway: p?.predictedAwayScore ?? null, hasSaved: !!p, outcome: p?.outcome ?? undefined };
      })
    );
    initRiskPlays(riskRes.success ? riskRes.data ?? undefined : undefined);
    setSaved(false);
  }, [sid, rules.length]);

  useEffect(() => { load(0); }, [load]);

  // Auto-scroll to first unpredicted match
  useEffect(() => {
    if (!listRef.current || !matches.length) return;
    const idx = matches.findIndex((m) => !isLocked(m) && m.predHome == null);
    if (idx < 0) return;
    const el = listRef.current.children[idx] as HTMLElement;
    el?.scrollIntoView({ behavior: "smooth", block: "center" });
  }, [matches.length > 0 && !matches[0]?.hasSaved]);

  const update = (idx: number, home: boolean, delta: number) => {
    setMatches((prev) => prev.map((m, i) => {
      if (i !== idx || isLocked(m)) return m;
      const key = home ? "predHome" : "predAway";
      const cur = m[key] ?? 0;
      return { ...m, [key]: Math.max(0, cur + delta) };
    }));
  };

  const matchLabel = (id: number) => {
    const m = matches.find((x) => x.matchId === id);
    return m ? `${m.homeTeamShortName || m.homeTeamName} vs ${m.awayTeamShortName || m.awayTeamName}` : "";
  };

  const teamName = (m: PredMatch, teamId: string) =>
    String(m.homeTeamId) === teamId ? (m.homeTeamShortName || m.homeTeamName) : (m.awayTeamShortName || m.awayTeamName);

  const save = async () => {
    if (!gw) return;
    const unlocked = matches.filter((m) => !isLocked(m) && !isPostponed(m));
    const missing = unlocked.filter((m) => m.predHome == null || m.predAway == null);
    if (missing.length) return alert(`Please enter predictions for all matches (${missing.length} remaining)`);
    setSaving(true);
    const predictions = unlocked.filter((m) => m.predHome != null && m.predAway != null)
      .map((m) => ({ matchId: m.matchId, predictedHomeScore: m.predHome!, predictedAwayScore: m.predAway! }));
    const res = await api.submitPredictions({ seasonId: sid, predictions, appName: APP_NAME } as never);
    if (res.success) {
      setMatches((prev) => prev.map((m) => isLocked(m) ? m : { ...m, hasSaved: true }));
      // Save risk plays
      const activeRisks = riskPlays.filter((r) => r.matchId);
      if (activeRisks.length) {
        await api.submitRiskPlays({ seasonId: sid, riskPlays: activeRisks.map((r) => ({ matchId: r.matchId!, riskType: r.riskType, selection: r.selection })), appName: APP_NAME });
      }
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    }
    setSaving(false);
  };

  // Scoring breakdown
  const getBreakdown = () => {
    const predicted = matches.filter((m) => m.predHome != null && m.predAway != null && m.outcome);
    return rules.map((rule) => {
      const matching = predicted.filter((m) => m.outcome === rule.outcome);
      return { label: rule.description || rule.outcome, points: rule.points, count: matching.length, total: matching.length * rule.points, outcome: rule.outcome, matches: matching };
    });
  };

  const maxRulePoints = rules.length ? Math.max(...rules.map((r) => r.points)) : 0;
  const breakdown = showTotal ? getBreakdown() : [];
  const predTotal = breakdown.reduce((s, b) => s + b.total, 0);
  const riskBonus = riskPlays.filter((r) => r.isResolved && r.bonusPoints != null).reduce((s, r) => s + r.bonusPoints!, 0);
  const pendingRisk = riskPlays.filter((r) => !r.isResolved && r.bonusPoints != null).reduce((s, r) => s + r.bonusPoints!, 0);
  const grandTotal = predTotal + riskBonus;
  const totalPossible = matches.filter((m) => m.status === "Finished" && m.predHome != null).length * maxRulePoints;
  const maxRiskBonus = riskPlays.reduce((s, r) => s + (RISK_META[r.riskType].maxBonus === "max" ? maxRulePoints : (RISK_META[r.riskType].maxBonus as number)), 0);

  const deadline = gw?.matches
    .filter((m) => m.kickoffTime && new Date(m.kickoffTime) > new Date())
    .sort((a, b) => new Date(a.kickoffTime!).getTime() - new Date(b.kickoffTime!).getTime())[0]?.kickoffTime;

  const toggleRule = (label: string) => setExpandedRules((prev) => {
    const next = new Set(prev);
    next.has(label) ? next.delete(label) : next.add(label);
    return next;
  });

  const unlockedMatches = matches.filter((m) => !isLocked(m));

  const updateRisk = (idx: number, patch: Partial<RiskPlay>) =>
    setRiskPlays((prev) => prev.map((r, i) => i === idx ? { ...r, ...patch } : r));

  const riskStatusColor = (r: RiskPlay) =>
    r.isResolved ? (r.isWon ? "text-green-500" : "text-red-500") : r.bonusPoints != null ? "text-orange-400" : "text-[var(--sc-text-secondary)]";

  return (
    <div className="py-4 max-w-xl mx-auto px-2">
      <h1 className="text-xl font-bold mb-1">Make Predictions</h1>
      <p className="text-sm text-[var(--sc-text-secondary)] mb-4">Predict the score for each match</p>

      {gw && (
        <>
          {/* Status alerts */}
          {allLocked && !matches.some((m) => m.hasSaved) && (
            <div className="mb-2 p-2 rounded text-sm bg-red-900/30 text-red-400">You missed this gameweek! No predictions were submitted before the deadline.</div>
          )}
          {!allLocked && matches.some((m) => m.hasSaved) && (
            <div className="mb-2 p-2 rounded text-sm bg-blue-900/30 text-blue-400">You&apos;ve already submitted predictions. You can still edit before kickoff.</div>
          )}
          {!allLocked && !matches.some((m) => m.hasSaved) && (
            <div className="mb-2 p-2 rounded text-sm bg-yellow-900/30 text-yellow-400">No predictions made yet. Enter your scores below!</div>
          )}

          {/* Gameweek nav */}
          <div className="flex items-center justify-center gap-4 mb-4">
            <button disabled={gw.gameweekNumber <= 1} onClick={() => load(gw.gameweekNumber - 1)} className="text-2xl font-bold disabled:opacity-30 px-2">‹</button>
            <span className="text-lg font-bold">Gameweek {gw.gameweekNumber}</span>
            <button disabled={gw.gameweekNumber >= gw.currentGameweek} onClick={() => load(gw.gameweekNumber + 1)} className="text-2xl font-bold disabled:opacity-30 px-2">›</button>
          </div>

          {/* Deadline */}
          {deadline && (
            <div className="text-center text-xs mb-3 text-[var(--sc-text-secondary)]">
              ⏰ Next kickoff: {new Date(deadline).toLocaleString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" })}
            </div>
          )}

          {/* Points summary */}
          {showTotal && (
            <div className="rounded-lg border border-[var(--sc-border)] p-3 mb-3 text-center" style={{ background: "var(--sc-surface)" }}>
              <div className="text-3xl font-extrabold">{grandTotal}</div>
              {pendingRisk !== 0 && <div className="text-xs font-semibold text-orange-400">{pendingRisk > 0 ? "+" : ""}{pendingRisk} pending</div>}
              <div className="text-xs text-[var(--sc-text-secondary)]">out of {totalPossible} + {maxRiskBonus} possible</div>
              <button onClick={() => setShowBreakdown(!showBreakdown)} className="mt-2 text-sm text-[var(--sc-primary)] font-semibold">
                {showBreakdown ? "Hide" : "Show"} Breakdown ▾
              </button>
            </div>
          )}

          {/* Points breakdown */}
          {showBreakdown && showTotal && (
            <div className="rounded-lg border border-[var(--sc-border)] p-3 mb-3" style={{ background: "var(--sc-surface)" }}>
              <div className="text-sm font-bold mb-2">Points Breakdown</div>
              {breakdown.map((row) => (
                <div key={row.outcome} className={`rounded-md mb-1 ${OUTCOME_CSS[row.outcome] || "bg-gray-500/15"}`}>
                  <div className="flex items-center justify-between px-2 py-1.5 cursor-pointer" onClick={() => row.count > 0 && toggleRule(row.label)}>
                    <span className="text-[13px]">{row.label}</span>
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-[var(--sc-text-secondary)]">{row.points} × {row.count}</span>
                      <span className="font-bold text-[13px] min-w-[28px] text-right">{row.total}</span>
                      {row.count > 0 && <span className="text-xs">{expandedRules.has(row.label) ? "▴" : "▾"}</span>}
                    </div>
                  </div>
                  {row.count > 0 && expandedRules.has(row.label) && (
                    <div className="px-2 pb-1.5">
                      {row.matches.map((m) => (
                        <div key={m.matchId} className="flex items-center justify-between text-xs py-0.5 border-t border-white/10">
                          <div className="flex items-center gap-1 flex-1 min-w-0">
                            {m.homeTeamLogoUrl && <img src={m.homeTeamLogoUrl} alt="" className="w-3.5 h-3.5" />}
                            <span className="truncate">{m.homeTeamShortName} vs {m.awayTeamShortName}</span>
                          </div>
                          <div className="flex items-center gap-2 shrink-0">
                            <span className="text-[var(--sc-text-secondary)]">{m.predHome}-{m.predAway}</span>
                            <span className="font-semibold">({m.homeScore}-{m.awayScore})</span>
                            <span className="font-bold text-[var(--sc-primary)]">+{row.points}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
              {/* Risk plays in breakdown */}
              {riskPlays.some((r) => r.bonusPoints != null) && (
                <div className="mt-1.5 pt-1.5 border-t border-dashed border-[var(--sc-border)]">
                  <div className="text-xs font-bold px-2 pb-1 text-[var(--sc-text-secondary)]">🎲 Risk Plays</div>
                  {riskPlays.filter((r) => r.bonusPoints != null).map((rp) => (
                    <div key={rp.riskType} className="flex items-center justify-between px-2 py-1 text-xs">
                      <div className="flex items-center gap-1">
                        <span>{RISK_META[rp.riskType].icon}</span>
                        <span>{RISK_META[rp.riskType].label}</span>
                        {rp.matchId && <span className="text-[var(--sc-text-secondary)]">· {matchLabel(rp.matchId)}</span>}
                      </div>
                      <span className={`font-bold ${riskStatusColor(rp)}`}>
                        {rp.bonusPoints! > 0 ? "+" : ""}{rp.bonusPoints}
                        {!rp.isResolved && <span className="text-[10px] opacity-70"> (live)</span>}
                      </span>
                    </div>
                  ))}
                </div>
              )}
              <div className="border-t-2 border-[var(--sc-border)] pt-1.5 mt-1 px-2 flex justify-between">
                <span className="font-bold">Total</span>
                <span className="font-bold text-base">{grandTotal} / {totalPossible} + {maxRiskBonus}</span>
              </div>
            </div>
          )}

          {/* Scoring Rules drawer */}
          {rules.length > 0 && (
            <div className="mb-3">
              <button onClick={() => setShowRules(!showRules)} className="w-full flex items-center justify-between px-3 py-2 rounded-lg border border-[var(--sc-border)] text-sm font-semibold" style={{ background: "var(--sc-surface)" }}>
                <span>📋 Scoring Rules</span>
                <span>{showRules ? "▴" : "▾"}</span>
              </button>
              {showRules && (
                <div className="border border-t-0 border-[var(--sc-border)] rounded-b-lg overflow-hidden" style={{ background: "var(--sc-surface)" }}>
                  {rules.map((r) => (
                    <div key={r.outcome} className="flex items-center justify-between px-3 py-2 border-b border-[var(--sc-border)] last:border-0">
                      <div>
                        <div className="text-sm font-semibold">{r.description}</div>
                      </div>
                      <span className="font-bold text-[var(--sc-primary)]">+{r.points}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Match list */}
          <div ref={listRef} className="rounded-lg border border-[var(--sc-border)] overflow-hidden divide-y divide-[var(--sc-border)]" style={{ maxWidth: 600, margin: "0 auto" }}>
            {matches.map((m, i) => {
              const locked = isLocked(m);
              const pp = isPostponed(m);
              const pts = showPoints && m.status === "Finished" && m.outcome ? rules.find((r) => r.outcome === m.outcome)?.points ?? 0 : null;
              return (
                <div key={m.matchId} className={`px-2 py-2.5 relative ${pp ? "opacity-45" : ""} ${m.outcome ? OUTCOME_CSS[m.outcome] || "" : ""}`}>
                  <div className="flex items-center">
                    <div className="flex-1 flex items-center justify-end gap-1 min-w-0">
                      <span className={`text-xs sm:text-sm font-semibold truncate text-right ${pp ? "line-through" : ""}`}>{m.homeTeamShortName || m.homeTeamName}</span>
                      {(m.homeTeamLogoUrl || m.homeTeamLogo) && <img src={m.homeTeamLogoUrl || m.homeTeamLogo} alt="" className="w-5 h-5 sm:w-[22px] sm:h-[22px] shrink-0" />}
                    </div>
                    {pp ? (
                      <div className="shrink-0 px-2"><span className="font-extrabold text-[13px] text-red-500">PP</span></div>
                    ) : locked ? (
                      <div className="flex items-center justify-center shrink-0 px-0.5 gap-1">
                        <span className="font-bold text-[15px] opacity-45 w-7 text-center">{m.predHome ?? "–"}</span>
                        <span className="font-bold text-[11px]">-</span>
                        <span className="font-bold text-[15px] opacity-45 w-7 text-center">{m.predAway ?? "–"}</span>
                        <span className="ml-1 text-[10px] opacity-40">🔒</span>
                      </div>
                    ) : (
                      <div className="flex items-center justify-center shrink-0 px-0.5 gap-1">
                        <div className="flex flex-col items-center w-[38px] rounded-lg border border-[var(--sc-border)] overflow-hidden select-none">
                          <button onClick={() => update(i, true, 1)} className="w-full h-[22px] text-[13px] font-bold bg-green-600 text-white">+</button>
                          <div className="text-lg font-bold py-0.5 w-full text-center" style={{ background: "var(--sc-surface)" }}>{m.predHome ?? "–"}</div>
                          <button onClick={() => update(i, true, -1)} className="w-full h-[22px] text-[13px] font-bold bg-red-600 text-white">−</button>
                        </div>
                        <span className="text-[11px] font-bold">-</span>
                        <div className="flex flex-col items-center w-[38px] rounded-lg border border-[var(--sc-border)] overflow-hidden select-none">
                          <button onClick={() => update(i, false, 1)} className="w-full h-[22px] text-[13px] font-bold bg-green-600 text-white">+</button>
                          <div className="text-lg font-bold py-0.5 w-full text-center" style={{ background: "var(--sc-surface)" }}>{m.predAway ?? "–"}</div>
                          <button onClick={() => update(i, false, -1)} className="w-full h-[22px] text-[13px] font-bold bg-red-600 text-white">−</button>
                        </div>
                      </div>
                    )}
                    <div className="flex-1 flex items-center gap-1 min-w-0">
                      {(m.awayTeamLogoUrl || m.awayTeamLogo) && <img src={m.awayTeamLogoUrl || m.awayTeamLogo} alt="" className="w-5 h-5 sm:w-[22px] sm:h-[22px] shrink-0" />}
                      <span className={`text-xs sm:text-sm font-semibold truncate ${pp ? "line-through" : ""}`}>{m.awayTeamShortName || m.awayTeamName}</span>
                    </div>
                    {pts != null && <span className={`absolute right-1.5 top-2 text-xs font-extrabold ${pts > 0 ? "text-green-500" : "text-red-500"}`}>+{pts}</span>}
                  </div>
                  <div className="text-center text-[11px] font-semibold text-[var(--sc-text-secondary)] mt-0.5">
                    {pp ? <span className="text-red-500">Postponed</span>
                      : m.status === "Finished" ? `FT ${m.homeScore}-${m.awayScore}`
                      : m.kickoffTime ? new Date(m.kickoffTime).toLocaleString(undefined, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" }) : ""}
                  </div>
                </div>
              );
            })}
          </div>

          {/* Risk Plays */}
          {riskPlays.length > 0 && (
            <div className="mt-3" style={{ maxWidth: 600, margin: "12px auto 0" }}>
              <button onClick={() => setShowRisk(!showRisk)}
                className="w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-white"
                style={{ background: "linear-gradient(135deg,#7c4dff,#651fff)" }}>
                <div className="flex items-center gap-2">
                  <span className="text-lg">🎲</span>
                  <div className="text-left">
                    <div className="font-bold text-sm">Risk Plays</div>
                    <div className="text-[11px] opacity-80">
                      {allLocked
                        ? `${riskPlays.filter((r) => r.isWon).length} won · ${riskPlays.filter((r) => r.isWon === false).length} lost`
                        : `${riskPlays.filter((r) => r.matchId).length} active`}
                    </div>
                  </div>
                </div>
                <span>{showRisk ? "▴" : "▾"}</span>
              </button>

              {showRisk && (
                <div className="border border-t-0 border-[var(--sc-border)] rounded-b-lg overflow-hidden">
                  {riskPlays.map((rp, ri) => {
                    const meta = RISK_META[rp.riskType];
                    const rpMatch = rp.matchId ? matches.find((x) => x.matchId === rp.matchId) : null;
                    const rpLocked = rpMatch ? isLocked(rpMatch) : false;
                    const editable = !rpLocked && !allLocked;
                    return (
                      <div key={rp.riskType} className="px-3 py-2.5 border-b border-[var(--sc-border)]">
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center gap-1">
                            <span>{meta.icon}</span>
                            <span className="font-bold text-[13px]">{meta.label}</span>
                          </div>
                          {rp.bonusPoints != null && (
                            <span className={`font-bold text-[13px] ${riskStatusColor(rp)}`}>
                              {rp.bonusPoints > 0 ? "+" : ""}{rp.bonusPoints}
                              {!rp.isResolved && <span className="text-[10px] opacity-70"> (live)</span>}
                            </span>
                          )}
                        </div>
                        <div className="text-[11px] text-[var(--sc-text-secondary)] mb-1.5">{meta.desc}</div>

                        {editable ? (
                          <>
                            <select value={rp.matchId ?? ""} onChange={(e) => updateRisk(ri, { matchId: e.target.value ? Number(e.target.value) : undefined, selection: undefined })}
                              className="w-full text-xs p-1.5 rounded border border-[var(--sc-border)] mb-1" style={{ background: "var(--sc-surface)" }}>
                              <option value="">— Select match —</option>
                              {unlockedMatches.map((m) => (
                                <option key={m.matchId} value={m.matchId}>{m.homeTeamShortName || m.homeTeamName} vs {m.awayTeamShortName || m.awayTeamName}</option>
                              ))}
                            </select>
                            {rp.matchId && rpMatch && (rp.riskType === "CleanSheetBet" || rp.riskType === "FirstGoalTeam") && (
                              <select value={rp.selection ?? ""} onChange={(e) => updateRisk(ri, { selection: e.target.value || undefined })}
                                className="w-full text-xs p-1.5 rounded border border-[var(--sc-border)]" style={{ background: "var(--sc-surface)" }}>
                                <option value="">— Pick team —</option>
                                <option value={String(rpMatch.homeTeamId)}>{rpMatch.homeTeamShortName || rpMatch.homeTeamName}</option>
                                <option value={String(rpMatch.awayTeamId)}>{rpMatch.awayTeamShortName || rpMatch.awayTeamName}</option>
                              </select>
                            )}
                            {rp.matchId && rp.riskType === "OverUnderGoals" && (
                              <select value={rp.selection ?? ""} onChange={(e) => updateRisk(ri, { selection: e.target.value || undefined })}
                                className="w-full text-xs p-1.5 rounded border border-[var(--sc-border)]" style={{ background: "var(--sc-surface)" }}>
                                <option value="">— Pick —</option>
                                <option value="Over">Over 2.5 goals</option>
                                <option value="Under">Under 2.5 goals</option>
                              </select>
                            )}
                          </>
                        ) : rp.matchId ? (
                          <div className="text-xs font-semibold">
                            {matchLabel(rp.matchId)}
                            {rp.selection && rpMatch && (rp.riskType === "CleanSheetBet" || rp.riskType === "FirstGoalTeam") && (
                              <span className="text-[var(--sc-text-secondary)]"> · {teamName(rpMatch, rp.selection)}</span>
                            )}
                            {rp.selection && rp.riskType === "OverUnderGoals" && (
                              <span className="text-[var(--sc-text-secondary)]"> · {rp.selection} 2.5</span>
                            )}
                          </div>
                        ) : null}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}

          {/* Save button */}
          {matches.some((m) => !isLocked(m)) && (
            <div className="flex justify-center mt-4">
              <button onClick={save} disabled={saving}
                className="px-6 py-2 rounded-full text-sm font-bold text-white disabled:opacity-50 transition-all"
                style={{ background: saved ? "#4CAF50" : "var(--sc-tertiary)" }}>
                {saving ? "Saving…" : saved ? "✓ Saved!" : "Save Predictions"}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
