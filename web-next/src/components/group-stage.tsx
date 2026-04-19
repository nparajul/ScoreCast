'use client';

import Link from 'next/link';
import { PointsTableGroup, CompetitionZoneResult } from '@/lib/types';

interface Props {
  groups: PointsTableGroup[];
  zones?: CompetitionZoneResult[];
}

export default function GroupStage({ groups, zones = [] }: Props) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {groups.map(g => (
        <div key={g.groupName} className="bg-[var(--sc-surface)] rounded-lg border border-[var(--sc-border)] overflow-hidden">
          <div className="px-3 py-2 font-bold text-sm border-b border-[var(--sc-border)]">{g.groupName}</div>
          <table className="w-full text-xs">
            <thead>
              <tr className="text-[var(--sc-text-secondary)]">
                <th className="p-1.5 text-left w-6">#</th>
                <th className="p-1.5 text-left">Team</th>
                <th className="p-1.5 text-center w-6">P</th>
                <th className="p-1.5 text-center w-6">W</th>
                <th className="p-1.5 text-center w-6">D</th>
                <th className="p-1.5 text-center w-6">L</th>
                <th className="p-1.5 text-center w-6">GD</th>
                <th className="p-1.5 text-center w-7 font-bold">Pts</th>
              </tr>
            </thead>
            <tbody>
              {g.rows.map(r => {
                const zone = zones.find(z => r.position >= z.startPosition && r.position <= z.endPosition);
                return (
                  <tr key={r.teamId} className="border-t border-[var(--sc-border)]" style={{ borderLeft: zone ? `3px solid ${zone.color}` : '3px solid transparent' }}>
                    <td className="p-1.5 text-[var(--sc-text-secondary)]">{r.position}</td>
                    <td className="p-1.5">
                      <Link href={`/teams/${r.teamId}`} className="flex items-center gap-1.5 no-underline text-inherit">
                        {r.teamLogoUrl && <img src={r.teamLogoUrl} alt="" className="w-5 h-5" />}
                        <span className="font-semibold truncate">{r.teamShortName ?? r.teamName}</span>
                      </Link>
                    </td>
                    <td className="p-1.5 text-center">{r.played}</td>
                    <td className="p-1.5 text-center">{r.won}</td>
                    <td className="p-1.5 text-center">{r.drawn}</td>
                    <td className="p-1.5 text-center">{r.lost}</td>
                    <td className="p-1.5 text-center">{r.goalDifference}</td>
                    <td className="p-1.5 text-center font-bold">{r.points}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ))}
    </div>
  );
}
