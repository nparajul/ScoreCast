'use client';

import { useState } from 'react';
import { BracketResult, BracketSlot } from '@/lib/types';

interface Props {
  bracket?: BracketResult;
}

function MatchSlot({ slot }: { slot: BracketSlot }) {
  return (
    <div className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg p-2 text-xs min-w-[140px]">
      <div className="flex justify-between items-center gap-2">
        <span className="font-semibold truncate">{slot.homeTeam ?? slot.home ?? 'TBD'}</span>
        <span className="font-bold">{slot.homeScore ?? '-'}</span>
      </div>
      <div className="flex justify-between items-center gap-2 mt-1 border-t border-[var(--sc-border)] pt-1">
        <span className="font-semibold truncate">{slot.awayTeam ?? slot.away ?? 'TBD'}</span>
        <span className="font-bold">{slot.awayScore ?? '-'}</span>
      </div>
    </div>
  );
}

const ROUND_LABELS: Record<string, string> = {
  'Round of 32': 'R32', 'Round of 16': 'R16', 'Quarter-Finals': 'QF', 'Semi-Finals': 'SF', 'Final': 'Final', 'Third Place': '3rd',
};

export default function KnockoutBracket({ bracket }: Props) {
  const rounds = bracket?.rounds ?? [];
  const roundMap = Object.fromEntries(rounds.map(r => [r.name, r]));
  const orderedNames = ['Round of 32', 'Round of 16', 'Quarter-Finals', 'Semi-Finals', 'Final', 'Third Place'];
  const available = orderedNames.filter(n => roundMap[n]?.slots?.length);
  const mobileLabels = available.map(n => ROUND_LABELS[n] || n);
  const [mobileRound, setMobileRound] = useState(mobileLabels[0] ?? 'R16');

  if (!rounds.length) return <p className="text-sm text-[var(--sc-text-secondary)] mt-2">No knockout bracket available.</p>;

  // Mobile: single round view
  const mobileContent = () => {
    const fullName = available.find(n => (ROUND_LABELS[n] || n) === mobileRound);
    const slots = fullName ? roundMap[fullName]?.slots ?? [] : [];
    return (
      <div>
        <div className="flex bg-[var(--sc-primary)] rounded-full p-0.5 mb-3 overflow-x-auto">
          {mobileLabels.map(label => (
            <button key={label} onClick={() => setMobileRound(label)} className={`flex-1 text-center py-1.5 rounded-full text-xs font-bold cursor-pointer whitespace-nowrap px-2 ${mobileRound === label ? 'bg-[var(--sc-surface)] text-[var(--sc-primary)]' : 'text-white/70 bg-transparent'}`}>{label}</button>
          ))}
        </div>
        <div className="flex flex-col gap-3">
          {slots.map((s, i) => <MatchSlot key={i} slot={s} />)}
        </div>
      </div>
    );
  };

  // Desktop: all rounds side by side
  const desktopRounds = available.filter(n => n !== 'Third Place');
  const thirdPlace = roundMap['Third Place'];

  return (
    <>
      {/* Mobile */}
      <div className="md:hidden">{mobileContent()}</div>

      {/* Desktop */}
      <div className="hidden md:block overflow-x-auto">
        <div className="flex gap-6 items-start min-w-max">
          {desktopRounds.map(name => {
            const round = roundMap[name];
            const label = ROUND_LABELS[name] || name;
            const isFinal = name === 'Final';
            return (
              <div key={name} className="flex flex-col items-center gap-3" style={{ justifyContent: 'center', minHeight: isFinal ? undefined : '100%' }}>
                <div className="text-xs font-bold text-[var(--sc-text-secondary)] mb-1">
                  {isFinal && <div className="text-3xl text-center">🏆</div>}
                  {name}
                </div>
                {round.slots.map((s, i) => <MatchSlot key={i} slot={s} />)}
                {isFinal && thirdPlace && thirdPlace.slots.length > 0 && (
                  <div className="mt-4">
                    <div className="text-xs font-bold text-center mb-1">3rd Place</div>
                    <MatchSlot slot={thirdPlace.slots[0]} />
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
}
