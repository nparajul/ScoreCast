'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import { api } from '@/lib/api';
import { TeamDetailResult, TeamMatchesResult, TeamSquadResult, SquadPlayer } from '@/lib/types';

const TABS = ['Squad', 'Results', 'Fixtures'] as const;
type Tab = typeof TABS[number];

const POS_ORDER: Record<string, number> = { Goalkeeper: 0, 'Centre-Back': 1, 'Left-Back': 2, 'Right-Back': 3, 'Defensive Midfield': 4, 'Central Midfield': 5, 'Attacking Midfield': 6, 'Left Winger': 7, 'Right Winger': 8, 'Centre-Forward': 9 };
const POS_GROUP: Record<string, string> = {};
['Goalkeeper'].forEach(p => POS_GROUP[p] = 'GK');
['Centre-Back', 'Left-Back', 'Right-Back', 'Defence'].forEach(p => POS_GROUP[p] = 'DEF');
['Defensive Midfield', 'Central Midfield', 'Attacking Midfield', 'Left Midfield', 'Right Midfield', 'Midfield'].forEach(p => POS_GROUP[p] = 'MID');
['Left Winger', 'Right Winger', 'Centre-Forward', 'Offence'].forEach(p => POS_GROUP[p] = 'FWD');

function groupByPosition(players: SquadPlayer[]) {
  const groups: Record<string, SquadPlayer[]> = {};
  players.forEach(p => {
    const g = POS_GROUP[p.position ?? ''] ?? 'Other';
    (groups[g] ??= []).push(p);
  });
  const order = ['GK', 'DEF', 'MID', 'FWD', 'Other'];
  return order.filter(g => groups[g]).map(g => ({ label: g, players: groups[g] }));
}

export default function TeamDetailPage() {
  const { id } = useParams<{ id: string }>();
  const teamId = +id;

  const [team, setTeam] = useState<TeamDetailResult | null>(null);
  const [matches, setMatches] = useState<TeamMatchesResult | null>(null);
  const [squad, setSquad] = useState<TeamSquadResult | null>(null);
  const [tab, setTab] = useState<Tab>('Squad');

  useEffect(() => {
    api.getTeamDetail(teamId).then(r => { if (r.data) setTeam(r.data); });
  }, [teamId]);

  const loadTab = useCallback(async (t: Tab) => {
    setTab(t);
    if (t === 'Squad' && !squad) { const r = await api.getTeamSquad(teamId); if (r.data) setSquad(r.data); }
    if ((t === 'Results' || t === 'Fixtures') && !matches) { const r = await api.getTeamMatches(teamId); if (r.data) setMatches(r.data); }
  }, [teamId, squad, matches]);

  useEffect(() => { loadTab('Squad'); }, [loadTab]);

  if (!team) return null;

  const now = new Date();
  const results = matches?.matches.filter(m => m.status === 'Finished') ?? [];
  const fixtures = matches?.matches.filter(m => m.status !== 'Finished') ?? [];

  const renderMatches = (list: typeof results) => list.map(m => (
    <div key={m.matchId} className="flex items-center justify-between px-3 py-2 border-b border-[var(--sc-border)] text-sm">
      <div className="flex items-center gap-2 flex-1">
        {m.homeTeamLogo && <img src={m.homeTeamLogo} alt="" className="w-5 h-5" />}
        <span className="font-semibold truncate">{m.homeTeamShortName}</span>
      </div>
      <div className="font-bold px-3">{m.homeScore ?? '-'} - {m.awayScore ?? '-'}</div>
      <div className="flex items-center gap-2 flex-1 justify-end">
        <span className="font-semibold truncate">{m.awayTeamShortName}</span>
        {m.awayTeamLogo && <img src={m.awayTeamLogo} alt="" className="w-5 h-5" />}
      </div>
    </div>
  ));

  return (
    <div>
      {/* Header */}
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] pb-1">
        <div className="max-w-3xl mx-auto px-4 pt-3">
          <div className="flex items-center gap-3 mb-2">
            {team.logoUrl && <img src={team.logoUrl} alt="" className="w-12 h-12" />}
            <div>
              <h1 className="text-lg font-bold">{team.name}</h1>
              {team.venue && <p className="text-xs text-[var(--sc-text-secondary)]">🏟️ {team.venue}</p>}
            </div>
          </div>
          <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5">
            {TABS.map(t => (
              <button key={t} onClick={() => loadTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-xs font-bold cursor-pointer ${tab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
            ))}
          </div>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 pt-3 pb-24">
        {tab === 'Squad' && squad && groupByPosition(squad.players).map(g => (
          <div key={g.label} className="mb-4">
            <h2 className="text-sm font-bold mb-2">{g.label}</h2>
            <div className="grid gap-2" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))' }}>
              {g.players.map(p => (
                <div key={p.playerId} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg p-2 text-center">
                  {p.photoUrl ? <img src={p.photoUrl} alt="" className="w-12 h-12 rounded-full mx-auto mb-1" /> : <div className="w-12 h-12 rounded-full bg-[var(--sc-border)] mx-auto mb-1 flex items-center justify-center text-lg">👤</div>}
                  <div className="text-[11px] font-semibold truncate">{p.name}</div>
                </div>
              ))}
            </div>
          </div>
        ))}

        {tab === 'Results' && (
          <div className="bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)] overflow-hidden">
            {results.length > 0 ? renderMatches(results) : <p className="p-4 text-sm text-[var(--sc-text-secondary)]">No results yet.</p>}
          </div>
        )}

        {tab === 'Fixtures' && (
          <div className="bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)] overflow-hidden">
            {fixtures.length > 0 ? renderMatches(fixtures) : <p className="p-4 text-sm text-[var(--sc-text-secondary)]">No upcoming fixtures.</p>}
          </div>
        )}
      </div>
    </div>
  );
}
