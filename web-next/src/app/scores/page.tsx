'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '@/lib/api';
import type { CompetitionResult, SeasonResult, GameweekMatchesResult, MatchDetail } from '@/lib/types';
import { MatchTile } from '@/components/match-tile';

function dateLabel(dateStr: string): string {
  const d = new Date(dateStr);
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const target = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const diff = (target.getTime() - today.getTime()) / 86400000;
  if (diff === 0) return 'Today';
  if (diff === 1) return 'Tomorrow';
  if (diff === -1) return 'Yesterday';
  return d.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' });
}

function groupByDate(matches: MatchDetail[]) {
  const groups: { label: string; key: string; matches: MatchDetail[] }[] = [];
  const map = new Map<string, MatchDetail[]>();
  for (const m of matches) {
    const key = m.kickoffTime ? new Date(m.kickoffTime).toDateString() : 'TBD';
    if (!map.has(key)) map.set(key, []);
    map.get(key)!.push(m);
  }
  for (const [key, ms] of map) {
    groups.push({ label: key === 'TBD' ? 'TBD' : dateLabel(ms[0].kickoffTime!), key, matches: ms });
  }
  return groups;
}

interface CompSection {
  comp: CompetitionResult;
  season?: SeasonResult;
  expanded: boolean;
  loading: boolean;
  loaded: boolean;
  gw?: GameweekMatchesResult;
  todayMatches: MatchDetail[];
  lastGwMatches: MatchDetail[];
  nextGwMatches: MatchDetail[];
  lastGwNum: number;
  nextGwNum: number;
}

const STORAGE_KEY = 'sc-scores-comp';

function Skeleton() {
  return (
    <div className="space-y-3 px-4 pt-3">
      {[1, 2, 3].map(i => (
        <div key={i} className="rounded-xl border border-[var(--sc-border)] overflow-hidden">
          <div className="h-12 bg-[var(--sc-surface)] animate-pulse" />
          <div className="space-y-0">
            {[1, 2, 3].map(j => <div key={j} className="h-12 bg-[var(--sc-surface)] animate-pulse border-t border-[var(--sc-border)]" />)}
          </div>
        </div>
      ))}
    </div>
  );
}

