'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { api } from '@/lib/api';
import { CompetitionResult, SeasonResult, PointsTableResult, BracketResult, CompetitionZoneResult } from '@/lib/types';
import LeagueTable from '@/components/league-table';
import GroupStage from '@/components/group-stage';
import BestThirdTable from '@/components/best-third-table';
import KnockoutBracket from '@/components/knockout-bracket';

export default function PointsTablePage() {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [seasons, setSeasons] = useState<SeasonResult[]>([]);
  const [selectedComp, setSelectedComp] = useState<CompetitionResult | null>(null);
  const [selectedSeason, setSelectedSeason] = useState<SeasonResult | null>(null);
  const [table, setTable] = useState<PointsTableResult | null>(null);
  const [bracket, setBracket] = useState<BracketResult | null>(null);
  const [zones, setZones] = useState<CompetitionZoneResult[]>([]);
  const [hasLive, setHasLive] = useState(false);
  const [groupTab, setGroupTab] = useState('Groups');
  const pollRef = useRef<ReturnType<typeof setInterval> | undefined>(undefined);

  useEffect(() => {
    api.getCompetitions().then(r => { if (r.data) setCompetitions(r.data); });
  }, []);

  const loadTable = useCallback(async (comp: CompetitionResult, season: SeasonResult) => {
    const [tRes, bRes, zRes] = await Promise.all([
      api.getPointsTable(season.id),
      api.getBracket(season.id),
      api.getCompetitionZones(comp.code),
    ]);
    if (tRes.data) setTable(tRes.data);
    if (bRes.data) setBracket(bRes.data);
    if (zRes.data) setZones(zRes.data);
  }, []);

  const onCompChange = async (code: string) => {
    const comp = competitions.find(c => c.code === code);
    if (!comp) return;
    setSelectedComp(comp);
    setTable(null); setBracket(null); setZones([]);
    const sRes = await api.getSeasons(comp.code);
    if (sRes.data) {
      setSeasons(sRes.data);
      const current = sRes.data.find(s => s.isCurrent) ?? sRes.data[0];
      if (current) { setSelectedSeason(current); await loadTable(comp, current); }
    }
  };

  const onSeasonChange = async (id: number) => {
    const season = seasons.find(s => s.id === id);
    if (season && selectedComp) { setSelectedSeason(season); await loadTable(selectedComp, season); }
  };

  // 30s polling
  useEffect(() => {
    if (pollRef.current) clearInterval(pollRef.current);
    if (!selectedSeason) return;
    pollRef.current = setInterval(async () => {
      const gwRes = await api.getGameweekMatches(selectedSeason.id, 0);
      const live = gwRes.data?.matches.some(m => m.status === 'Live');
      setHasLive(!!live);
      if (live) {
        const tRes = await api.getPointsTable(selectedSeason.id);
        if (tRes.data) setTable(tRes.data);
      }
    }, 30000);
    return () => { if (pollRef.current) clearInterval(pollRef.current); };
  }, [selectedSeason]);

  const isGroup = table?.format === 'GroupAndKnockout';
  const tabs = ['Groups', 'Best 3rd', 'Knockout'];

  return (
    <div>
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] py-2">
        <div className="max-w-3xl mx-auto px-4">
          <div className="flex items-center gap-2 mb-2">
            <h1 className="text-lg font-bold">Points Table</h1>
            {hasLive && <span className="text-[10px] font-bold text-white bg-green-600 px-2 py-0.5 rounded-full animate-pulse">● LIVE</span>}
          </div>
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
        {table && table.groups.length > 0 && !isGroup && <LeagueTable rows={table.groups[0].rows} zones={zones} />}
        {table && isGroup && (
          <>
            <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3">
              {tabs.map(t => (
                <button key={t} onClick={() => setGroupTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-xs font-semibold cursor-pointer ${groupTab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
              ))}
            </div>
            {groupTab === 'Groups' && <GroupStage groups={table.groups} zones={zones} />}
            {groupTab === 'Best 3rd' && <BestThirdTable rows={table.bestThirdPlaced} />}
            {groupTab === 'Knockout' && <KnockoutBracket bracket={bracket ?? undefined} />}
          </>
        )}
        {table && table.groups.length === 0 && <p className="text-sm text-[var(--sc-text-secondary)]">No table data available for this season.</p>}
      </div>
    </div>
  );
}
