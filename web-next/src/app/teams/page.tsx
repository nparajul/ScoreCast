'use client';

import { useState, useEffect, useRef } from 'react';
import Link from 'next/link';
import { api } from '@/lib/api';
import type { TeamResult } from '@/lib/types';

const PAGE_SIZE = 50;

export default function TeamsPage() {
  const [search, setSearch] = useState('');
  const [teams, setTeams] = useState<TeamResult[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const debounce = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const load = async (q: string, skip: number, reset: boolean) => {
    if (reset) setLoading(true); else setLoadingMore(true);
    const r = await api.searchTeams(q, skip, PAGE_SIZE);
    if (r.data) {
      setTeams(prev => reset ? r.data!.teams : [...prev, ...r.data!.teams]);
      setHasMore(!!r.data.hasMore);
    }
    setLoading(false);
    setLoadingMore(false);
  };

  useEffect(() => { load('', 0, true); }, []);

  const onSearch = (v: string) => {
    setSearch(v);
    clearTimeout(debounce.current);
    debounce.current = setTimeout(() => load(v, 0, true), 300);
  };

  return (
    <div className="max-w-3xl mx-auto px-4 pt-4 pb-24">
      <h1 className="text-xl font-bold mb-3">Teams</h1>
      <input
        type="text" value={search} onChange={e => onSearch(e.target.value)} placeholder="Search teams..."
        className="w-full bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg px-3 py-2 text-sm mb-4 outline-none focus:ring-1 focus:ring-[var(--sc-primary)]"
      />
      {loading ? (
        <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))' }}>
          {Array.from({ length: 12 }).map((_, i) => (
            <div key={i} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg p-3 h-20 animate-pulse" />
          ))}
        </div>
      ) : teams.length > 0 ? (
        <>
          <div className="grid gap-2" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))' }}>
            {teams.map(t => (
              <Link key={t.id} href={`/teams/${t.id}`} className="bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg p-2 text-center no-underline text-inherit hover:shadow-md transition-shadow">
                {t.logoUrl ? <img src={t.logoUrl} alt="" className="w-8 h-8 mx-auto mb-1 object-contain" /> : <div className="w-8 h-8 mx-auto mb-1 rounded-full bg-[var(--sc-border)] flex items-center justify-center text-sm">⚽</div>}
                <div className="text-xs font-semibold truncate">{t.shortName ?? t.name}</div>
              </Link>
            ))}
          </div>
          {hasMore && (
            <div className="flex justify-center mt-4">
              <button onClick={() => load(search, teams.length, false)} disabled={loadingMore} className="px-4 py-2 text-sm font-semibold border border-[var(--sc-border)] rounded-lg bg-[var(--sc-surface)] cursor-pointer disabled:opacity-50 hover:shadow-sm transition-shadow">
                {loadingMore ? 'Loading…' : 'Load More'}
              </button>
            </div>
          )}
        </>
      ) : (
        <p className="text-sm text-center text-[var(--sc-text-secondary)] py-8">{search ? 'No teams found' : 'Search for a team to get started'}</p>
      )}
    </div>
  );
}
