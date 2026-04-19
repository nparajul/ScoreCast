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

  useEffect(() => {
    (async () => {
      const r = await api.getCompetitions();
      if (r.success && r.data) {
        setCompetitions(r.data);
        const first = r.data[0];
        if (first) { setSelected(first); await loadInsights(first); }
      }
    })();
  }, []);

  const loadInsights = async (comp: CompetitionResult) => {
    setLoaded(false);
    setInsights([]);
    const seasons = await api.getSeasons(comp.code);
    const current = seasons.data?.find(s => s.isCurrent);
    if (!current) { setLoaded(true); return; }
    const gw = await api.getGameweekMatches(current.id, 0);
    if (!gw.success || !gw.data) { setLoaded(true); return; }
    const gwNumber = gw.data.gameweekNumber;
    const resp = await api.getMatchInsights(current.id, gwNumber);
    if (resp.success && resp.data && resp.data.length > 0) {
      setInsights(resp.data);
      setGwNum(gwNumber);
    } else {
      const next = await api.getMatchInsights(current.id, gwNumber + 1);
      if (next.success && next.data && next.data.length > 0) {
        setInsights(next.data);
        setGwNum(gwNumber + 1);
      } else {
        setGwNum(gwNumber);
      }
    }
    setLoaded(true);
  };

  const onCompChange = async (comp: CompetitionResult) => {
    setSelected(comp);
    await loadInsights(comp);
  };

  return (
    <div className="text-white">
      <div className="sticky top-0 z-10 bg-[#0A1929] border-b border-white/10 px-4 py-3">
        <h1 className="text-lg font-extrabold mb-2">✨ AI Insights</h1>
        {competitions.length > 1 && (
          <div className="flex gap-2 overflow-x-auto">
            {competitions.map(c => (
              <button key={c.code} onClick={() => onCompChange(c)}
                className={`px-3 py-1 rounded-full text-xs font-bold whitespace-nowrap ${c.code === selected?.code ? "bg-[#FF6B35]" : "border border-white/20 opacity-70"}`}>
                {c.logoUrl && <img src={c.logoUrl} alt="" className="w-4 h-4 inline mr-1 object-contain" />}
                {c.name}
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="max-w-2xl mx-auto p-4">
        {!loaded ? (
          <p className="text-center py-8 opacity-60">Loading...</p>
        ) : insights.length === 0 ? (
          <p className="text-center py-8 opacity-60">No upcoming matches to analyze.</p>
        ) : (
          <div className="rounded-xl border border-white/10 overflow-hidden">
            <div className="px-4 py-2 bg-white/5 flex items-center gap-2">
              {selected?.logoUrl && <img src={selected.logoUrl} alt="" className="w-5 h-5 object-contain" />}
              <span className="font-bold flex-1">Matchweek {gwNum}</span>
              <span className="text-xs opacity-60">{insights.length} matches</span>
            </div>
            {insights.map(m => (
              <Link key={m.matchId} href={`/matches/${m.matchId}`} className="block border-t border-white/10 px-4 py-3 hover:bg-white/5">
                <div className="flex items-center justify-between text-sm mb-1">
                  <span className="flex items-center gap-1">
                    {m.homeTeamLogo && <img src={m.homeTeamLogo} alt="" className="w-5 h-5 object-contain" />}
                    <span className="font-bold">{m.homeTeamShortName ?? m.homeTeamName}</span>
                  </span>
                  <span className="text-xs opacity-60">vs</span>
                  <span className="flex items-center gap-1">
                    <span className="font-bold">{m.awayTeamShortName ?? m.awayTeamName}</span>
                    {m.awayTeamLogo && <img src={m.awayTeamLogo} alt="" className="w-5 h-5 object-contain" />}
                  </span>
                </div>
                {/* Probability bar */}
                <div className="flex h-1.5 rounded-full overflow-hidden gap-px mb-1">
                  <div style={{ width: `${m.homeWinPct}%` }} className="bg-[#FF6B35] rounded-l-full" />
                  <div style={{ width: `${m.drawPct}%` }} className="bg-white/20" />
                  <div style={{ width: `${m.awayWinPct}%` }} className="bg-[#0A1929] rounded-r-full" />
                </div>
                <div className="flex justify-between text-[11px]">
                  <span className="text-[#FF6B35] font-bold">{m.homeWinPct}%</span>
                  <span className="opacity-60">{m.drawPct}%</span>
                  <span className="text-[#0A1929] font-bold">{m.awayWinPct}%</span>
                </div>
                {m.homeXg != null && m.awayXg != null && (
                  <div className="flex justify-between text-[11px] mt-1">
                    <span className="font-semibold">xG {m.homeXg}</span>
                    {m.topScoreline && <span className="opacity-60">likely <strong>{m.topScoreline}</strong> ({m.topScorelinePct}%)</span>}
                    <span className="font-semibold">xG {m.awayXg}</span>
                  </div>
                )}
                {m.aiSummary && <p className="text-xs italic opacity-70 mt-1">🔥 {m.aiSummary}</p>}
                {m.kickoffTime && <p className="text-[10px] opacity-50 mt-1">{new Date(m.kickoffTime).toLocaleString("en-GB", { weekday: "short", day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" })}</p>}
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
