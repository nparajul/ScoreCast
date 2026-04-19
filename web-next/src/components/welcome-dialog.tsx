"use client";

import { useState, useEffect } from "react";
import { api } from "@/lib/api";
import { useAuth } from "@/contexts/auth-context";
import type { TeamResult } from "@/lib/types";

const PROFANITY_RE = /\b(ass|shit|fuck|damn|bitch|crap|dick|piss|cock|cunt|bastard|slut|whore)\b/i;

export function WelcomeDialog({ onClose }: { onClose: () => void }) {
  const { user, profile } = useAuth();
  const [displayName, setDisplayName] = useState("");
  const [nameError, setNameError] = useState("");
  const [selectedTeam, setSelectedTeam] = useState<TeamResult | null>(null);
  const [teamQuery, setTeamQuery] = useState("");
  const [teamResults, setTeamResults] = useState<TeamResult[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setDisplayName(profile?.displayName || user?.displayName || "");
  }, [profile, user]);

  useEffect(() => {
    if (teamQuery.length < 1) { setTeamResults([]); return; }
    const t = setTimeout(async () => {
      const r = await api.searchTeams(teamQuery, 0, 30);
      if (r.success && r.data) setTeamResults(r.data.teams || []);
    }, 300);
    return () => clearTimeout(t);
  }, [teamQuery]);

  const save = async () => {
    const t = displayName.trim();
    if (t.length < 2) { setNameError("Must be at least 2 characters"); return; }
    if (PROFANITY_RE.test(t)) { setNameError("Please choose an appropriate name"); return; }
    setSaving(true);
    try {
      await api.updateMyProfile({
        displayName: t,
        hasCompletedOnboarding: true,
        ...(selectedTeam ? { favouriteTeamId: selectedTeam.id, favoriteTeam: selectedTeam.name } : {}),
      });
      onClose();
    } catch { /* ignore */ } finally { setSaving(false); }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-sm rounded-2xl bg-[var(--sc-surface)] shadow-2xl p-6 space-y-4">
        <h2 className="text-lg font-extrabold text-center">Complete Your Profile</h2>
        <div>
          <label className="text-xs font-semibold text-[var(--sc-text-secondary)]">Display Name</label>
          <input value={displayName} onChange={(e) => { setDisplayName(e.target.value); setNameError(""); }} maxLength={30}
            className="w-full mt-1 rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm outline-none focus:ring-2 focus:ring-[var(--sc-primary)]" />
          {nameError && <p className="text-xs text-red-500 mt-1">{nameError}</p>}
        </div>
        <div className="relative">
          <label className="text-xs font-semibold text-[var(--sc-text-secondary)]">Favourite Team (optional)</label>
          <div className="flex items-center gap-2 mt-1">
            {selectedTeam?.logoUrl && <img src={selectedTeam.logoUrl} alt="" className="w-8 h-8 object-contain" />}
            <input value={selectedTeam ? selectedTeam.name : teamQuery}
              onChange={(e) => { setSelectedTeam(null); setTeamQuery(e.target.value); setShowDropdown(true); }}
              onFocus={() => setShowDropdown(true)} placeholder="Search..."
              className="flex-1 rounded-lg border border-[var(--sc-border)] bg-[var(--sc-bg)] px-4 py-3 text-sm outline-none focus:ring-2 focus:ring-[var(--sc-primary)]" />
          </div>
          {showDropdown && teamResults.length > 0 && (
            <div className="absolute left-0 right-0 z-10 max-h-40 overflow-y-auto rounded-lg border border-[var(--sc-border)] bg-[var(--sc-surface)] shadow-lg mt-1">
              {teamResults.map((t) => (
                <button key={t.id} className="flex items-center gap-2 w-full px-4 py-2 text-sm hover:bg-[var(--sc-bg)] text-left" onClick={() => { setSelectedTeam(t); setShowDropdown(false); }}>
                  {t.logoUrl && <img src={t.logoUrl} alt="" className="w-5 h-5 object-contain" />}
                  {t.name}
                </button>
              ))}
            </div>
          )}
        </div>
        <button onClick={save} disabled={saving} className="w-full py-3 rounded-xl text-sm font-bold text-white bg-[var(--sc-primary)]">
          {saving ? "Saving..." : "Save & Continue"}
        </button>
      </div>
    </div>
  );
}
