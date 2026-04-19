'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { api } from '@/lib/api';
import type { TeamDetailResult, TeamMatchesResult, TeamSquadResult, SquadPlayer, PlayerStatsResult, PlayerStatRow, PointsTableResult } from '@/lib/types';

const TABS = ['Squad', 'Results', 'Fixtures', 'Stats'] as const;
type Tab = typeof TABS[number];

const POS_GROUP: Record<string, string> = {};
['Goalkeeper'].forEach(p => POS_GROUP[p] = 'GK');
['Centre-Back', 'Left-Back', 'Right-Back', 'Defence'].forEach(p => POS_GROUP[p] = 'DEF');
['Defensive Midfield', 'Central Midfield', 'Attacking Midfield', 'Left Midfield', 'Right Midfield', 'Midfield'].forEach(p => POS_GROUP[p] = 'MID');
['Left Winger', 'Right Winger', 'Centre-Forward', 'Offence'].forEach(p => POS_GROUP[p] = 'FWD');

function groupByPosition(players: SquadPlayer[]) {
  const groups: Record<string, SquadPlayer[]> = {};
  players.filter(p => !p.isCoach).forEach(p => {
    const g = POS_GROUP[p.position ?? ''] ?? 'Other';
    (groups[g] ??= []).push(p);
  });
  return ['GK', 'DEF', 'MID', 'FWD', 'Other'].filter(g => groups[g]).map(g => ({ label: g, players: groups[g] }));
}

function Countdown({ kickoff }: { kickoff: string }) {
  const [diff, setDiff] = useState('');
  useEffect(() => {
    const calc = () => {
      const ms = new Date(kickoff).getTime() - Date.now();
      if (ms <= 0) { setDiff('Now'); return; }
      const d = Math.floor(ms / 86400000);
      const h = Math.floor((ms % 86400000) / 3600000);
      const m = Math.floor((ms % 3600000) / 60000);
      setDiff(`${d > 0 ? `${d}d ` : ''}${h}h ${m}m`);
    };
    calc();
    const id = setInterval(calc, 60000);
    return () => clearInterval(id);
  }, [kickoff]);
  return <span className="text-lg font-extrabold text-[var(--sc-tertiary)]">{diff}</span>;
}

