'use client';

import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import { CompetitionResult, SeasonResult, PlayerStatRow } from '@/lib/types';

type SortCol = 'Goals' | 'Assists' | 'YellowCards' | 'RedCards';

export default function PlayerStatsPage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [seasons, setSeasons] = useState<SeasonResult[]>([]);
  const [selectedSeason, setSelectedSeason] = useState<SeasonResult | null>(null);
  const [rows, setRows] = useState<PlayerStatRow[]>([]);
  const [sortCol, setSortCol] = useState<SortCol>('Goals');
  const [sortDesc, setSortDesc] = useState(true);
  const [mobileTab, setMobileTab] = useState('Overall');

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

  const sortBy = (col: SortCol) => { if (sortCol === col) setSortDesc(!sortDesc); else { setSortCol(col); setSortDesc(true); } };
  const val = (r: PlayerStatRow, col: SortCol) => col === 'Goals' ? r.goals + r.penaltyGoals : col === 'Assists' ? r.assists : col === 'YellowCards' ? r.yellowCards : r.redCards;
  const sorted = [...rows].sort((a, b) => (val(b, sortCol) - val(a, sortCol)) * (sortDesc ? 1 : -1));

  const mobileSorted = [...rows].sort((a, b) => {
    if (mobileTab === 'Goals') return (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals);
    if (mobileTab === 'Assists') return b.assists - a.assists;
    if (mobileTab === 'Discipline') return (b.yellowCards + b.redCards) - (a.yellowCards + a.redCards);
    return (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals);
  }).slice(0, 50);

  return (
    <div>
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] py-2">
        <div className="max-w-3xl mx-auto px-4">
          <h1 className="text-lg font-bold mb-2">Player Stats</h1>
          <div className="flex gap-2">
            <select onChange={e => onCompChange(e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm" defaultValue="">
              <option value="" disabled>Competition</option>
              {competitions.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
            </select>
            {seasons.length > 0 && (
              <select value={selectedSeason?.id ?? ''} onChange={e => onSeasonChange(+e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
                {seasons.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 pt-2 pb-24">
        {rows.length > 0 && (
          <>
            {/* Mobile */}
            <div className="md:hidden">
              <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3 overflow-x-auto">
                {['Overall', 'Goals', 'Assists', 'Discipline'].map(t => (
                  <button key={t} onClick={() => setMobileTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-[10px] font-bold cursor-pointer whitespace-nowrap px-2 ${mobileTab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
                ))}
              </div>
              <table className="w-full text-xs">
                <thead><tr className="text-[var(--sc-text-secondary)]">
                  <th className="p-1 text-left w-6">#</th><th className="p-1 text-left">Player</th>
                  {mobileTab === 'Overall' && <><th className="p-1 text-center w-6">⚽</th><th className="p-1 text-center w-6">👟</th><th className="p-1 text-center w-6">🟨</th><th className="p-1 text-center w-6">🟥</th></>}
                  {mobileTab === 'Goals' && <><th className="p-1 text-center w-7">⚽</th><th className="p-1 text-center w-7">Pen</th></>}
                  {mobileTab === 'Assists' && <th className="p-1 text-center w-7">👟</th>}
                  {mobileTab === 'Discipline' && <><th className="p-1 text-center w-7">🟨</th><th className="p-1 text-center w-7">🟥</th></>}
                </tr></thead>
                <tbody>
                  {mobileSorted.map((r, i) => (
                    <tr key={r.playerId} className="border-t border-[var(--sc-border)]">
                      <td className="p-1 font-semibold">{i + 1}</td>
                      <td className="p-1"><div className="flex items-center gap-1">{r.teamLogo && <img src={r.teamLogo} alt="" className="w-4 h-4" />}<span className="font-semibold truncate">{r.playerName}</span></div></td>
                      {mobileTab === 'Overall' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center">{r.assists}</td><td className="p-1 text-center">{r.yellowCards}</td><td className="p-1 text-center">{r.redCards}</td></>}
                      {mobileTab === 'Goals' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center text-[var(--sc-text-secondary)]">{r.penaltyGoals}</td></>}
                      {mobileTab === 'Assists' && <td className="p-1 text-center font-bold">{r.assists}</td>}
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
                  <th className="p-2 text-left w-10">#</th><th className="p-2 text-left">Player</th><th className="p-2 text-left">Team</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('Goals')}>⚽ Goals {sortCol === 'Goals' && (sortDesc ? '▼' : '▲')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('Assists')}>👟 Assists {sortCol === 'Assists' && (sortDesc ? '▼' : '▲')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('YellowCards')}>🟨 Yellows {sortCol === 'YellowCards' && (sortDesc ? '▼' : '▲')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('RedCards')}>🟥 Reds {sortCol === 'RedCards' && (sortDesc ? '▼' : '▲')}</th>
                </tr></thead>
                <tbody>
                  {sorted.map((r, i) => (
                    <tr key={r.playerId} className="border-b border-[var(--sc-border)] hover:bg-black/5">
                      <td className="p-2">{i + 1}</td>
                      <td className="p-2 font-semibold"><div className="flex items-center gap-2">{r.playerImageUrl && <img src={r.playerImageUrl} alt="" className="w-8 h-8 rounded-full" />}{r.playerName}</div></td>
                      <td className="p-2"><div className="flex items-center gap-2">{r.teamLogo && <img src={r.teamLogo} alt="" className="w-5 h-5" />}{r.teamName}</div></td>
                      <td className="p-2 text-center font-bold">{r.goals + r.penaltyGoals}{r.penaltyGoals > 0 && <span className="text-xs text-[var(--sc-text-secondary)]"> ({r.penaltyGoals}p)</span>}</td>
                      <td className="p-2 text-center">{r.assists}</td>
                      <td className="p-2 text-center">{r.yellowCards}</td>
                      <td className="p-2 text-center">{r.redCards}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
