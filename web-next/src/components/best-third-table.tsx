'use client';

import Link from 'next/link';
import { PointsTableRow } from '@/lib/types';

interface Props {
  rows: PointsTableRow[];
}

export default function BestThirdTable({ rows }: Props) {
  if (!rows.length) return <p className="text-sm text-[var(--sc-text-secondary)] mt-2">No data available.</p>;
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm border-collapse max-w-2xl">
        <thead>
          <tr className="text-xs text-[var(--sc-text-secondary)] border-b border-[var(--sc-border)]">
            <th className="p-2 text-left w-8">#</th>
            <th className="p-2 text-left">Team</th>
            <th className="p-2 text-center w-8">P</th>
            <th className="p-2 text-center w-8">GD</th>
            <th className="p-2 text-center w-8">GF</th>
            <th className="p-2 text-center w-8 font-bold">Pts</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => {
            const qualified = i < 8;
            return (
              <tr key={r.teamId} className="border-b border-[var(--sc-border)]" style={{ borderLeft: qualified ? '3px solid #4caf50' : '3px solid transparent', background: qualified ? '#4caf5015' : undefined }}>
                <td className="p-2 font-semibold">{i + 1}</td>
                <td className="p-2">
                  <Link href={`/teams/${r.teamId}`} className="flex items-center gap-2 no-underline text-inherit">
                    {r.teamLogoUrl && <img src={r.teamLogoUrl} alt="" className="w-6 h-6" />}
                    <span className="font-semibold">{r.teamShortName ?? r.teamName}</span>
                  </Link>
                </td>
                <td className="p-2 text-center">{r.played}</td>
                <td className="p-2 text-center">{r.goalDifference}</td>
                <td className="p-2 text-center">{r.goalsFor}</td>
                <td className="p-2 text-center font-bold">{r.points}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
      <div className="flex gap-4 mt-3">
        <div className="flex items-center gap-1">
          <span className="w-3 h-3 rounded-sm inline-block" style={{ background: '#4caf50' }} />
          <span className="text-xs text-[var(--sc-text-secondary)]">Qualifies for next stage</span>
        </div>
      </div>
    </div>
  );
}
