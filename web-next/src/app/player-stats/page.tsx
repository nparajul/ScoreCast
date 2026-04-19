'use client';

import { useState, useEffect, useRef } from 'react';
import { api } from '@/lib/api';
import type { CompetitionResult, SeasonResult, PlayerStatRow } from '@/lib/types';

type SortCol = 'Goals' | 'PenGoals' | 'Assists' | 'YellowCards' | 'RedCards' | 'CleanSheets' | 'MinutesPlayed';
const MOBILE_TABS = ['Overall', 'Goals', 'Assists', 'Clean Sheets', 'Discipline'] as const;

export default function PlayerStatsPage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [seasons, setSeasons] = useState<SeasonResult[]>([]);
  const [selectedSeason, setSelectedSeason] = useState<SeasonResult | null>(null);
  const [rows, setRows] = useState<PlayerStatRow[]>([]);
  const [search, setSearch] = useState('');
  const [sortCol, setSortCol] = useState<SortCol>('Goals');
  const [sortDesc, setSortDesc] = useState(true);
  const [mobileTab, setMobileTab] = useState<string>('Overall');
  const [teamFilter, setTeamFilter] = useState('');
  const [posFilter, setPosFilter] = useState('');
  const searchRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const [debouncedSearch, setDebouncedSearch] = useState('');

  useEffect(() => { api.getCompetitions().then(r => { if (r.data) setCompetitions(r.data); }); }, []);

  const onCompChange = async (code: string) => {
    setRows([]);
    const sRes = await api.getSeasons(code);
    if (sRes.data) {
      setSeasons(sRes.data);
      const cur = sRes.data.find(s => s.isCurrent) ?? sRes.data[0];
      if (cur) { setSelectedSeason(cur); const r = await api.getPlayerStats(cur.id); if (r.data) setRows(r.data.rows); }
    }
  };

  const onSeasonChange = async (id: number) => {
    const s = seasons.find(x => x.id === id);
    if (s) { setSelectedSeason(s); const r = await api.getPlayerStats(s.id); if (r.data) setRows(r.data.rows); }
  };

  const onSearch = (v: string) => {
    setSearch(v);
    clearTimeout(searchRef.current);
    searchRef.current = setTimeout(() => setDebouncedSearch(v), 300);
  };

  const sortBy = (col: SortCol) => { if (sortCol === col) setSortDesc(!sortDesc); else { setSortCol(col); setSortDesc(true); } };
  const sortArrow = (col: SortCol) => sortCol === col ? (sortDesc ? ' ▼' : ' ▲') : '';

  const val = (r: PlayerStatRow, col: SortCol): number => {
    switch (col) {
      case 'Goals': return r.goals + r.penaltyGoals;
      case 'PenGoals': return r.penaltyGoals;
      case 'Assists': return r.assists;
      case 'YellowCards': return r.yellowCards;
      case 'RedCards': return r.redCards;
      case 'CleanSheets': return r.cleanSheets ?? 0;
      case 'MinutesPlayed': return r.minutesPlayed ?? 0;
    }
  };

  const filtered = rows.filter(r => {
    if (debouncedSearch && !r.playerName.toLowerCase().includes(debouncedSearch.toLowerCase()) && !r.teamName?.toLowerCase().includes(debouncedSearch.toLowerCase())) return false;
    if (teamFilter && r.teamName !== teamFilter) return false;
    if (posFilter && r.position !== posFilter) return false;
    return true;
  });

  const sorted = [...filtered].sort((a, b) => (val(b, sortCol) - val(a, sortCol)) * (sortDesc ? 1 : -1));

  const mobileSorted = [...filtered].sort((a, b) => {
    if (mobileTab === 'Goals') return (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals);
    if (mobileTab === 'Assists') return b.assists - a.assists;
    if (mobileTab === 'Clean Sheets') return (b.cleanSheets ?? 0) - (a.cleanSheets ?? 0);
    if (mobileTab === 'Discipline') return (b.yellowCards + b.redCards) - (a.yellowCards + a.redCards);
    return (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals);
  }).slice(0, 50);

  const teams = [...new Set(rows.map(r => r.teamName).filter(Boolean))].sort();
  const positions = [...new Set(rows.map(r => r.position).filter(Boolean))].sort();

  return (
    <div>
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] py-2 border-b border-[var(--sc-border)]">
        <div className="max-w-5xl mx-auto px-4">
          <h1 className="text-lg font-bold mb-2">Player Stats</h1>
          <div className="flex flex-wrap gap-2">
            <select onChange={e => onCompChange(e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm" defaultValue="">
              <option value="" disabled>Competition</option>
              {competitions.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
            </select>
            {seasons.length > 0 && (
              <select value={selectedSeason?.id ?? ''} onChange={e => onSeasonChange(+e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
                {seasons.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}
            {rows.length > 0 && (
              <>
                <select value={teamFilter} onChange={e => setTeamFilter(e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
                  <option value="">All Teams</option>
                  {teams.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
                <select value={posFilter} onChange={e => setPosFilter(e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
                  <option value="">All Positions</option>
                  {positions.map(p => <option key={p} value={p!}>{p}</option>)}
                </select>
              </>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 pt-2 pb-24">
        {rows.length > 0 && (
          <>
            <input type="text" value={search} onChange={e => onSearch(e.target.value)} placeholder="Search players/teams…" className="w-full bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-2 text-sm mb-3 outline-none" />

            {/* Mobile */}
            <div className="md:hidden">
              <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3 overflow-x-auto">
                {MOBILE_TABS.map(t => (
                  <button key={t} onClick={() => setMobileTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-[10px] font-bold cursor-pointer whitespace-nowrap px-2 ${mobileTab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
                ))}
              </div>
              <table className="w-full text-xs">
                <thead><tr className="text-[var(--sc-text-secondary)]">
                  <th className="p-1 text-left w-6">#</th><th className="p-1 text-left">Player</th>
                  {mobileTab === 'Overall' && <><th className="p-1 text-center w-6">⚽</th><th className="p-1 text-center w-6">👟</th><th className="p-1 text-center w-6">🟨</th><th className="p-1 text-center w-6">🟥</th></>}
                  {mobileTab === 'Goals' && <><th className="p-1 text-center w-7">⚽</th><th className="p-1 text-center w-7">Pen</th></>}
                  {mobileTab === 'Assists' && <th className="p-1 text-center w-7">👟</th>}
                  {mobileTab === 'Clean Sheets' && <th className="p-1 text-center w-7">🧤</th>}
                  {mobileTab === 'Discipline' && <><th className="p-1 text-center w-7">🟨</th><th className="p-1 text-center w-7">🟥</th></>}
                </tr></thead>
                <tbody>
                  {mobileSorted.map((r, i) => (
                    <tr key={r.playerId} className="border-t border-[var(--sc-border)]">
                      <td className="p-1 font-semibold">{i + 1}</td>
                      <td className="p-1"><div className="flex items-center gap-1">{r.teamLogo && <img src={r.teamLogo} alt="" className="w-4 h-4" />}<span className="font-semibold truncate">{r.playerName}{r.position && <span className="text-[var(--sc-text-secondary)] font-normal text-[10px]"> ({r.position})</span>}</span></div></td>
                      {mobileTab === 'Overall' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center">{r.assists}</td><td className="p-1 text-center">{r.yellowCards}</td><td className="p-1 text-center">{r.redCards}</td></>}
                      {mobileTab === 'Goals' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center text-[var(--sc-text-secondary)]">{r.penaltyGoals}</td></>}
                      {mobileTab === 'Assists' && <td className="p-1 text-center font-bold">{r.assists}</td>}
                      {mobileTab === 'Clean Sheets' && <td className="p-1 text-center font-bold">{r.cleanSheets ?? 0}</td>}
                      {mobileTab === 'Discipline' && <><td className="p-1 text-center font-bold">{r.yellowCards}</td><td className="p-1 text-center font-bold">{r.redCards}</td></>}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Desktop */}
            <div className="hidden md:block">
              <table className="w-full text-sm border-collapse">
                <thead><tr className="text-xs text-[var(--sc-text-secondary)] border-b border-[var(--sc-border)]">
                  <th className="p-2 text-left w-10">#</th>
                  <th className="p-2 text-left">Player</th>
                  <th className="p-2 text-left">Team</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('Goals')}>⚽ Goals{sortArrow('Goals')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('PenGoals')}>Pen{sortArrow('PenGoals')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('Assists')}>👟 Assists{sortArrow('Assists')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('YellowCards')}>🟨{sortArrow('YellowCards')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('RedCards')}>🟥{sortArrow('RedCards')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('CleanSheets')}>🧤 CS{sortArrow('CleanSheets')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('MinutesPlayed')}>Min{sortArrow('MinutesPlayed')}</th>
                </tr></thead>
                <tbody>
                  {sorted.map((r, i) => (
                    <tr key={r.playerId} className="border-b border-[var(--sc-border)] hover:bg-black/5">
                      <td className="p-2">{i + 1}</td>
                      <td className="p-2 font-semibold"><div className="flex items-center gap-2">{r.playerImageUrl && <img src={r.playerImageUrl} alt="" className="w-8 h-8 rounded-full" />}{r.playerName}{r.position && <span className="text-xs text-[var(--sc-text-secondary)] font-normal">({r.position})</span>}</div></td>
                      <td className="p-2"><div className="flex items-center gap-2">{r.teamLogo && <img src={r.teamLogo} alt="" className="w-5 h-5" />}{r.teamName}</div></td>
                      <td className="p-2 text-center font-bold">{r.goals + r.penaltyGoals}</td>
                      <td className="p-2 text-center text-[var(--sc-text-secondary)]">{r.penaltyGoals}</td>
                      <td className="p-2 text-center">{r.assists}</td>
                      <td className="p-2 text-center">{r.yellowCards}</td>
                      <td className="p-2 text-center">{r.redCards}</td>
                      <td className="p-2 text-center">{r.cleanSheets ?? '-'}</td>
                      <td className="p-2 text-center">{r.minutesPlayed ?? '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
        {rows.length === 0 && selectedSeason && (
          <p className="text-sm text-[var(--sc-text-secondary)] text-center py-8">No player stats available.</p>
        )}
      </div>
    </div>
  );
}