export default function ScoresPage() {
  const [sections, setSections] = useState<CompSection[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [selectedComp, setSelectedComp] = useState<string>('');
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const pollRef = useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  const loadSection = useCallback(async (section: CompSection) => {
    if (!section.season || section.loaded) return section;
    section.loading = true;
    const gwRes = await api.getGameweekMatches(section.season.id, 0);
    if (!gwRes.success || !gwRes.data) { section.loading = false; return section; }
    section.gw = gwRes.data;
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today); tomorrow.setDate(tomorrow.getDate() + 1);
    section.todayMatches = gwRes.data.matches.filter(m => {
      if (!m.kickoffTime) return false;
      const d = new Date(m.kickoffTime);
      return d >= today && d < tomorrow;
    });
    if (section.todayMatches.length === 0) {
      const hasFinished = gwRes.data.matches.some(m => m.status === 'Finished');
      if (hasFinished) {
        section.lastGwNum = gwRes.data.gameweekNumber;
        section.lastGwMatches = gwRes.data.matches.filter(m => m.status === 'Finished');
        if (gwRes.data.gameweekNumber < gwRes.data.totalGameweeks) {
          const nextRes = await api.getGameweekMatches(section.season.id, gwRes.data.gameweekNumber + 1);
          if (nextRes.success && nextRes.data) {
            section.nextGwNum = gwRes.data.gameweekNumber + 1;
            section.nextGwMatches = nextRes.data.matches;
          }
        }
      } else {
        section.nextGwNum = gwRes.data.gameweekNumber;
        section.nextGwMatches = gwRes.data.matches;
        if (gwRes.data.gameweekNumber > 1) {
          const prevRes = await api.getGameweekMatches(section.season.id, gwRes.data.gameweekNumber - 1);
          if (prevRes.success && prevRes.data) {
            section.lastGwNum = gwRes.data.gameweekNumber - 1;
            section.lastGwMatches = prevRes.data.matches.filter(m => m.status === 'Finished');
          }
        }
      }
    }
    section.loading = false;
    section.loaded = true;
    return section;
  }, []);

  useEffect(() => {
    (async () => {
      const [defaultRes, compsRes] = await Promise.all([api.getDefaultCompetition(), api.getCompetitions()]);
      const comps = compsRes.data ?? [];
      setCompetitions(comps);
      const defaultCode = defaultRes.data?.code ?? 'PL';
      const saved = typeof window !== 'undefined' ? sessionStorage.getItem(STORAGE_KEY) : null;
      const filterCode = saved || '';
      setSelectedComp(filterCode);

      const defaultComp = comps.find(c => c.code === defaultCode);
      const otherComps = comps.filter(c => c.code !== defaultCode);
      const allSections: CompSection[] = [];

      for (const comp of defaultComp ? [defaultComp, ...otherComps] : otherComps) {
        const sRes = await api.getSeasons(comp.code);
        const season = sRes.data?.find(s => s.isCurrent);
        if (!season) continue;
        allSections.push({
          comp, season, expanded: true, loading: false, loaded: false,
          todayMatches: [], lastGwMatches: [], nextGwMatches: [],
          lastGwNum: 0, nextGwNum: 0,
        });
      }

      const loaded = await Promise.all(allSections.map(s => loadSection({ ...s })));
      setSections(loaded);
      setLoaded(true);
    })();
  }, [loadSection]);

  // Polling for live matches
  useEffect(() => {
    if (pollRef.current) clearInterval(pollRef.current);
    const hasLive = sections.some(s => s.gw?.matches.some(m => m.status === 'Live'));
    if (!hasLive) return;
    pollRef.current = setInterval(async () => {
      const updated = await Promise.all(sections.map(async s => {
        if (!s.season || !s.gw?.matches.some(m => m.status === 'Live')) return s;
        const res = await api.getGameweekMatches(s.season.id, s.gw.gameweekNumber);
        if (res.success && res.data) {
          const today = new Date(); today.setHours(0, 0, 0, 0);
          const tomorrow = new Date(today); tomorrow.setDate(tomorrow.getDate() + 1);
          return {
            ...s, gw: res.data,
            todayMatches: res.data.matches.filter(m => m.kickoffTime && new Date(m.kickoffTime) >= today && new Date(m.kickoffTime) < tomorrow),
          };
        }
        return s;
      }));
      setSections(updated);
    }, 30000);
    return () => clearInterval(pollRef.current);
  }, [sections]);

  const onFilterChange = (code: string) => {
    setSelectedComp(code);
    if (typeof window !== 'undefined') {
      if (code) sessionStorage.setItem(STORAGE_KEY, code);
      else sessionStorage.removeItem(STORAGE_KEY);
    }
  };

  const toggleSection = (idx: number) => {
    setSections(prev => prev.map((s, i) => i === idx ? { ...s, expanded: !s.expanded } : s));
  };

  const filtered = selectedComp ? sections.filter(s => s.comp.code === selectedComp) : sections;
  const hasLive = sections.some(s => s.gw?.matches.some(m => m.status === 'Live'));

  return (
    <div className="max-w-2xl mx-auto pb-24">
      {/* Sticky header */}
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] border-b border-[var(--sc-border)] px-4 py-2.5">
        <div className="flex items-center justify-between gap-3 mb-2">
          <h1 className="text-lg font-extrabold tracking-tight">Scores & Fixtures</h1>
          {hasLive && (
            <span className="flex items-center gap-1.5 text-xs font-bold text-red-500">
              <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" /> LIVE
            </span>
          )}
        </div>
        <select
          value={selectedComp}
          onChange={e => onFilterChange(e.target.value)}
          className="rounded-lg border border-[var(--sc-border)] bg-[var(--sc-surface)] px-3 py-1.5 text-sm font-medium"
        >
          <option value="">All Competitions</option>
          {competitions.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
        </select>
      </div>

      {!loaded ? <Skeleton /> : (
        <div className="px-4 pt-3">
          {filtered.map((section, idx) => (
            <div key={section.comp.id} className="rounded-xl border border-[var(--sc-border)] overflow-hidden mb-3">
              {/* Competition header */}
              <div
                className="flex items-center gap-2.5 px-3 py-2.5 bg-[var(--sc-surface)] cursor-pointer"
                onClick={() => toggleSection(sections.indexOf(section))}
              >
                <a href={`/competitions/${section.comp.id}`} onClick={e => e.stopPropagation()} className="flex items-center gap-2 no-underline text-inherit">
                  {section.comp.logoUrl && <img src={section.comp.logoUrl} alt="" className="w-5 h-5 object-contain" />}
                  <span className="font-bold text-[15px] hover:underline underline-offset-2">{section.comp.name}</span>
                </a>
                {section.comp.countryFlagUrl && <img src={section.comp.countryFlagUrl} alt="" className="w-4 h-3 opacity-60 rounded-sm" />}
                <div className="flex-1" />
                <svg className={`w-4 h-4 opacity-35 transition-transform ${section.expanded ? 'rotate-180' : ''}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M6 9l6 6 6-6" /></svg>
              </div>

              {section.expanded && (
                <div className="bg-[var(--sc-surface)]">
                  {section.loading ? (
                    <div className="flex justify-center py-4"><div className="w-5 h-5 border-2 border-[var(--sc-primary)] border-t-transparent rounded-full animate-spin" /></div>
                  ) : section.todayMatches.length > 0 ? (
                    <>
                      <div className="text-center py-1"><span className="inline-block text-[11px] font-bold uppercase tracking-wide px-2.5 py-0.5 rounded-full bg-orange-500/15 text-orange-500">⚽ Today</span></div>
                      {section.todayMatches.map(m => <MatchTile key={m.matchId} match={m} />)}
                    </>
                  ) : (
                    <>
                      {section.lastGwMatches.length > 0 && (
                        <>
                          <div className="text-center py-1"><span className="inline-block text-[11px] font-bold uppercase tracking-wide px-2.5 py-0.5 rounded-full bg-green-500/12 text-green-500">✓ MW {section.lastGwNum} Results</span></div>
                          {section.lastGwMatches.map(m => <MatchTile key={m.matchId} match={m} />)}
                        </>
                      )}
                      {section.nextGwMatches.length > 0 && (
                        <>
                          <div className="text-center py-1"><span className="inline-block text-[11px] font-bold uppercase tracking-wide px-2.5 py-0.5 rounded-full bg-blue-500/12 text-blue-500">📅 MW {section.nextGwNum} Upcoming</span></div>
                          {section.nextGwMatches.map(m => <MatchTile key={m.matchId} match={m} />)}
                        </>
                      )}
                      {section.lastGwMatches.length === 0 && section.nextGwMatches.length === 0 && (
                        <div className="py-6 text-center text-[var(--sc-text-secondary)] text-sm">⚽ No matches scheduled</div>
                      )}
                    </>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
