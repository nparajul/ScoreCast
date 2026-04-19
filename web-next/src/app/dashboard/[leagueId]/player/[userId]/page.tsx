'use client';

import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { api } from '@/lib/api';
import type { PlayerProfileResult, GameweekMatchesResult, MyPredictionResult, LeagueStandingsResult, RiskPlayResult, ScoringRuleResult, MatchDetail } from '@/lib/types';

const OUTCOME_COLORS: Record<string, string> = {
  ExactScore: 'bg-green-700/25',
  CorrectResultAndGD: 'bg-teal-600/25',
  CorrectResult: 'bg-amber-500/25',
  CorrectGD: 'bg-purple-500/20',
  Wrong: 'bg-red-600/20',
};

export default function PlayerProfilePage() {
  const { leagueId, userId } = useParams<{ leagueId: string; userId: string }>();
  const lid = Number(leagueId), uid = Number(userId);
  const [profile, setProfile] = useState<PlayerProfileResult | null>(null);
  const [standings, setStandings] = useState<LeagueStandingsResult | null>(null);
  const [gw, setGw] = useState<GameweekMatchesResult | null>(null);
  const [preds, setPreds] = useState<Map<number, MyPredictionResult>>(new Map());
  const [visible, setVisible] = useState(false);
  const [riskPlaysVisible, setRiskPlaysVisible] = useState(false);
  const [riskPlays, setRiskPlays] = useState<RiskPlayResult[]>([]);
  const [rules, setRules] = useState<ScoringRuleResult[]>([]);
  const [startGw, setStartGw] = useState(1);
  const [showBreakdown, setShowBreakdown] = useState(false);

  useEffect(() => {
    Promise.all([api.getPlayerProfile(uid, lid), api.getLeagueStandings(lid), api.getScoringRules()]).then(([p, s, r]) => {
      if (p.success && p.data) setProfile(p.data);
      if (s.success && s.data) { setStandings(s.data); setStartGw(s.data.startingGameweekNumber ?? 1); }
      if (r.success && r.data) setRules(r.data);
    });
  }, [uid, lid]);

  const loadGw = useCallback(async (gwNum: number) => {
    if (!standings) return;
    const res = await api.getGameweekMatches(standings.seasonId, gwNum);
    if (!res.success || !res.data) return;
    setGw(res.data);
    const gwRes = await api.getPlayerGameweek(uid, lid, standings.seasonId, res.data.gameweekId);
    setVisible(false); setRiskPlaysVisible(false); setRiskPlays([]);
    const m = new Map<number, MyPredictionResult>();
    if (gwRes.success && gwRes.data) {
      setVisible(gwRes.data.predictionsVisible);
      setRiskPlaysVisible(gwRes.data.riskPlaysVisible);
      if (gwRes.data.predictionsVisible) gwRes.data.predictions.forEach(p => m.set(p.matchId, p));
      if (gwRes.data.riskPlaysVisible) setRiskPlays(gwRes.data.riskPlays);
    }
    setPreds(m);
  }, [uid, lid, standings]);

  useEffect(() => { if (standings) loadGw(0); }, [standings, loadGw]);

  if (!profile) return <div className="py-8 text-center opacity-50">Loading…</div>;

  const statItems = [
    { label: 'Total Pts', value: profile.totalPoints },
    { label: 'Best GW', value: profile.bestGameweek ?? '-' },
    { label: 'Avg / GW', value: (profile.averagePointsPerGameweek ?? profile.avgPerGw)?.toFixed(1) ?? '-' },
    { label: 'Exact Scores', value: profile.exactScores },
    { label: 'Correct Results', value: profile.correctResults },
    { label: 'MW Played', value: profile.matchweeksPlayed ?? '-' },
  ];

  const matchLabel = (matchId: number) => {
    const m = gw?.matches.find(x => x.matchId === matchId);
    return m ? `${m.homeTeamShortName ?? m.homeTeamName} vs ${m.awayTeamShortName ?? m.awayTeamName}` : '';
  };

  // Points breakdown
  const predictedMatches = gw?.matches.filter(m => preds.has(m.matchId) && m.status === 'Finished') ?? [];
  const totalPts = predictedMatches.reduce((sum, m) => sum + (preds.get(m.matchId)?.pointsAwarded ?? 0), 0);
  const riskBonus = riskPlays.filter(r => r.bonusPoints != null).reduce((sum, r) => sum + (r.bonusPoints ?? 0), 0);

  return (
    <div className="py-4 max-w-2xl mx-auto px-4">
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <div className="w-14 h-14 rounded-full bg-[var(--sc-border)] flex items-center justify-center text-2xl overflow-hidden shrink-0">
          {profile.avatarUrl ? <img src={profile.avatarUrl} alt="" className="w-full h-full object-cover" /> : '👤'}
        </div>
        <div>
          <h1 className="text-xl font-bold">{profile.displayName}</h1>
          {profile.favoriteTeam && <p className="text-sm text-[var(--sc-text-secondary)]">❤️ {profile.favoriteTeam}</p>}
          {standings && <p className="text-xs text-[var(--sc-text-secondary)]">{standings.leagueName} · {standings.competitionName}</p>}
          {profile.rank && <p className="text-xs text-[var(--sc-text-secondary)]">Rank #{profile.rank}</p>}
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 sm:grid-cols-6 gap-2 mb-6">
        {statItems.map(s => (
          <div key={s.label} className="rounded-lg border border-[var(--sc-border)] p-2.5 text-center">
            <div className="text-xl font-extrabold">{s.value}</div>
            <div className="text-[11px] text-[var(--sc-text-secondary)]">{s.label}</div>
          </div>
        ))}
      </div>

      {/* Gameweek Nav */}
      {gw && (
        <>
          <h2 className="font-bold mb-2">Predictions</h2>
          <div className="flex items-center justify-center gap-4 mb-3">
            <button disabled={gw.gameweekNumber <= startGw} onClick={() => loadGw(gw.gameweekNumber - 1)} className="text-lg disabled:opacity-30 cursor-pointer">‹</button>
            <span className="text-lg font-bold">Gameweek {gw.gameweekNumber}</span>
            <button disabled={gw.gameweekNumber >= gw.totalGameweeks} onClick={() => loadGw(gw.gameweekNumber + 1)} className="text-lg disabled:opacity-30 cursor-pointer">›</button>
          </div>

          {!visible ? (
            <div className="text-sm text-[var(--sc-text-secondary)] text-center py-4 bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)]">🔒 Predictions are hidden until matches kick off.</div>
          ) : !gw.matches.some(m => preds.has(m.matchId)) ? (
            <div className="text-sm text-[var(--sc-text-secondary)] text-center py-4 bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)]">{profile.displayName} didn&apos;t make any predictions this gameweek.</div>
          ) : (
            <>
              {/* Points summary */}
              {predictedMatches.length > 0 && (
                <div className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-xl p-3 mb-3 text-center">
                  <div className="text-3xl font-extrabold">{totalPts + riskBonus}</div>
                  <div className="text-xs text-[var(--sc-text-secondary)]">points this gameweek</div>
                  <button onClick={() => setShowBreakdown(!showBreakdown)} className="text-xs text-[var(--sc-primary)] font-bold mt-1 cursor-pointer bg-transparent border-none">
                    {showBreakdown ? 'Hide' : 'Show'} Breakdown ▾
                  </button>
                </div>
              )}

              {showBreakdown && (
                <div className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-xl p-3 mb-3 text-sm">
                  <div className="font-bold mb-2">Points Breakdown</div>
                  {rules.map(rule => {
                    const matching = predictedMatches.filter(m => preds.get(m.matchId)?.outcome === rule.label);
                    if (matching.length === 0) return null;
                    const pts = matching.length * rule.points;
                    return (
                      <div key={rule.label} className="flex justify-between py-1 border-b border-[var(--sc-border)]">
                        <span>{rule.label}</span>
                        <span className="font-bold">{rule.points} × {matching.length} = {pts}</span>
                      </div>
                    );
                  })}
                  {riskPlays.filter(r => r.bonusPoints != null).length > 0 && (
                    <div className="mt-2 pt-2 border-t border-dashed border-[var(--sc-border)]">
                      <div className="text-xs font-bold text-[var(--sc-text-secondary)] mb-1">🎲 Risk Plays</div>
                      {riskPlays.filter(r => r.bonusPoints != null).map((rp, i) => (
                        <div key={i} className="flex justify-between py-0.5 text-xs">
                          <span>🎲 {rp.riskType} · {matchLabel(rp.matchId)}</span>
                          <span className={`font-bold ${rp.isWon ? 'text-green-500' : 'text-red-500'}`}>{rp.bonusPoints > 0 ? '+' : ''}{rp.bonusPoints}</span>
                        </div>
                      ))}
                    </div>
                  )}
                  <div className="flex justify-between font-bold pt-2 mt-2 border-t-2 border-[var(--sc-border)]">
                    <span>Total</span><span>{totalPts + riskBonus}</span>
                  </div>
                </div>
              )}

              {/* Match list */}
              <div className="rounded-xl border border-[var(--sc-border)] overflow-hidden divide-y divide-[var(--sc-border)]">
                {gw.matches.map(m => {
                  const p = preds.get(m.matchId);
                  const rowClass = p?.outcome ? OUTCOME_COLORS[p.outcome] ?? '' : '';
                  return (
                    <div key={m.matchId} className={`px-2 py-2 ${rowClass}`}>
                      <div className="flex items-center">
                        <div className="flex-1 flex items-center justify-end gap-1 min-w-0">
                          <span className="text-xs font-semibold truncate text-right">{m.homeTeamShortName || m.homeTeamName}</span>
                          {(m.homeTeamLogo || m.homeTeamLogoUrl) && <img src={m.homeTeamLogo ?? m.homeTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                        </div>
                        <div className="px-2 text-center w-20 shrink-0">
                          {p ? <span className="text-sm font-bold">{p.predictedHomeScore} - {p.predictedAwayScore}</span> : <span className="text-xs opacity-30">—</span>}
                        </div>
                        <div className="flex-1 flex items-center gap-1 min-w-0">
                          {(m.awayTeamLogo || m.awayTeamLogoUrl) && <img src={m.awayTeamLogo ?? m.awayTeamLogoUrl} alt="" className="w-5 h-5 shrink-0" />}
                          <span className="text-xs font-semibold truncate">{m.awayTeamShortName || m.awayTeamName}</span>
                        </div>
                      </div>
                      <div className="text-center text-[11px] text-[var(--sc-text-secondary)] mt-1">
                        {m.status === 'Finished' ? `FT ${m.homeScore}-${m.awayScore}` : m.kickoffTime ? new Date(m.kickoffTime).toLocaleString(undefined, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }) : ''}
                        {p?.pointsAwarded != null && m.status === 'Finished' && <span className="ml-2 font-bold text-[var(--sc-primary)]">+{p.pointsAwarded}</span>}
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Risk Plays */}
              {riskPlaysVisible && riskPlays.length > 0 && (
                <div className="mt-3">
                  <div className="flex items-center gap-2 px-3 py-2.5 rounded-t-xl text-white text-sm font-bold" style={{ background: 'linear-gradient(135deg, #7c4dff, #651fff)' }}>
                    🎲 Risk Plays
                  </div>
                  <div className="border border-t-0 border-[var(--sc-border)] rounded-b-xl overflow-hidden divide-y divide-[var(--sc-border)]">
                    {riskPlays.map((rp, i) => (
                      <div key={i} className="px-3 py-2.5 flex items-center justify-between text-sm">
                        <div className="flex items-center gap-1.5">
                          <span>🎲</span>
                          <span className="font-bold">{rp.riskType}</span>
                          <span className="text-xs text-[var(--sc-text-secondary)]">· {matchLabel(rp.matchId)}</span>
                        </div>
                        {rp.bonusPoints != null && (
                          <span className={`font-bold ${rp.isWon ? 'text-green-500' : 'text-red-500'}`}>
                            {rp.bonusPoints > 0 ? '+' : ''}{rp.bonusPoints}
                            {!rp.isResolved && <span className="text-[10px] opacity-70"> (live)</span>}
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
              {!riskPlaysVisible && visible && (
                <div className="text-sm text-[var(--sc-text-secondary)] text-center py-3 mt-3 bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)]">
                  🎲 Risk plays are hidden until all matches have kicked off.
                </div>
              )}
            </>
          )}
        </>
      )}
    </div>
  );
}
