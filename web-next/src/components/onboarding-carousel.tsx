"use client";

import { useState, useEffect, useCallback } from "react";
import { api } from "@/lib/api";
import { useAuth } from "@/contexts/auth-context";
import type { TeamResult } from "@/lib/types";

const STEPS = [
  { emoji: "👋", title: "Welcome to ScoreCast!", bg: "from-[var(--sc-primary)] to-[#1a2d45]" },
  { emoji: "🏆", title: "Predict & Score", bg: "from-green-800 to-green-600" },
  { emoji: "👥", title: "Leagues & Community", bg: "from-orange-700 to-orange-500" },
  { emoji: "📱", title: "Install on Your Phone", bg: "from-[#0A1929] to-indigo-900" },
  { emoji: "✏️", title: "Your Display Name", bg: "from-purple-900 to-purple-600" },
  { emoji: "🛡️", title: "Pick Your Team", bg: "from-[#37003C] to-purple-700" },
];

const PROFANITY_RE = /\b(ass|shit|fuck|damn|bitch|crap|dick|piss|cock|cunt|bastard|slut|whore)\b/i;

export function OnboardingCarousel() {
  const { user, completeOnboarding, profile } = useAuth();
  const [step, setStep] = useState(0);
  const [displayName, setDisplayName] = useState("");
  const [nameError, setNameError] = useState("");
  const [selectedTeam, setSelectedTeam] = useState<TeamResult | null>(null);
  const [teamQuery, setTeamQuery] = useState("");
  const [teamResults, setTeamResults] = useState<TeamResult[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setDisplayName(profile?.displayName || user?.displayName || user?.email?.split("@")[0] || "");
  }, [profile, user]);

  useEffect(() => {
    if (teamQuery.length < 1) { setTeamResults([]); return; }
    const t = setTimeout(async () => {
      const r = await api.searchTeams(teamQuery, 0, 30);
      if (r.success && r.data) setTeamResults(r.data.teams || []);
    }, 300);
    return () => clearTimeout(t);
  }, [teamQuery]);

  const validateName = useCallback((n: string) => {
    const t = n.trim();
    if (t.length < 2) return "Must be at least 2 characters";
    if (t.length > 30) return "Must be 30 characters or less";
    if (PROFANITY_RE.test(t)) return "Please choose an appropriate name";
    return "";
  }, []);

  const next = () => {
    if (step === 4) {
      const err = validateName(displayName);
      if (err) { setNameError(err); return; }
    }
    setStep((s) => Math.min(s + 1, 5));
  };

  const finish = async () => {
    setSaving(true);
    try {
      await api.updateMyProfile({
        displayName: displayName.trim(),
        hasCompletedOnboarding: true,
        ...(selectedTeam ? { favouriteTeamId: selectedTeam.id, favoriteTeam: selectedTeam.name } : {}),
      });
      completeOnboarding();
    } catch { /* ignore */ } finally { setSaving(false); }
  };

  const skip = async () => {
    setSaving(true);
    try {
      await api.updateMyProfile({ hasCompletedOnboarding: true });
      completeOnboarding();
    } catch { /* ignore */ } finally { setSaving(false); }
  };

  const s = STEPS[step];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-md rounded-2xl overflow-hidden bg-[var(--sc-surface)] shadow-2xl max-h-[90vh] flex flex-col">
        {/* Hero */}
        <div className={`bg-gradient-to-br ${s.bg} p-8 text-center text-white`}>
          <div className="text-5xl mb-2">{s.emoji}</div>
          <h2 className="text-xl font-extrabold">{s.title}</h2>
        </div>

        {/* Content */}
        <div className="p-6 flex-1 overflow-y-auto">
          {step === 0 && (
            <p className="text-sm text-[var(--sc-text-secondary)] leading-relaxed">
              ScoreCast is a free football predictions app where you compete with friends and the community.
              Predict match scores, climb leaderboards, and prove you know the beautiful game best.
            </p>
          )}
          {step === 1 && (
            <div className="space-y-2 text-sm text-[var(--sc-text-secondary)]">
              <p>Predict scorelines and earn points based on accuracy:</p>
              <div className="grid grid-cols-2 gap-2 mt-3">
                {[["🎯 Exact Score", "10 pts"], ["📐 Result + GD", "7 pts"], ["✅ Correct Result", "5 pts"], ["📏 Goal Difference", "3 pts"]].map(([l, p]) => (
                  <div key={l} className="rounded-lg bg-[var(--sc-bg)] p-3 text-center">
                    <div className="text-xs">{l}</div>
                    <div className="font-bold text-[var(--sc-tertiary)]">{p}</div>
                  </div>
                ))}
              </div>
              <p className="mt-2">Use <strong>Risk Plays</strong> to double points on matches you&apos;re confident about!</p>
            </div>
          )}
          {step === 2 && (
            <p className="text-sm text-[var(--sc-text-secondary)] leading-relaxed">
              Create prediction leagues and invite friends with a code, or join existing ones.
              Track live scores, browse AI match insights, and watch goal highlights.
            </p>
          )}
          {step === 3 && (
            <p className="text-sm text-[var(--sc-text-secondary)] leading-relaxed">
              ScoreCast works as an app on your phone — no app store needed.
              For the best experience, use <strong>Google Chrome</strong>.
              You&apos;ll find the <strong>Install Guide</strong> in the menu after setup.
            </p>
          )}
          {step === 4 && (
            <div>
              <input
                value={displayName}
                onChange={(e) => { setDisplayName(e.target.value); setNameError(""); }}
                placeholder="How others will see you"
                maxLength={30}
                className="w-full rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm outline-none focus:ring-2 focus:ring-[var(--sc-primary)]"
              />
              {nameError && <p className="text-xs text-red-500 mt-1">{nameError}</p>}
              <p className="text-xs text-[var(--sc-text-secondary)] mt-1">2–30 characters</p>
            </div>
          )}
          {step === 5 && (
            <div className="relative">
              <div className="flex items-center gap-3 mb-2">
                {selectedTeam?.logoUrl && <img src={selectedTeam.logoUrl} alt="" className="w-9 h-9 object-contain" />}
                <input
                  value={selectedTeam ? selectedTeam.name : teamQuery}
                  onChange={(e) => { setSelectedTeam(null); setTeamQuery(e.target.value); setShowDropdown(true); }}
                  onFocus={() => setShowDropdown(true)}
                  placeholder="Search any team..."
                  className="flex-1 rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm outline-none focus:ring-2 focus:ring-[var(--sc-primary)]"
                />
              </div>
              {showDropdown && teamResults.length > 0 && (
                <div className="absolute left-0 right-0 top-full z-10 max-h-48 overflow-y-auto rounded-lg border border-[var(--sc-border)] bg-[var(--sc-surface)] shadow-lg">
                  {teamResults.map((t) => (
                    <button key={t.id} className="flex items-center gap-2 w-full px-4 py-2 text-sm hover:bg-[var(--sc-bg)] text-left" onClick={() => { setSelectedTeam(t); setShowDropdown(false); }}>
                      {t.logoUrl && <img src={t.logoUrl} alt="" className="w-6 h-6 object-contain" />}
                      {t.name}
                    </button>
                  ))}
                </div>
              )}
              <p className="text-xs text-[var(--sc-text-secondary)] mt-1">Optional — shown on your profile</p>
            </div>
          )}
        </div>

        {/* Progress dots + nav */}
        <div className="px-6 pb-5">
          <div className="flex justify-center gap-1 mb-4">
            {STEPS.map((_, i) => (
              <div key={i} className="h-1.5 rounded-full transition-all" style={{ width: i === step ? 20 : 6, background: i === step ? "var(--sc-primary)" : "var(--sc-border)" }} />
            ))}
          </div>
          <div className="flex gap-3">
            {step > 0 && <button onClick={() => setStep((s) => s - 1)} className="flex-1 py-2.5 rounded-xl text-sm font-semibold text-[var(--sc-text-secondary)]">Back</button>}
            {step < 5 ? (
              <>
                <button onClick={skip} className="py-2.5 px-4 rounded-xl text-xs text-[var(--sc-text-secondary)]">Skip</button>
                <button onClick={next} className="flex-[2] py-2.5 rounded-xl text-sm font-bold text-white bg-[var(--sc-primary)]">Next</button>
              </>
            ) : (
              <button onClick={finish} disabled={saving} className="flex-[2] py-2.5 rounded-xl text-sm font-bold text-white" style={{ background: "var(--sc-tertiary)" }}>
                {saving ? "Saving..." : "🚀 Start Predicting!"}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
