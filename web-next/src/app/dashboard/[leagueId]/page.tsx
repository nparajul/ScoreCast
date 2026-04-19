'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { api } from '@/lib/api';
import type { LeagueStandingsResult } from '@/lib/types';

export default function LeagueDetailPage() {
  const { leagueId } = useParams<{ leagueId: string }>();
  const lid = Number(leagueId);
  const [data, setData] = useState<LeagueStandingsResult | null>(null);
  const [myUserId, setMyUserId] = useState(0);

  useEffect(() => {
    Promise.all([api.getLeagueStandings(lid), api.getMyProfile()]).then(([s, p]) => {
      if (s.success && s.data) setData(s.data);
      if (p.success && p.data) setMyUserId(p.data.id);
    });
  }, [lid]);

  if (!data) return <div className="py-8 text-center opacity-50">Loading…</div>;

  return (
    <div className="py-4 max-w-2xl mx-auto px-4">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 mb-4">
        <div>
          <h1 className="text-xl font-bold">{data.leagueName}</h1>
          <p className="text-xs text-[var(--sc-text-secondary)]">
            {data.standings.length} member{data.standings.length !== 1 ? 's' : ''}
            {data.startingGameweekNumber && data.startingGameweekNumber > 1 ? ` · Since GW${data.startingGameweekNumber}` : ''}
          </p>
        </div>
        <Link href={`/predict/${data.seasonId}`} className="px-4 py-1.5 rounded-full text-xs font-bold text-white no-underline" style={{ background: 'var(--sc-tertiary)' }}>
          ✏️ Make Predictions
        </Link>
      </div>

      <div className="rounded-xl border border-[var(--sc-border)] overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-[10px] text-[var(--sc-text-secondary)]">
              <th className="px-1 py-1.5 text-left w-6">#</th>
              <th className="px-1 py-1.5 text-left">Player</th>
              <th className="px-2 py-1.5 text-center font-bold" title="Points">Pts</th>
              <th className="px-2 py-1.5 text-center" title="Gameweek Points">GW</th>
              <th className="px-2 py-1.5 text-center" title="Exact Scorelines">Exact</th>
              <th className="px-2 py-1.5 text-center" title="Correct Results">Correct</th>
              <th className="px-2 py-1.5 text-center" title="Matchweeks Played">MP</th>
            </tr>
          </thead>
          <tbody>
            {data.standings.map((row, i) => {
              const isMe = row.userId === myUserId;
              const href = isMe ? `/predict/${data.seasonId}` : `/dashboard/${lid}/player/${row.userId}`;
              return (
                <tr key={row.userId} className="border-t border-[var(--sc-border)]">
                  <td className="px-1 py-2 font-semibold">{i + 1}</td>
                  <td className="px-1 py-2">
                    <Link href={href} className="flex items-center gap-2 no-underline text-inherit">
                      <div className="w-7 h-7 rounded-full bg-[var(--sc-border)] flex items-center justify-center text-xs overflow-hidden shrink-0">
                        {row.avatarUrl ? <img src={row.avatarUrl} alt="" className="w-full h-full object-cover" /> : '👤'}
                      </div>
                      <span className="font-semibold truncate max-w-[120px] sm:max-w-none underline underline-offset-2 decoration-[var(--sc-text-secondary)]">{row.displayName}</span>
                    </Link>
                  </td>
                  <td className="px-2 py-2 text-center font-bold text-[15px]">{row.totalPoints}</td>
                  <td className="px-2 py-2 text-center font-semibold" style={{ color: 'var(--sc-tertiary)' }}>{row.gameweekPoints}</td>
                  <td className="px-2 py-2 text-center">{row.exactScores}</td>
                  <td className="px-2 py-2 text-center">{row.correctResults}</td>
                  <td className="px-2 py-2 text-center">{row.predictionCount}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
