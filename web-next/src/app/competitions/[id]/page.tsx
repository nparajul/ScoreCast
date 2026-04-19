'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { api } from '@/lib/api';
import { CompetitionResult, SeasonResult, PointsTableResult, BracketResult, CompetitionZoneResult, GameweekMatchesResult, PlayerStatsResult, PlayerStatRow } from '@/lib/types';
import LeagueTable from '@/components/league-table';
import GroupStage from '@/components/group-stage';
import BestThirdTable from '@/components/best-third-table';
import KnockoutBracket from '@/components/knockout-bracket';

type SortCol = 'Goals' | 'Assists' | 'YellowCards' | 'RedCards';

export default function CompetitionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const compId = +id;

  const [comp, setComp] = useState<CompetitionResult | null>(null);
  const [seasons, setSeasons] = useState<SeasonResult[]>([]);
  const [season, setSeason] = useState<SeasonResult | null>(null);
  const [table, setTable] = useState<PointsTableResult | null>(null);
  const [bracket, setBracket] = useState<BracketResult | null>(null);
  const [zones, setZones] = useState<CompetitionZoneResult[]>([]);
  const [gw, setGw] = useState<GameweekMatchesResult | null>(null);
  const [stats, setStats] = useState<PlayerStatsResult | null>(null);
  const [tab, setTab] = useState('Table');
  const [groupTab, setGroupTab] = useState('Groups');
  const [playerTab, setPlayerTab] = useState('Overall');
  const [sortCol, setSortCol] = useState<SortCol>('Goals');
  const [sortDesc, setSortDesc] = useState(true);

  const loadSeason = useCallback(async (c: CompetitionResult, s: SeasonResult) => {
    const [tR, bR, zR, gR, sR] = await Promise.all([
      api.getPointsTable(s.id), api.getBracket(s.id), api.getCompetitionZones(c.code),
      api.getGameweekMatches(s.id, 0), api.getPlayerStats(s.id),
    ]);
    if (tR.data) setTable(tR.data);
    if (bR.data) setBracket(bR.data);
    if (zR.data) setZones(zR.data);
    if (gR.data) setGw(gR.data);
    if (sR.data) setStats(sR.data);
  }, []);

  useEffect(() => {
    (async () => {
      const cRes = await api.getCompetitions();
      const c = cRes.data?.find(x => x.id === compId);
      if (!c) return;
      setComp(c);
      const sRes = await api.getSeasons(c.code);
      if (sRes.data) {
        setSeasons(sRes.data);
        const cur = sRes.data.find(x => x.isCurrent) ?? sRes.data[0];
        if (cur) { setSeason(cur); await loadSeason(c, cur); }
      }
    })();
  }, [compId, loadSeason]);

  const onSeasonChange = async (sid: number) => {
    const s = seasons.find(x => x.id === sid);
    if (s && comp) { setSeason(s); setTable(null); setBracket(null); setGw(null); setStats(null); await loadSeason(comp, s); }
  };

  const prevGw = async () => { if (season && gw && gw.gameweekNumber > 1) { const r = await api.getGameweekMatches(season.id, gw.gameweekNumber - 1); if (r.data) setGw(r.data); } };
  const nextGw = async () => { if (season && gw && gw.gameweekNumber < gw.totalGameweeks) { const r = await api.getGameweekMatches(season.id, gw.gameweekNumber + 1); if (r.data) setGw(r.data); } };

  const isGroup = table?.format === 'GroupAndKnockout';
  const mainTabs = isGroup ? ['Table', 'Scores', 'Players'] : ['Table', 'Scores', 'Players'];
  const groupTabs = ['Groups', 'Best 3rd', 'Knockout'];

  const sortBy = (col: SortCol) => { if (sortCol === col) setSortDesc(!sortDesc); else { setSortCol(col); setSortDesc(true); } };
  const sorted = (rows: PlayerStatRow[]) => {
    const s = [...rows];
    const dir = sortDesc ? -1 : 1;
    s.sort((a, b) => {
      const v = (r: PlayerStatRow) => sortCol === 'Goals' ? r.goals + r.penaltyGoals : sortCol === 'Assists' ? r.assists : sortCol === 'YellowCards' ? r.yellowCards : r.redCards;
      return (v(b) - v(a)) * dir;
    });
    return s;
  };
  const mobileStats = (rows: PlayerStatRow[]) => {
    const s = [...rows];
    s.sort((a, b) => playerTab === 'Goals' ? (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals) : playerTab === 'Assists' ? b.assists - a.assists : playerTab === 'Discipline' ? (b.yellowCards + b.redCards) - (a.yellowCards + a.redCards) : (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals));
    return s.slice(0, 50);
  };

  const matchesByDate = gw?.matches.reduce<Record<string, typeof gw.matches>>((acc, m) => {
    const d = m.kickoffTime ? new Date(m.kickoffTime).toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }) : 'TBD';
    (acc[d] ??= []).push(m);
    return acc;
  }, {}) ?? {};

  if (!comp) return null;

  return (
    <div>
      {/* Banner */}
      <div className="px-4 py-6">
        <div className="max-w-3xl mx-auto flex items-center gap-3">
          {comp.logoUrl && <img src={comp.logoUrl} alt="" className="w-12 h-12 object-contain" />}
          <div className="flex-1">
            <h1 className="text-xl font-bold">{comp.name}</h1>
            <div className="flex items-center gap-1">
              {comp.countryFlagUrl && <img src={comp.countryFlagUrl} alt="" className="w-4 h-3" />}
              <span className="text-xs text-[var(--sc-text-secondary)]">{comp.countryName}</span>
            </div>
          </div>
          {seasons.length > 0 && (
            <select value={season?.id ?? ''} onChange={e => onSeasonChange(+e.target.value)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
              {seasons.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          )}
        </div>
      </div>

      {/* Tab bar */}
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] border-b-2 border-[var(--sc-border)]">
        <div className="max-w-3xl mx-auto flex overflow-x-auto">
          {mainTabs.map(t => (
            <button key={t} onClick={() => setTab(t)} className={`px-4 py-2.5 text-sm font-medium whitespace-nowrap cursor-pointer border-b-2 ${tab === t ? 'border-[var(--sc-tertiary)] text-[var(--sc-primary)] font-bold' : 'border-transparent text-[var(--sc-text-secondary)]'} bg-transparent`}>{t}</button>
          ))}
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 pt-3 pb-24">
        {/* Table tab */}
        {tab === 'Table' && table && (
          isGroup ? (
            <>
              <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3">
                {groupTabs.map(t => (
                  <button key={t} onClick={() => setGroupTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-xs font-semibold cursor-pointer ${groupTab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
                ))}
              </div>
              {groupTab === 'Groups' && <GroupStage groups={table.groups} zones={zones} />}
              {groupTab === 'Best 3rd' && <BestThirdTable rows={table.bestThirdPlaced} />}
              {groupTab === 'Knockout' && <KnockoutBracket bracket={bracket ?? undefined} />}
            </>
          ) : table.groups.length > 0 ? <LeagueTable rows={table.groups[0].rows} zones={zones} /> : <p className="text-sm text-[var(--sc-text-secondary)]">No table data.</p>
        )}

        {/* Scores tab */}
        {tab === 'Scores' && gw && (
          <div>
            <div className="flex items-center justify-center gap-2 mb-4">
              <button onClick={prevGw} disabled={gw.gameweekNumber <= 1} className="px-2 py-1 rounded bg-[var(--sc-surface)] border border-[var(--sc-border)] disabled:opacity-30 cursor-pointer">◀</button>
              <span className="text-sm font-bold bg-[var(--sc-primary)] text-white px-3 py-1 rounded-full">MW {gw.gameweekNumber}</span>
              <button onClick={nextGw} disabled={gw.gameweekNumber >= gw.totalGameweeks} className="px-2 py-1 rounded bg-[var(--sc-surface)] border border-[var(--sc-border)] disabled:opacity-30 cursor-pointer">▶</button>
            </div>
            {Object.entries(matchesByDate).map(([date, matches]) => (
              <div key={date} className="bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)] mb-3 overflow-hidden">
                <div className="text-center font-bold text-sm py-2">{date}</div>
                {matches.map(m => (
                  <div key={m.matchId} className="flex items-center justify-between px-3 py-2 border-t border-[var(--sc-border)] text-sm">
                    <div className="flex items-center gap-2 flex-1">
                      {m.homeTeamLogoUrl && <img src={m.homeTeamLogoUrl} alt="" className="w-5 h-5" />}
                      <span className="font-semibold truncate">{m.homeTeamShortName ?? m.homeTeamName}</span>
                    </div>
                    <div className="font-bold px-3">{m.homeScore ?? '-'} - {m.awayScore ?? '-'}</div>
                    <div className="flex items-center gap-2 flex-1 justify-end">
                      <span className="font-semibold truncate">{m.awayTeamShortName ?? m.awayTeamName}</span>
                      {m.awayTeamLogoUrl && <img src={m.awayTeamLogoUrl} alt="" className="w-5 h-5" />}
                    </div>
                  </div>
                ))}
              </div>
            ))}
          </div>
        )}

        {/* Players tab */}
        {tab === 'Players' && stats && (
          <>
            {/* Mobile */}
            <div className="md:hidden">
              <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3 overflow-x-auto">
                {['Overall', 'Goals', 'Assists', 'Discipline'].map(t => (
                  <button key={t} onClick={() => setPlayerTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-[10px] font-bold cursor-pointer whitespace-nowrap px-2 ${playerTab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
                ))}
              </div>
              <table className="w-full text-xs">
                <thead><tr className="text-[var(--sc-text-secondary)]">
                  <th className="p-1 text-left w-6">#</th><th className="p-1 text-left">Player</th>
                  {playerTab === 'Overall' && <><th className="p-1 text-center w-6">⚽</th><th className="p-1 text-center w-6">👟</th><th className="p-1 text-center w-6">🟨</th><th className="p-1 text-center w-6">🟥</th></>}
                  {playerTab === 'Goals' && <><th className="p-1 text-center w-7">⚽</th><th className="p-1 text-center w-7">Pen</th></>}
                  {playerTab === 'Assists' && <th className="p-1 text-center w-7">👟</th>}
                  {playerTab === 'Discipline' && <><th className="p-1 text-center w-7">🟨</th><th className="p-1 text-center w-7">🟥</th></>}
                </tr></thead>
                <tbody>
                  {mobileStats(stats.rows).map((r, i) => (
                    <tr key={r.playerId} className="border-t border-[var(--sc-border)]">
                      <td className="p-1 font-semibold">{i + 1}</td>
                      <td className="p-1"><div className="flex items-center gap-1">{r.teamLogo && <img src={r.teamLogo} alt="" className="w-4 h-4" />}<span className="font-semibold truncate">{r.playerName}</span></div></td>
                      {playerTab === 'Overall' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center">{r.assists}</td><td className="p-1 text-center">{r.yellowCards}</td><td className="p-1 text-center">{r.redCards}</td></>}
                      {playerTab === 'Goals' && <><td className="p-1 text-center font-bold">{r.goals + r.penaltyGoals}</td><td className="p-1 text-center text-[var(--sc-text-secondary)]">{r.penaltyGoals}</td></>}
                      {playerTab === 'Assists' && <td className="p-1 text-center font-bold">{r.assists}</td>}
                      {playerTab === 'Discipline' && <><td className="p-1 text-center font-bold">{r.yellowCards}</td><td className="p-1 text-center font-bold">{r.redCards}</td></>}
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
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('YellowCards')}>🟨 {sortCol === 'YellowCards' && (sortDesc ? '▼' : '▲')}</th>
                  <th className="p-2 text-center cursor-pointer" onClick={() => sortBy('RedCards')}>🟥 {sortCol === 'RedCards' && (sortDesc ? '▼' : '▲')}</th>
                </tr></thead>
                <tbody>
                  {sorted(stats.rows).map((r, i) => (
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
