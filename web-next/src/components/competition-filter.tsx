"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { CompetitionResult, SeasonResult } from "@/lib/types";

export interface CompetitionFilterState {
  competition?: CompetitionResult;
  season?: SeasonResult;
}

export function CompetitionFilter({ onChange }: { onChange: (s: CompetitionFilterState) => void }) {
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  const [seasons, setSeasons] = useState<SeasonResult[]>([]);
  const [comp, setComp] = useState<CompetitionResult>();
  const [season, setSeason] = useState<SeasonResult>();

  useEffect(() => {
    (async () => {
      const [compsRes, defaultRes] = await Promise.all([api.getCompetitions(), api.getDefaultCompetition()]);
      const comps = compsRes.data ?? [];
      setCompetitions(comps);
      const def = comps.find((c) => c.code === defaultRes.data?.code) ?? comps[0];
      if (def) {
        setComp(def);
        const seasonsRes = await api.getSeasons(def.code);
        const s = seasonsRes.data ?? [];
        setSeasons(s);
        const current = s.find((x) => x.isCurrent) ?? s[0];
        setSeason(current);
        onChange({ competition: def, season: current });
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCompChange = async (code: string) => {
    const c = competitions.find((x) => x.code === code);
    setComp(c);
    setSeason(undefined);
    if (!c) { onChange({}); return; }
    const res = await api.getSeasons(c.code);
    const s = res.data ?? [];
    setSeasons(s);
    const current = s.find((x) => x.isCurrent) ?? s[0];
    setSeason(current);
    onChange({ competition: c, season: current });
  };

  const handleSeasonChange = (id: string) => {
    const s = seasons.find((x) => x.id === Number(id));
    setSeason(s);
    onChange({ competition: comp, season: s });
  };

  return (
    <div className="flex items-center gap-2 flex-wrap">
      <select
        value={comp?.code ?? ""}
        onChange={(e) => handleCompChange(e.target.value)}
        className="rounded-lg border border-[var(--sc-border)] bg-[var(--sc-surface)] px-3 py-1.5 text-sm font-medium"
      >
        {competitions.map((c) => (
          <option key={c.code} value={c.code}>{c.name}</option>
        ))}
      </select>
      <select
        value={season?.id ?? ""}
        onChange={(e) => handleSeasonChange(e.target.value)}
        className="rounded-lg border border-[var(--sc-border)] bg-[var(--sc-surface)] px-3 py-1.5 text-sm font-medium"
        disabled={!seasons.length}
      >
        {seasons.map((s) => (
          <option key={s.id} value={s.id}>{s.name}</option>
        ))}
      </select>
    </div>
  );
}
