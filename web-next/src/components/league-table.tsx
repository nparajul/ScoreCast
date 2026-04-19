'use client';

import Link from 'next/link';
import { PointsTableRow, CompetitionZoneResult } from '@/lib/types';

interface Props {
  rows: PointsTableRow[];
  zones?: CompetitionZoneResult[];
  highlightTeamName?: string;
}

function getZone(pos: number, zones: CompetitionZoneResult[]) {
  return zones.find(z => pos >= z.startPosition && pos <= z.endPosition);
}

function formColor(ch: string) {
  return ch === 'W' ? '#4caf50' : ch === 'L' ? '#f44336' : '#9e9e9e';
}

export default function LeagueTable({ rows, zones = [], highlightTeamName }: Props) {
  const isHl = (r: PointsTableRow) =>
    highlightTeamName && (r.teamName === highlightTeamName || r.teamShortName === highlightTeamName);

  return (
    <div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="text-xs text-[var(--sc-text-secondary)] border-b border-[var(--sc-border)]">
              <th className="p-2 text-left w-8">#</th>
              <th className="p-2 text-left">Team</th>
              <th className="p-2 text-center w-8">P</th>
              <th className="p-2 text-center w-8 hidden sm:table-cell">W</th>
              <th className="p-2 text-center w-8 hidden sm:table-cell">D</th>
              <th className="p-2 text-center w-8 hidden sm:table-cell">L</th>
              <th className="p-2 text-center w-8 hidden md:table-cell">GF</th>
              <th className="p-2 text-center w-8 hidden md:table-cell">GA</th>
              <th className="p-2 text-center w-8">GD</th>
              <th className="p-2 text-center w-8 font-bold">Pts</th>
              <th className="p-2 text-center hidden lg:table-cell">Form</th>
            </tr>
          </thead>
          <tbody>
            {rows.map(r => {
              const hl = isHl(r);
              const zone = getZone(r.position, zones);
              return (
                <tr
                  key={r.teamId}
                  className="border-b border-[var(--sc-border)]"
                  style={{
                    borderLeft: hl ? '3px solid var(--sc-tertiary)' : zone ? `3px solid ${zone.color}` : '3px solid transparent',
                    background: hl ? 'rgba(255,107,53,0.1)' : zone ? `${zone.color}15` : undefined,
                  }}
                >
                  <td className="p-2 font-semibold">{r.position}</td>
                  <td className="p-2">
                    <Link href={`/teams/${r.teamId}`} className="flex items-center gap-2 no-underline text-inherit">
                      {r.teamLogoUrl && <img src={r.teamLogoUrl} alt="" className="w-6 h-6" />}
                      <span className={hl ? 'font-bold' : 'font-semibold'}>{r.teamShortName ?? r.teamName}</span>
                    </Link>
                  </td>
                  <td className="p-2 text-center">{r.played}</td>
                  <td className="p-2 text-center hidden sm:table-cell">{r.won}</td>
                  <td className="p-2 text-center hidden sm:table-cell">{r.drawn}</td>
                  <td className="p-2 text-center hidden sm:table-cell">{r.lost}</td>
                  <td className="p-2 text-center hidden md:table-cell">{r.goalsFor}</td>
                  <td className="p-2 text-center hidden md:table-cell">{r.goalsAgainst}</td>
                  <td className="p-2 text-center">{r.goalDifference}</td>
                  <td className="p-2 text-center font-bold">{r.points}</td>
                  <td className="p-2 text-center hidden lg:table-cell">
                    {r.form && (
                      <div className="flex gap-1 justify-center">
                        {r.form.split('').map((ch, i) => (
                          <span key={i} className="inline-flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold text-white" style={{ background: formColor(ch) }}>{ch}</span>
                        ))}
                      </div>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {zones.length > 0 && (
        <div className="flex gap-4 mt-3 flex-wrap">
          {zones.map(z => (
            <div key={z.name} className="flex items-center gap-1">
              <span className="w-3 h-3 rounded-sm inline-block" style={{ background: z.color }} />
              <span className="text-xs text-[var(--sc-text-secondary)]">{z.name}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