export default function TeamDetailPage() {
  const { id } = useParams<{ id: string }>();
  const teamId = +id;

  const [team, setTeam] = useState<TeamDetailResult | null>(null);
  const [matches, setMatches] = useState<TeamMatchesResult | null>(null);
  const [squad, setSquad] = useState<TeamSquadResult | null>(null);
  const [stats, setStats] = useState<PlayerStatRow[]>([]);
  const [tab, setTab] = useState<Tab>('Squad');
  const [statsSeasonId, setStatsSeasonId] = useState<number | undefined>();

  useEffect(() => {
    api.getTeamDetail(teamId).then(r => { if (r.data) setTeam(r.data); });
  }, [teamId]);

  const loadTab = useCallback(async (t: Tab) => {
    setTab(t);
    if (t === 'Squad' && !squad) { const r = await api.getTeamSquad(teamId); if (r.data) setSquad(r.data); }
    if ((t === 'Results' || t === 'Fixtures') && !matches) { const r = await api.getTeamMatches(teamId); if (r.data) setMatches(r.data); }
    if (t === 'Stats' && stats.length === 0) { const r = await api.getTeamPlayerStats(teamId); if (r.data) setStats(r.data.rows); }
  }, [teamId, squad, matches, stats.length]);

  useEffect(() => { loadTab('Squad'); }, [loadTab]);

  const loadStats = async (seasonId?: number) => {
    setStatsSeasonId(seasonId);
    const r = await api.getTeamPlayerStats(teamId, seasonId);
    if (r.data) setStats(r.data.rows);
  };

  if (!team) return <div className="py-12 text-center text-sm opacity-50">Loading…</div>;

  const form = team.recentForm ?? team.form ?? [];
  const results = (matches?.results ?? matches?.matches?.filter(m => m.status === 'Finished')) ?? [];
  const fixtures = (matches?.fixtures ?? matches?.matches?.filter(m => m.status !== 'Finished')) ?? [];
  const coach = squad?.players.find(p => p.isCoach);

  return (
    <div>
      {/* Header */}
      <div className="sticky top-14 z-10 bg-[var(--sc-bg)] pb-1">
        <div className="max-w-3xl mx-auto px-4 pt-3">
          <div className="flex items-center gap-3 mb-2">
            {team.logoUrl && <img src={team.logoUrl} alt="" className="w-12 h-12 md:w-14 md:h-14" />}
            <div className="flex-1 min-w-0">
              <h1 className="text-lg font-bold truncate">{team.name}</h1>
              <div className="flex items-center gap-1.5 text-xs text-[var(--sc-text-secondary)]">
                {team.countryFlagUrl && <img src={team.countryFlagUrl} alt="" className="w-4 h-3 rounded-sm" />}
                {team.countryName && <span>{team.countryName}</span>}
                {team.venue && <span>· 🏟️ {team.venue}</span>}
              </div>
            </div>
          </div>

          {/* Next match countdown */}
          {team.nextMatch?.kickoffTime && (
            <div className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-xl p-3 mb-2">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs font-bold text-[var(--sc-text-secondary)] mb-0.5">Next Match</div>
                  <div className="text-sm font-semibold">
                    {team.nextMatch.isHome ? `${team.shortName ?? team.name} vs ${team.nextMatch.opponentName}` : `${team.nextMatch.opponentName} vs ${team.shortName ?? team.name}`}
                  </div>
                  <div className="text-xs text-[var(--sc-text-secondary)]">
                    {new Date(team.nextMatch.kickoffTime).toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short' })} · {new Date(team.nextMatch.kickoffTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>
                <Countdown kickoff={team.nextMatch.kickoffTime} />
              </div>
            </div>
          )}

          {/* Form guide */}
          {form.length > 0 && (
            <div className="flex items-center gap-1.5 mb-2">
              <span className="text-xs font-bold text-[var(--sc-text-secondary)] mr-1">Form</span>
              {form.slice(0, 5).map((f, i) => (
                <span key={i} className={`w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-bold text-white ${f.result === 'W' ? 'bg-green-500' : f.result === 'L' ? 'bg-red-500' : 'bg-gray-400'}`}>
                  {f.result}
                </span>
              ))}
            </div>
          )}

          {/* Pill tabs */}
          <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5">
            {TABS.map(t => (
              <button key={t} onClick={() => loadTab(t)} className={`flex-1 text-center py-1.5 rounded-full text-xs font-bold cursor-pointer ${tab === t ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{t}</button>
            ))}
          </div>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 pt-3 pb-24">
        {/* Squad */}
        {tab === 'Squad' && squad && (
          <>
            {coach && (
              <div className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-xl p-3 mb-3 flex items-center gap-3">
                {coach.photoUrl ? <img src={coach.photoUrl} alt="" className="w-12 h-12 rounded-full" /> : <div className="w-12 h-12 rounded-full bg-[var(--sc-border)] flex items-center justify-center text-lg">👤</div>}
                <div>
                  <div className="text-xs font-bold text-[var(--sc-text-secondary)]">Coach</div>
                  <div className="font-semibold">{coach.name}</div>
                  {coach.nationality && <div className="text-xs text-[var(--sc-text-secondary)]">{coach.nationality}</div>}
                </div>
              </div>
            )}
            {groupByPosition(squad.players).map(g => (
              <div key={g.label} className="mb-4">
                <h2 className="text-sm font-bold mb-2">{g.label}</h2>
                <div className="grid gap-2" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))' }}>
                  {g.players.map(p => (
                    <div key={p.playerId ?? p.name} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg p-2 text-center">
                      {(p.photoUrl || p.imageUrl) ? <img src={p.photoUrl ?? p.imageUrl} alt="" className="w-12 h-12 rounded-full mx-auto mb-1" /> : <div className="w-12 h-12 rounded-full bg-[var(--sc-border)] mx-auto mb-1 flex items-center justify-center text-lg">👤</div>}
                      <div className="text-[11px] font-semibold truncate">{p.name}</div>
                      {p.shirtNumber && <div className="text-[10px] text-[var(--sc-text-secondary)]">#{p.shirtNumber}</div>}
                      {p.nationality && <div className="text-[9px] text-[var(--sc-text-secondary)] truncate">{p.nationality}</div>}
                      {p.dateOfBirth && <div className="text-[9px] text-[var(--sc-text-secondary)]">{new Date(p.dateOfBirth).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}</div>}
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </>
        )}

        {/* Results */}
        {tab === 'Results' && (
          <div className="bg-[var(--sc-surface)] rounded-xl border border-[var(--sc-border)] overflow-hidden">
            {results.length > 0 ? results.map(m => (
              <Link key={m.matchId} href={`/matches/${m.matchId}`} className="flex items-center justify-between px-3 py-2.5 border-b border-[var(--sc-border)] text-sm no-underline text-inherit hover:bg-black/[0.02]">
                <div className="flex items-center gap-2 flex-1 min-w-0">
                  {(m.homeTeamLogo || m.homeTeamLogoUrl) && <img src={m.homeTeamLogo ?? m.homeTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                  <span className="font-semibold truncate">{m.homeTeamShortName ?? m.homeTeamName}</span>
                </div>
                <div className="font-bold px-3 shrink-0">{m.homeScore ?? '-'} - {m.awayScore ?? '-'}</div>
                <div className="flex items-center gap-2 flex-1 justify-end min-w-0">
                  <span className="font-semibold truncate">{m.awayTeamShortName ?? m.awayTeamName}</span>
                  {(m.awayTeamLogo || m.awayTeamLogoUrl) && <img src={m.awayTeamLogo ?? m.awayTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                </div>
              </Link>
            )) : <p className="p-4 text-sm text-[var(--sc-text-secondary)]">No results yet.</p>}
          </div>
        )}

        {/* Fixtures */}
        {tab === 'Fixtures' && (
          <div className="bg-[var(--sc-surface)] rounded-xl border border-[var(--sc-border)] overflow-hidden">
            {fixtures.length > 0 ? fixtures.map(m => (
              <div key={m.matchId} className="flex items-center justify-between px-3 py-2.5 border-b border-[var(--sc-border)] text-sm">
                <div className="flex items-center gap-2 flex-1 min-w-0">
                  {(m.homeTeamLogo || m.homeTeamLogoUrl) && <img src={m.homeTeamLogo ?? m.homeTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                  <span className="font-semibold truncate">{m.homeTeamShortName ?? m.homeTeamName}</span>
                </div>
                <div className="text-xs text-[var(--sc-text-secondary)] px-3 shrink-0 text-center">
                  {m.kickoffTime ? (
                    <div>
                      <div>{new Date(m.kickoffTime).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}</div>
                      <div>{new Date(m.kickoffTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                    </div>
                  ) : 'TBD'}
                </div>
                <div className="flex items-center gap-2 flex-1 justify-end min-w-0">
                  <span className="font-semibold truncate">{m.awayTeamShortName ?? m.awayTeamName}</span>
                  {(m.awayTeamLogo || m.awayTeamLogoUrl) && <img src={m.awayTeamLogo ?? m.awayTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                </div>
              </div>
            )) : <p className="p-4 text-sm text-[var(--sc-text-secondary)]">No upcoming fixtures.</p>}
          </div>
        )}

        {/* Stats */}
        {tab === 'Stats' && (
          <>
            {team.competitions && team.competitions.length > 0 && (
              <div className="mb-3">
                <select value={statsSeasonId ?? ''} onChange={e => loadStats(e.target.value ? +e.target.value : undefined)} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-1.5 text-sm">
                  <option value="">All Competitions</option>
                  {team.competitions.map(c => <option key={c.seasonId} value={c.seasonId}>{c.competitionName}</option>)}
                </select>
              </div>
            )}
            {stats.length > 0 ? (
              <div className="bg-[var(--sc-surface)] rounded-xl border border-[var(--sc-border)] overflow-hidden">
                <table className="w-full text-sm">
                  <thead><tr className="text-xs text-[var(--sc-text-secondary)] border-b border-[var(--sc-border)]">
                    <th className="p-2 text-left w-8">#</th><th className="p-2 text-left">Player</th>
                    <th className="p-2 text-center">⚽</th><th className="p-2 text-center">👟</th>
                    <th className="p-2 text-center">🟨</th><th className="p-2 text-center">🟥</th>
                  </tr></thead>
                  <tbody>
                    {[...stats].sort((a, b) => (b.goals + b.penaltyGoals) - (a.goals + a.penaltyGoals) || b.assists - a.assists).slice(0, 50).map((r, i) => (
                      <tr key={r.playerId} className="border-b border-[var(--sc-border)]">
                        <td className="p-2">{i + 1}</td>
                        <td className="p-2"><div className="flex items-center gap-1.5">{r.playerImageUrl && <img src={r.playerImageUrl} alt="" className="w-6 h-6 rounded-full" />}<span className="font-semibold text-xs md:text-sm">{r.playerName}</span></div></td>
                        <td className="p-2 text-center font-bold">{r.goals + r.penaltyGoals}</td>
                        <td className="p-2 text-center">{r.assists}</td>
                        <td className="p-2 text-center">{r.yellowCards}</td>
                        <td className="p-2 text-center">{r.redCards}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : <p className="text-sm text-[var(--sc-text-secondary)]">No stats available.</p>}
          </>
        )}
      </div>
    </div>
  );
}
