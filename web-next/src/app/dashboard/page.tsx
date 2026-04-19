"use client";

import { useAuth } from "@/contexts/auth-context";
import { api } from "@/lib/api";
import type { UserSeasonResult, PredictionLeagueResult, MyPredictionStatsResult, PredictionReplayResult, CompetitionResult, SeasonResult } from "@/lib/types";
import Link from "next/link";
import { useEffect, useState } from "react";
import { OnboardingCarousel } from "@/components/onboarding-carousel";
import { sendEmailVerification } from "firebase/auth";

export default function DashboardPage() {
  const { user, synced, needsOnboarding } = useAuth();
  const [seasons, setSeasons] = useState<UserSeasonResult[]>([]);
  const [leagues, setLeagues] = useState<PredictionLeagueResult[]>([]);
  const [stats, setStats] = useState<MyPredictionStatsResult | null>(null);
  const [replay, setReplay] = useState<PredictionReplayResult | null>(null);
  const [replaySeasonId, setReplaySeasonId] = useState<number>(0);
  const [replayGw, setReplayGw] = useState<number>(0);
  const [reordering, setReordering] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [competitions, setCompetitions] = useState<CompetitionResult[]>([]);
  // Dialogs
  const [showCreate, setShowCreate] = useState(false);
  const [showJoin, setShowJoin] = useState(false);
  const [showAddComp, setShowAddComp] = useState(false);
  const [newLeagueName, setNewLeagueName] = useState("");
  const [selectedComp, setSelectedComp] = useState<CompetitionResult | null>(null);
  const [inviteCode, setInviteCode] = useState("");
  const [resending, setResending] = useState(false);

  useEffect(() => {
    if (!user || !synced) return;
    Promise.all([
      api.getUserSeasons(),
      api.getMyLeagues(),
      api.getMyPredictionStats(),
      api.getCompetitions(),
    ]).then(([s, l, st, c]) => {
      const ss = s.success && s.data ? s.data : [];
      if (s.success && s.data) setSeasons(ss);
      if (l.success && l.data) setLeagues(l.data);
      if (st.success && st.data) setStats(st.data);
      if (c.success && c.data) setCompetitions(c.data);
      setLoaded(true);
      // Load replay (non-blocking)
      if (ss.length > 0) loadReplay(ss[0]);
    });
  }, [user, synced]);

  const loadReplay = async (season: UserSeasonResult) => {
    try {
      const gw = await api.getGameweekMatches(season.seasonId, 0);
      if (!gw.success || !gw.data) return;
      let finished = gw.data.matches.filter((m) => m.status === "Finished");
      let gwNum = gw.data.gameweekNumber;
      if (finished.length === 0 && gw.data.gameweekNumber > 1) {
        const prev = await api.getGameweekMatches(season.seasonId, gw.data.gameweekNumber - 1);
        if (!prev.success || !prev.data) return;
        finished = prev.data.matches.filter((m) => m.status === "Finished");
        gwNum = prev.data.gameweekNumber;
      }
      for (const m of [...finished].reverse()) {
        const r = await api.getPredictionReplay(m.matchId);
        if (r.success && r.data) {
          setReplay(r.data);
          setReplaySeasonId(season.seasonId);
          setReplayGw(gwNum);
          return;
        }
      }
    } catch { /* non-critical */ }
  };

  const move = async (from: number, to: number) => {
    const arr = [...seasons];
    const [item] = arr.splice(from, 1);
    arr.splice(to, 0, item);
    setSeasons(arr);
    await api.reorderUserSeasons(arr.map((s) => s.seasonId));
  };

  const createLeague = async () => {
    if (!newLeagueName.trim() || !selectedComp) return;
    setShowCreate(false);
    const r = await api.createLeague({ name: newLeagueName, competitionId: selectedComp.id });
    if (r.success && r.data) setLeagues((p) => [...p, r.data!]);
    setNewLeagueName(""); setSelectedComp(null);
  };

  const joinLeague = async () => {
    if (!inviteCode.trim()) return;
    setShowJoin(false);
    const r = await api.joinLeague(inviteCode.trim().toUpperCase());
    if (r.success && r.data) setLeagues((p) => [...p, r.data!]);
    setInviteCode("");
  };

  const addCompetition = async () => {
    if (!selectedComp) return;
    setShowAddComp(false);
    const existing = seasons.find((s) => s.competitionCode === selectedComp.code);
    if (existing) return;
    const seasonsResp = await api.getSeasons(selectedComp.code);
    const current = seasonsResp.data?.find((s: SeasonResult) => s.isCurrent);
    if (!current) return;
    const r = await api.enrollUserSeason(current.id);
    if (r.success && r.data) setSeasons((p) => [...p, r.data!]);
    setSelectedComp(null);
  };

  const copyCode = (code: string) => {
    navigator.clipboard.writeText(code);
  };

  const resendEmail = async () => {
    if (!user || resending) return;
    setResending(true);
    try { await sendEmailVerification(user); } catch { /* ignore */ }
    setTimeout(() => setResending(false), 5000);
  };

  const gw = stats?.lastGameweek;

  if (!user || !synced || !loaded) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="text-4xl animate-pulse">⚽</div>
        <span className="ml-3 text-sm text-[var(--sc-text-secondary)]">{!synced ? "Syncing account..." : "Loading dashboard..."}</span>
      </div>
    );
  }

  return (
    <div className="py-4 max-w-2xl mx-auto space-y-6">
      {needsOnboarding && <OnboardingCarousel />}

      {/* Welcome */}
      <div>
        <h1 className="text-2xl font-bold">Hi, {user.displayName || user.email?.split("@")[0] || "there"} 👋</h1>
        <p className="text-sm text-[var(--sc-text-secondary)]">Welcome back to ScoreCast</p>
      </div>

      {/* Email unverified banner */}
      {!user.emailVerified && (
        <div className="rounded-xl bg-amber-50 border border-amber-200 p-3 flex items-center justify-between text-sm">
          <span className="text-amber-800">📧 Please verify your email address</span>
          <button onClick={resendEmail} disabled={resending} className="text-xs font-bold text-amber-700 underline">
            {resending ? "Sent!" : "Resend"}
          </button>
        </div>
      )}

      {/* Hero Stats Card */}
      {gw && stats && (stats.totalPredictions ?? 0) > 0 && (
        <div className="rounded-xl p-5 text-center text-white" style={{ background: "linear-gradient(135deg, var(--sc-primary), #1a2d45)" }}>
          <div className="text-xs tracking-widest opacity-70 mb-1">GAMEWEEK {gw.gameweekNumber}</div>
          <div className="flex justify-around my-3">
            <div><span className="text-2xl font-extrabold">{gw.userCorrect}/{gw.userTotal}</span><br /><span className="text-xs opacity-60">You</span></div>
            <div><span className="text-2xl font-extrabold">{gw.beatPct}<span className="text-sm">%</span></span><br /><span className="text-xs opacity-60">Beaten →</span></div>
            <div><span className="text-2xl font-extrabold">{Math.round(gw.communityAvgCorrect ?? 0)}/{Math.round(gw.communityAvgTotal ?? 0)}</span><br /><span className="text-xs opacity-60">Average</span></div>
          </div>
          {(stats.currentStreak ?? 0) >= 2 && <div className="text-sm">🔥 {stats.currentStreak}-match streak</div>}
          {seasons.length > 0 && (
            <Link href={`/predict/${seasons[0].seasonId}`} className="inline-block mt-3 px-5 py-2 rounded-full font-bold text-sm" style={{ background: "var(--sc-tertiary)" }}>
              ⚽ Predict Now
            </Link>
          )}
        </div>
      )}

      {/* Quick Predict CTA (if no stats but has seasons) */}
      {(!gw || !(stats?.totalPredictions)) && seasons.length > 0 && (
        <Link href={`/predict/${seasons[0].seasonId}`} className="block rounded-xl p-4 text-center font-bold text-white" style={{ background: "var(--sc-tertiary)" }}>
          ⚽ Start Predicting
        </Link>
      )}

      {/* Last Replay */}
      {replay && (
        <div className="rounded-xl border border-[var(--sc-border)] p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-bold">📽️ Last Prediction</span>
            {replaySeasonId > 0 && <Link href={`/gw-replay/${replaySeasonId}/${replayGw}`} className="text-xs font-semibold" style={{ color: "var(--sc-tertiary)" }}>Full Replay →</Link>}
          </div>
          <div className="flex items-center justify-center gap-3 mb-2">
            {(replay.homeLogo || replay.homeTeamLogo) && <img src={replay.homeLogo || replay.homeTeamLogo} alt="" className="w-6 h-6" />}
            <span className="text-lg font-extrabold">{replay.homeScore} – {replay.awayScore}</span>
            {(replay.awayLogo || replay.awayTeamLogo) && <img src={replay.awayLogo || replay.awayTeamLogo} alt="" className="w-6 h-6" />}
          </div>
          <div className="text-center text-xs opacity-60">You predicted <strong>{replay.predictedHome ?? replay.predictedHomeScore} – {replay.predictedAway ?? replay.predictedAwayScore}</strong></div>
          {replay.deathMinute && <div className="text-center text-xs text-red-600 mt-1">💀 Died at minute {replay.deathMinute}</div>}
          {replay.outcome === "ExactScore" && <div className="text-center text-xs text-green-600 font-semibold mt-1">🎯 Exact score!</div>}
          {replay.aiCommentary && <div className="mt-2 text-xs italic opacity-80">🎙️ {replay.aiCommentary}</div>}
        </div>
      )}

      {/* Achievements */}
      {stats?.achievements && stats.achievements.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {stats.achievements.map((a) => (
            <span key={a} className="px-3 py-1 rounded-full text-xs font-semibold" style={{ background: "rgba(255,107,53,0.15)", color: "var(--sc-tertiary)" }}>{a}</span>
          ))}
        </div>
      )}

      {/* Competitions */}
      <section>
        <div className="flex items-center justify-between mb-2">
          <span className="font-bold">Competitions</span>
          {seasons.length > 1 && (
            <button onClick={() => setReordering(!reordering)} className="text-xs font-semibold" style={{ color: "var(--sc-tertiary)" }}>
              {reordering ? "✓ Done" : "Reorder"}
            </button>
          )}
        </div>
        <div className="rounded-xl border border-[var(--sc-border)] overflow-hidden divide-y divide-[var(--sc-border)]">
          {seasons.map((s, i) => (
            <div key={s.seasonId} className="flex items-center gap-3 px-4 py-3">
              {reordering && (
                <div className="flex flex-col gap-0.5">
                  <button disabled={i === 0} onClick={() => move(i, i - 1)} className="text-xs disabled:opacity-30">▲</button>
                  <button disabled={i === seasons.length - 1} onClick={() => move(i, i + 1)} className="text-xs disabled:opacity-30">▼</button>
                </div>
              )}
              {s.competitionLogoUrl && <img src={s.competitionLogoUrl} alt="" className="w-8 h-8 object-contain" />}
              <Link href={`/predict/${s.seasonId}`} className="flex-1 min-w-0">
                <div className="font-semibold truncate">{s.competitionName}</div>
                <div className="text-xs opacity-50">{s.seasonName} Season</div>
              </Link>
              {!reordering && <span className="opacity-30">›</span>}
            </div>
          ))}
          {!reordering && (
            <button onClick={() => setShowAddComp(true)} className="flex items-center gap-3 px-4 py-3 w-full text-left" style={{ color: "var(--sc-tertiary)" }}>
              <span className="text-xl">+</span>
              <span className="font-semibold">Add Competition</span>
            </button>
          )}
        </div>
      </section>

      {/* Leagues */}
      <section>
        <div className="flex items-center justify-between mb-2">
          <span className="font-bold">My Leagues</span>
          <div className="flex gap-2">
            <button onClick={() => setShowJoin(true)} className="text-xs font-semibold" style={{ color: "var(--sc-primary)" }}>Join</button>
            <button onClick={() => setShowCreate(true)} className="px-3 py-1 rounded-full text-xs font-bold text-white" style={{ background: "var(--sc-tertiary)" }}>+ Create</button>
          </div>
        </div>
        {loaded && leagues.length === 0 ? (
          <p className="text-sm opacity-50">Create a league or join with an invite code</p>
        ) : (
          <div className="rounded-xl border border-[var(--sc-border)] overflow-hidden divide-y divide-[var(--sc-border)]">
            {leagues.map((l) => (
              <Link key={l.id} href={`/dashboard/${l.id}`} className="flex items-center gap-3 px-4 py-3">
                <div className="flex-1 min-w-0">
                  <div className="font-semibold truncate">{l.name}</div>
                  <div className="text-xs opacity-50">{l.competitionName} · {l.memberCount} members</div>
                </div>
                <button onClick={(e) => { e.preventDefault(); copyCode(l.inviteCode ?? ""); }} className="text-xs font-mono opacity-40 hover:opacity-70" title="Copy invite code">
                  {l.inviteCode} 📋
                </button>
                <span className="opacity-30">›</span>
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* How to Play hint */}
      {loaded && seasons.length === 0 && leagues.length === 0 && (
        <Link href="/how-to-play" className="flex items-center gap-3 px-4 py-3 rounded-xl border border-[var(--sc-border)]">
          <span>❓</span>
          <span className="font-semibold flex-1">How to Play</span>
          <span className="opacity-30">›</span>
        </Link>
      )}

      {/* ── Modals ── */}

      {/* Create League */}
      {showCreate && (
        <Modal onClose={() => setShowCreate(false)} title="Create Prediction League">
          <select value={selectedComp?.id ?? ""} onChange={(e) => setSelectedComp(competitions.find((c) => c.id === +e.target.value) ?? null)}
            className="w-full rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm mb-3">
            <option value="">Select competition</option>
            {competitions.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <input value={newLeagueName} onChange={(e) => setNewLeagueName(e.target.value)} placeholder="League name, e.g. The Lads" maxLength={50}
            className="w-full rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm" />
          <div className="flex gap-3 mt-4">
            <button onClick={() => setShowCreate(false)} className="flex-1 py-2.5 rounded-xl text-sm text-[var(--sc-text-secondary)]">Cancel</button>
            <button onClick={createLeague} disabled={!newLeagueName.trim() || !selectedComp} className="flex-1 py-2.5 rounded-xl text-sm font-bold text-white bg-[var(--sc-tertiary)] disabled:opacity-40">Create</button>
          </div>
        </Modal>
      )}

      {/* Join League */}
      {showJoin && (
        <Modal onClose={() => setShowJoin(false)} title="Join a League">
          <input value={inviteCode} onChange={(e) => setInviteCode(e.target.value)} placeholder="Invite code, e.g. ABC123" maxLength={20}
            className="w-full rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm uppercase" />
          <div className="flex gap-3 mt-4">
            <button onClick={() => setShowJoin(false)} className="flex-1 py-2.5 rounded-xl text-sm text-[var(--sc-text-secondary)]">Cancel</button>
            <button onClick={joinLeague} disabled={!inviteCode.trim()} className="flex-1 py-2.5 rounded-xl text-sm font-bold text-white bg-[var(--sc-primary)] disabled:opacity-40">Join</button>
          </div>
        </Modal>
      )}

      {/* Add Competition */}
      {showAddComp && (
        <Modal onClose={() => setShowAddComp(false)} title="Add Competition">
          <select value={selectedComp?.id ?? ""} onChange={(e) => setSelectedComp(competitions.find((c) => c.id === +e.target.value) ?? null)}
            className="w-full rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm">
            <option value="">Select competition</option>
            {competitions.map((c) => {
              const active = seasons.some((s) => s.competitionCode === c.code);
              return <option key={c.id} value={c.id} disabled={active}>{c.name}{active ? " (Active)" : ""}</option>;
            })}
          </select>
          <div className="flex gap-3 mt-4">
            <button onClick={() => setShowAddComp(false)} className="flex-1 py-2.5 rounded-xl text-sm text-[var(--sc-text-secondary)]">Cancel</button>
            <button onClick={addCompetition} disabled={!selectedComp} className="flex-1 py-2.5 rounded-xl text-sm font-bold text-white bg-[var(--sc-tertiary)] disabled:opacity-40">Go</button>
          </div>
        </Modal>
      )}
    </div>
  );
}

function Modal({ children, onClose, title }: { children: React.ReactNode; onClose: () => void; title: string }) {
  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4" onClick={onClose}>
      <div className="w-full max-w-sm rounded-2xl bg-[var(--sc-surface)] shadow-2xl p-6" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-lg font-bold mb-4">{title}</h3>
        {children}
      </div>
    </div>
  );
}
