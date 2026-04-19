"use client";

import { useAuth } from "@/contexts/auth-context";
import { useAlert } from "@/contexts/alert-context";
import { api } from "@/lib/api";
import { TeamResult, UserProfileResult } from "@/lib/types";
import { useRouter } from "next/navigation";
import { useEffect, useState, useRef } from "react";

const PROFANITY = ["fuck","shit","ass","bitch","damn","crap","dick","bastard","cunt","piss"];
function hasProfanity(s: string) { return PROFANITY.some((w) => s.toLowerCase().includes(w)); }

export default function SettingsPage() {
  const { user, signOut } = useAuth();
  const { add } = useAlert();
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfileResult | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [username, setUsername] = useState("");
  const [teams, setTeams] = useState<TeamResult[]>([]);
  const [selectedTeam, setSelectedTeam] = useState<TeamResult | null>(null);
  const [teamQuery, setTeamQuery] = useState("");
  const [showTeamDropdown, setShowTeamDropdown] = useState(false);
  const [saving, setSaving] = useState(false);
  const [usernameError, setUsernameError] = useState("");
  const [modal, setModal] = useState<"logout" | "delete" | null>(null);
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
        const unique = Array.from(new Map(all.map((t) => [t.id, t])).values()).sort((a, b) => a.name.localeCompare(b.name));
        setTeams(unique);
        if (p.data?.favouriteTeamName) {
          setSelectedTeam(unique.find((t) => t.name === p.data!.favouriteTeamName) ?? null);
        }
      }
    })();
  }, [user]);

  const filteredTeams = teamQuery ? teams.filter((t) => t.name.toLowerCase().includes(teamQuery.toLowerCase())) : teams;

  const hasChanges = profile && (
    displayName !== (profile.displayName ?? "") ||
    username !== (profile.userId ?? "") ||
    selectedTeam?.name !== (profile.favouriteTeamName ?? "")
  );

  const save = async () => {
    if (!profile) return;
    if (hasProfanity(displayName)) { add("Display name contains inappropriate language", "error"); return; }
    setUsernameError("");
    setSaving(true);
    try {
      if (username.trim().toLowerCase() !== profile.userId) {
        const r = await api.setUsername({ username: username.trim() });
        if (!r.success) { setUsernameError(r.message ?? "Username not available"); setSaving(false); return; }
      }
      const r = await api.updateMyProfile({ displayName: displayName.trim(), favouriteTeamId: selectedTeam?.id });
      if (r.success && r.data) {
        setProfile(r.data);
        setDisplayName(r.data.displayName ?? "");
        setUsername(r.data.userId ?? "");
        setSelectedTeam(teams.find((t) => t.name === r.data!.favouriteTeamName) ?? null);
        add("Profile updated", "success");
      }
    } finally { setSaving(false); }
  };

  const handleLogout = async () => { await signOut(); router.replace("/login"); };
  const handleDelete = async () => { await signOut(); router.replace("/login"); };

  if (!profile) return <div className="flex justify-center p-8"><div className="text-4xl animate-pulse">⚽</div></div>;

  return (
    <div className="max-w-md mx-auto p-4">
      {/* Avatar & Info */}
      <div className="flex flex-col items-center mb-6">
        <div className="w-16 h-16 rounded-full bg-[var(--sc-primary)] text-white flex items-center justify-center text-2xl font-bold mb-2">
          {(profile.displayName?.[0] ?? "?").toUpperCase()}
        </div>
        <p className="font-bold text-lg">{profile.displayName ?? profile.userId}</p>
        <p className="text-sm text-[var(--sc-text-secondary)]">@{profile.userId}</p>
        <p className="text-sm text-[var(--sc-text-secondary)]">{profile.email}</p>
        <div className="flex items-center gap-1 mt-1">
          {user?.emailVerified && <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-semibold">✓ Verified</span>}
        </div>
        <p className="text-xs text-[var(--sc-text-secondary)] mt-1">
          Member since {new Date(profile.memberSince).toLocaleDateString("en-US", { month: "short", year: "numeric" })}
        </p>
      </div>

      {/* Profile Form */}
      <div className="bg-[var(--sc-surface)] rounded-xl p-4 mb-4 shadow-sm">
        <h3 className="text-xs font-bold uppercase tracking-wider text-[var(--sc-text-secondary)] mb-3">✏️ Profile</h3>
        <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} maxLength={30}
          placeholder="Display Name"
          className="w-full px-3 py-2 rounded-lg border border-[var(--sc-border)] text-sm mb-3 focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
        <div className="relative mb-3">
          <div className="flex items-center">
            <span className="text-[var(--sc-text-secondary)] text-sm mr-1">@</span>
            <input value={username} onChange={(e) => setUsername(e.target.value)} maxLength={20}
              placeholder="Username"
              className="flex-1 px-3 py-2 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          </div>
          {usernameError && <p className="text-xs text-red-500 mt-1">{usernameError}</p>}
        </div>

        {/* Team autocomplete */}
        <div className="relative mb-3" ref={dropdownRef}>
          <div className="flex items-center gap-2">
            {selectedTeam?.logoUrl && <img src={selectedTeam.logoUrl} alt="" className="w-6 h-6 object-contain" />}
            <input value={teamQuery}
              onChange={(e) => { setTeamQuery(e.target.value); setShowTeamDropdown(true); setSelectedTeam(null); }}
              onFocus={() => setShowTeamDropdown(true)}
              placeholder="Favourite Team"
              className="flex-1 px-3 py-2 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          </div>
          {showTeamDropdown && filteredTeams.length > 0 && (
            <div className="absolute z-20 w-full mt-1 bg-[var(--sc-surface)] border border-[var(--sc-border)] rounded-lg max-h-48 overflow-y-auto shadow-lg">
              {filteredTeams.slice(0, 50).map((t) => (
                <button key={t.id} className="flex items-center gap-2 w-full px-3 py-2 hover:bg-gray-50 text-left text-sm"
                  onClick={() => { setSelectedTeam(t); setTeamQuery(t.name); setShowTeamDropdown(false); }}>
                  {t.logoUrl ? <img src={t.logoUrl} alt="" className="w-5 h-5 object-contain" /> : <span>🛡️</span>}
                  {t.name}
                </button>
              ))}
            </div>
          )}
        </div>

        <button onClick={save} disabled={!hasChanges || saving}
          className="w-full py-2.5 rounded-lg bg-[var(--sc-primary)] text-white font-semibold text-sm disabled:opacity-40">
          {saving ? "Saving..." : "Save Changes"}
        </button>
      </div>

      {/* Logout */}
      <button onClick={() => setModal("logout")}
        className="w-full border border-red-500 text-red-500 rounded-lg py-2.5 font-semibold text-sm mb-2">
        Log Out
      </button>
      <button onClick={() => setModal("delete")}
        className="w-full text-red-400 text-xs hover:underline">
        Delete Account
      </button>

      {/* Confirm Modal */}
      {modal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setModal(null)}>
          <div className="bg-[var(--sc-surface)] rounded-xl p-6 max-w-xs w-full shadow-lg" onClick={(e) => e.stopPropagation()}>
            <p className="font-bold text-lg mb-2">{modal === "logout" ? "Log Out" : "Delete Account"}</p>
            <p className="text-sm text-[var(--sc-text-secondary)] mb-4">
              {modal === "logout" ? "Are you sure you want to log out?" : "This will sign you out. Account deletion is permanent."}
            </p>
            <div className="flex gap-3">
              <button onClick={() => setModal(null)} className="flex-1 border border-[var(--sc-border)] rounded-lg py-2 text-sm">Cancel</button>
              <button onClick={modal === "logout" ? handleLogout : handleDelete}
                className="flex-1 bg-red-600 text-white rounded-lg py-2 font-bold text-sm">
                {modal === "logout" ? "Log Out" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
