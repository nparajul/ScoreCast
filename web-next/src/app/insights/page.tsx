"use client";

import { api } from "@/lib/api";
import { CompetitionResult, MatchInsightResult } from "@/lib/types";
import { useEffect, useState } from "react";
import Link from "next/link";

export default function InsightsPage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [selected, setSelected] = useState<CompetitionResult | null>(null);
  const [insights, setInsights] = useState<MatchInsightResult[]>([]);
  const [gwNum, setGwNum] = useState(0);
  const [loaded, setLoaded] = useState(false);
  const [expanded, setExpanded] = useState<Set<number>>(new Set());

  useEffect(() => {
    (async () => {
      const [r, d] = await Promise.all([api.getCompetitions(), api.getDefaultCompetition()]);
      if (r.success && r.data) {
        setCompetitions(r.data);
        const defaultCode = d.success && d.data ? d.data.code : null;
        const first = r.data.find((c) => c.code === defaultCode) ?? r.data[0];
        if (first) { setSelected(first); await loadInsights(first); }
      }
    })();
  }, []);

  const loadInsights = async (comp: CompetitionResult) => {
    setLoaded(false); setInsights([]);
    const seasons = await api.getSeasons(comp.code);
    const current = seasons.data?.find((s) => s.isCurrent);
    if (!current) { setLoaded(true); return; }
    const gw = await api.getGameweekMatches(current.id, 0);
    if (!gw.success || !gw.data) { setLoaded(true); return; }
    const gwNumber = gw.data.currentGameweek;
    const resp = await api.getMatchInsights(current.id, gwNumber);
    if (resp.success && resp.data?.length) {
      setInsights(resp.data); setGwNum(gwNumber);
    } else {
      const next = await api.getMatchInsights(current.id, gwNumber + 1);
      setInsights(next.success && next.data?.length ? next.data : []);
      setGwNum(next.success && next.data?.length ? gwNumber + 1 : gwNumber);
    }
    setLoaded(true);
  };

  const toggle = (id: number) => setExpanded((s) => { const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n; });

  return (
    <div>
      <div className="sticky top-0 z-10 bg-[var(--sc-bg)] border-b border-[var(--sc-border)] px-4 py-3">
        <h1 className="text-lg font-extrabold mb-2">✨ AI Insights</h1>
        {competitions.length > 1 && (
          <div className="flex gap-2 overflow-x-auto">
            {competitions.map((c) => (
              <button key={c.code} onClick={() => { setSelected(c); loadInsights(c); }}
                className={`px-3 py-1 rounded-full text-xs font-bold whitespace-nowrap transition-colors ${c.code === selected?.code ? "bg-[var(--sc-tertiary)] text-white" : "border border-[var(--sc-border)] opacity-70"}`}>
                {c.logoUrl && <img src={c.logoUrl} alt="" className="w-4 h-4 inline mr-1 object-contain" />}
                {c.name}
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="max-w-2xl mx-auto p-4">
        {!loaded ? (
          <div className="text-center py-8"><div className="text-4xl animate-pulse">⚽</div></div>
        ) : insights.length === 0 ? (
          <p className="text-center py-8 text-[var(--sc-text-secondary)]">No upcoming matches to analyze.</p>
        ) : (
          <div className="rounded-xl border border-[var(--sc-border)] overflow-hidden bg-[var(--sc-surface)]">
            <div className="px-4 py-2 bg-gray-50 flex items-center gap-2">
              {selected?.logoUrl && <img src={selected.logoUrl} alt="" className="w-5 h-5 object-contain" />}
              <span className="font-bold flex-1">Matchweek {gwNum}</span>
              <span className="text-xs text-[var(--sc-text-secondary)]">{insights.length} matches</span>
            </div>
            {insights.map((m) => (
              <div key={m.matchId} className="border-t border-[var(--sc-border)] px-4 py-3">
                <Link href={`/matches/${m.matchId}`} className="block hover:opacity-80">
                  <div className="flex items-center justify-between text-sm mb-1.5">
                    <span className="flex items-center gap-1">
                      {m.homeTeamLogo && <img src={m.homeTeamLogo} alt="" className="w-5 h-5 object-contain" />}
                      <span className="font-bold">{m.homeTeamShortName ?? m.homeTeamName ?? m.homeTeam}</span>
                    </span>
                    <span className="text-xs text-[var(--sc-text-secondary)]">vs</span>
                    <span className="flex items-center gap-1">
                      <span className="font-bold">{m.awayTeamShortName ?? m.awayTeamName ?? m.awayTeam}</span>
                      {m.awayTeamLogo && <img src={m.awayTeamLogo} alt="" className="w-5 h-5 object-contain" />}
                    </span>
                  </div>
                  {/* Probability bar */}
                  <div className="flex h-1.5 rounded-full overflow-hidden gap-px mb-1">
                    <div style={{ width: `${m.homeWinPct ?? m.homeWinProbability ?? 0}%` }} className="bg-[var(--sc-tertiary)] rounded-l-full" />
                    <div style={{ width: `${m.drawPct ?? m.drawProbability ?? 0}%` }} className="bg-gray-300" />
                    <div style={{ width: `${m.awayWinPct ?? m.awayWinProbability ?? 0}%` }} className="bg-[var(--sc-primary)] rounded-r-full" />
                  </div>
                  <div className="flex justify-between text-[11px]">
                    <span className="text-[var(--sc-tertiary)] font-bold">{m.homeWinPct ?? m.homeWinProbability}%</span>
                    <span className="text-[var(--sc-text-secondary)]">{m.drawPct ?? m.drawProbability}%</span>
                    <span className="text-[var(--sc-primary)] font-bold">{m.awayWinPct ?? m.awayWinProbability}%</span>
                  </div>
                  {m.homeXg != null && m.awayXg != null && (
                    <div className="flex justify-between text-[11px] mt-1">
                      <span className="font-semibold">xG {m.homeXg}</span>
                      {m.topScoreline && <span className="text-[var(--sc-text-secondary)]">likely <strong>{m.topScoreline}</strong> ({m.topScorelinePct}%)</span>}
                      <span className="font-semibold">xG {m.awayXg}</span>
                    </div>
                  )}
                  {m.kickoffTime && (
                    <p className="text-[10px] text-[var(--sc-text-secondary)] mt-1">
                      {new Date(m.kickoffTime).toLocaleString("en-GB", { weekday: "short", day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" })}
                    </p>
                  )}
                </Link>
                {/* Expandable AI Summary */}
                {m.aiSummary && (
                  <button onClick={() => toggle(m.matchId)} className="mt-1 text-xs text-[var(--sc-secondary)] font-semibold hover:underline">
                    {expanded.has(m.matchId) ? "Hide AI summary ▲" : "🔥 Show AI summary ▼"}
                  </button>
                )}
                {m.aiSummary && expanded.has(m.matchId) && (
                  <p className="text-xs italic text-[var(--sc-text-secondary)] mt-1 leading-relaxed">{m.aiSummary}</p>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
