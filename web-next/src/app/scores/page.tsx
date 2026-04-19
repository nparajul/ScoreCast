"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "@/lib/api";
import type { GameweekMatchesResult, MatchDetail } from "@/lib/types";
import { CompetitionFilter, type CompetitionFilterState } from "@/components/competition-filter";
import { MatchTile } from "@/components/match-tile";

function dateLabel(dateStr: string): string {
  const d = new Date(dateStr);
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const target = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const diff = (target.getTime() - today.getTime()) / 86400000;
  if (diff === 0) return "Today";
  if (diff === 1) return "Tomorrow";
  if (diff === -1) return "Yesterday";
  return d.toLocaleDateString("en-GB", { weekday: "long", day: "numeric", month: "long" });
}

function groupByDate(matches: MatchDetail[]): Record<string, MatchDetail[]> {
  const groups: Record<string, MatchDetail[]> = {};
  for (const m of matches) {
    const key = m.kickoffTime ? new Date(m.kickoffTime).toDateString() : "TBD";
    (groups[key] ??= []).push(m);
  }
  return groups;
}

export default function ScoresPage() {
  const [filter, setFilter] = useState<CompetitionFilterState>({});
  const [gw, setGw] = useState<GameweekMatchesResult>();
  const [gwNum, setGwNum] = useState(0);
  const [loading, setLoading] = useState(false);
  const pollRef = useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  const load = useCallback(async (seasonId: number, gameweek: number) => {
    setLoading(true);
    const res = await api.getGameweekMatches(seasonId, gameweek);
    if (res.success && res.data) {
      setGw(res.data);
      setGwNum(res.data.gameweekNumber);
    }
    setLoading(false);
  }, []);

  // Load on filter change
  useEffect(() => {
    if (!filter.season) return;
    load(filter.season.id, 0); // 0 = current
  }, [filter.season, load]);

  // 30s polling when live matches exist
  useEffect(() => {
    if (pollRef.current) clearInterval(pollRef.current);
    const hasLive = gw?.matches.some((m) => m.status === "Live");
    if (!hasLive || !filter.season) return;
    pollRef.current = setInterval(() => {
      load(filter.season!.id, gwNum);
    }, 30000);
    return () => clearInterval(pollRef.current);
  }, [gw, filter.season, gwNum, load]);

  const navigate = (dir: -1 | 1) => {
    if (!filter.season || !gw) return;
    const next = gwNum + dir;
    if (next < 1 || next > gw.totalGameweeks) return;
    load(filter.season.id, next);
  };

  const hasLive = gw?.matches.some((m) => m.status === "Live");
  const groups = gw ? groupByDate(gw.matches) : {};

  return (
    <div className="max-w-2xl mx-auto pb-24">
      {/* Sticky header */}
      <div className="sticky top-14 z-10 bg-[#F5F7FA] border-b border-[var(--sc-border)] px-4 py-2.5">
        <div className="flex items-center justify-between gap-3 mb-2">
          <h1 className="text-lg font-extrabold tracking-tight">Scores & Fixtures</h1>
          {hasLive && (
            <span className="flex items-center gap-1.5 text-xs font-bold text-red-500">
              <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" /> LIVE
            </span>
          )}
        </div>
        <div className="flex items-center justify-between gap-3">
          <CompetitionFilter onChange={setFilter} />
          {gw && (
            <div className="flex items-center gap-1">
              <button onClick={() => navigate(-1)} disabled={gwNum <= 1} className="p-1 rounded hover:bg-black/5 disabled:opacity-30">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M15 18l-6-6 6-6"/></svg>
              </button>
              <span className="px-2.5 py-0.5 rounded-full bg-[var(--sc-primary)] text-white text-xs font-bold">MW {gwNum}</span>
              <button onClick={() => navigate(1)} disabled={gwNum >= gw.totalGameweeks} className="p-1 rounded hover:bg-black/5 disabled:opacity-30">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M9 18l6-6-6-6"/></svg>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="px-4 pt-3">
        {loading && !gw && <div className="text-center py-12 text-[var(--sc-text-secondary)] text-sm">Loading...</div>}
        {gw && Object.keys(groups).length === 0 && (
          <div className="text-center py-12 text-[var(--sc-text-secondary)] text-sm">⚽ No matches scheduled</div>
        )}
        {Object.entries(groups).map(([date, matches]) => (
          <div key={date} className="mb-4">
            <div className="text-xs font-bold text-[var(--sc-text-secondary)] uppercase tracking-wide mb-1.5 px-1">
              {dateLabel(matches[0].kickoffTime ?? date)}
            </div>
            <div className="rounded-xl border border-[var(--sc-border)] bg-[var(--sc-surface)] overflow-hidden">
              {matches.map((m) => (
                <MatchTile key={m.matchId} match={m} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
