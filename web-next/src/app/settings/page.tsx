"use client";

import { useAuth } from "@/contexts/auth-context";
import { api } from "@/lib/api";
import { TeamResult, UserProfileResult } from "@/lib/types";
import { useRouter } from "next/navigation";
import { useEffect, useState, useRef } from "react";

export default function SettingsPage() {
  const { user, signOut } = useAuth();
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfileResult | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [username, setUsername] = useState("");
  const [teams, setTeams] = useState<TeamResult[]>([]);
  const [selectedTeam, setSelectedTeam] = useState<TeamResult | null>(null);
  const [teamQuery, setTeamQuery] = useState("");
  const [showTeamDropdown, setShowTeamDropdown] = useState(false);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");
  const [showLogout, setShowLogout] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!user) return;
    (async () => {
      const [p, comps] = await Promise.all([api.getMyProfile(), api.getCompetitions()]);
      if (p.success && p.data) {
        setProfile(p.data);
        setDisplayName(p.data.displayName ?? "");
        setUsername(p.data.userId ?? "");
        setTeamQuery(p.data.favouriteTeamName ?? "");
      }
      if (comps.success && comps.data) {
        const all: TeamResult[] = [];
        for (const c of comps.data) {
          const t = await api.searchTeams(c.name);
          if (t.success && t.data) all.push(...t.data.teams);
        }
        const unique = Array.from(new Map(all.map(t => [t.id, t])).values()).sort((a, b) => a.name.localeCompare(b.name));
        setTeams(unique);
        if (p.data?.favouriteTeamName) {
          const match = unique.find(t => t.name === p.data!.favouriteTeamName);
          if (match) setSelectedTeam(match);
        }
      }
    })();
  }, [user]);

  const filteredTeams = teamQuery
    ? teams.filter(t => t.name.toLowerCase().includes(teamQuery.toLowerCase()))
    : teams;

  const hasChanges = profile && (
    displayName !== (profile.displayName ?? "") ||
    username !== (profile.userId ?? "") ||
    selectedTeam?.name !== (profile.favouriteTeamName ?? "")
  );

  const save = async () => {
    if (!profile) return;
    setSaving(true);
    setMsg("");
    if (username.trim().toLowerCase() !== profile.userId) {
      const r = await api.setUsername({ username: username.trim() });
      if (!r.success) { setMsg(r.message ?? "Username not available"); setSaving(false); return; }
    }
    const r = await api.updateMyProfile({ displayName: displayName.trim(), favoriteTeam: selectedTeam?.name });
    if (r.success && r.data) {
      setProfile(r.data);
      setDisplayName(r.data.displayName ?? "");
      setUsername(r.data.userId ?? "");
      setMsg("Profile updated!");
    }
    setSaving(false);
  };

  const handleLogout = async () => {
    await signOut();
    router.replace("/login");
  };

  if (!profile) return <div className="flex justify-center p-8 text-white">Loading...</div>;

  return (
    <div className="max-w-md mx-auto p-4 text-white">
      {/* Avatar & Info */}
      <div className="flex flex-col items-center mb-6">
        <div className="w-16 h-16 rounded-full bg-[#0A1929] border-2 border-[#FF6B35] flex items-center justify-center text-2xl font-bold mb-2">
          {(profile.displayName?.[0] ?? "?").toUpperCase()}
        </div>
        <p className="font-bold text-lg">{profile.displayName ?? profile.userId}</p>
        <p className="text-sm opacity-60">{profile.email}</p>
        <p className="text-sm opacity-60">Member since {new Date(profile.memberSince).toLocaleDateString("en-US", { month: "short", year: "numeric" })}</p>
      </div>

      {/* Profile Form */}
      <div className="bg-white/5 rounded-xl p-4 mb-4">
        <h3 className="text-xs font-bold uppercase tracking-wider mb-3 opacity-70">✏️ Profile</h3>
        <input value={displayName} onChange={e => setDisplayName(e.target.value)} maxLength={30}
          placeholder="Display Name" className="w-full bg-white/10 rounded-lg px-3 py-2 mb-3 outline-none" />
        <input value={username} onChange={e => setUsername(e.target.value)} maxLength={20}
          placeholder="Username" className="w-full bg-white/10 rounded-lg px-3 py-2 mb-3 outline-none" />

        {/* Team autocomplete */}
        <div className="relative mb-3" ref={dropdownRef}>
          <div className="flex items-center gap-2">
            {selectedTeam?.logoUrl && <img src={selectedTeam.logoUrl} alt="" className="w-6 h-6 object-contain" />}
            <input value={teamQuery}
              onChange={e => { setTeamQuery(e.target.value); setShowTeamDropdown(true); setSelectedTeam(null); }}
              onFocus={() => setShowTeamDropdown(true)}
              placeholder="Favourite Team" className="flex-1 bg-white/10 rounded-lg px-3 py-2 outline-none" />
          </div>
          {showTeamDropdown && filteredTeams.length > 0 && (
            <div className="absolute z-20 w-full mt-1 bg-[#0A1929] border border-white/20 rounded-lg max-h-48 overflow-y-auto">
              {filteredTeams.slice(0, 50).map(t => (
                <button key={t.id} className="flex items-center gap-2 w-full px-3 py-2 hover:bg-white/10 text-left text-sm"
                  onClick={() => { setSelectedTeam(t); setTeamQuery(t.name); setShowTeamDropdown(false); }}>
                  {t.logoUrl ? <img src={t.logoUrl} alt="" className="w-5 h-5 object-contain" /> : <span>🛡️</span>}
                  {t.name}
                </button>
              ))}
            </div>
          )}
        </div>

        {msg && <p className="text-sm mb-2 text-[#FF6B35]">{msg}</p>}
        <button onClick={save} disabled={!hasChanges || saving}
          className="w-full bg-[#0A1929] border border-[#FF6B35] rounded-lg py-2 font-bold disabled:opacity-40">
          {saving ? "Saving..." : "Save Changes"}
        </button>
      </div>

      {/* Logout */}
      <button onClick={() => setShowLogout(true)}
        className="w-full border border-red-500 text-red-400 rounded-lg py-2 font-bold">
        Log Out
      </button>

      {showLogout && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50" onClick={() => setShowLogout(false)}>
          <div className="bg-[#0A1929] border border-white/20 rounded-xl p-6 max-w-xs w-full" onClick={e => e.stopPropagation()}>
            <p className="font-bold text-lg mb-2">Log Out</p>
            <p className="text-sm opacity-70 mb-4">Are you sure you want to log out?</p>
            <div className="flex gap-3">
              <button onClick={() => setShowLogout(false)} className="flex-1 border border-white/20 rounded-lg py-2">Cancel</button>
              <button onClick={handleLogout} className="flex-1 bg-red-600 rounded-lg py-2 font-bold">Log Out</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
