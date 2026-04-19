"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { FirebaseError } from "firebase/app";

const PROFANITY = ["fuck","shit","ass","bitch","damn","crap","dick","bastard","cunt","piss"];

function hasProfanity(s: string) {
  const lower = s.toLowerCase();
  return PROFANITY.some((w) => lower.includes(w));
}

function passwordStrength(p: string): { label: string; color: string; pct: number } {
  let score = 0;
  if (p.length >= 8) score++;
  if (p.length >= 12) score++;
  if (/[A-Z]/.test(p) && /[a-z]/.test(p)) score++;
  if (/\d/.test(p)) score++;
  if (/[^A-Za-z0-9]/.test(p)) score++;
  if (score <= 1) return { label: "Weak", color: "#f44336", pct: 20 };
  if (score <= 3) return { label: "Medium", color: "#FF6B35", pct: 60 };
  return { label: "Strong", color: "#4CAF50", pct: 100 };
}

export default function RegisterPage() {
  const { signUp, signInWithGoogle } = useAuth();
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [terms, setTerms] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const strength = passwordStrength(password);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (hasProfanity(displayName)) { setError("Display name contains inappropriate language"); return; }
    if (password.length < 8) { setError("Password must be at least 8 characters"); return; }
    if (password !== confirm) { setError("Passwords do not match"); return; }
    if (!terms) { setError("You must agree to the terms"); return; }

    setLoading(true);
    try {
      await signUp(email, password, displayName.trim());
      router.replace("/verify-email");
    } catch (err) {
      const msg = err instanceof FirebaseError
        ? (err.code === "auth/email-already-in-use" ? "Email already in use" : err.message)
        : (err instanceof Error ? err.message : "Registration failed");
      setError(msg);
    } finally { setLoading(false); }
  };

  const handleGoogle = async () => {
    setError("");
    try {
      await signInWithGoogle();
      router.replace("/dashboard");
    } catch (err) {
      if (err instanceof FirebaseError && err.code === "auth/popup-closed-by-user") return;
      setError("Google sign-in failed");
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm">
        <div className="text-center mb-6">
          <div className="text-4xl mb-2">⚽</div>
          <h1 className="text-2xl font-bold">Create Account</h1>
          <p className="text-sm text-[var(--sc-text-secondary)]">Join the prediction game</p>
        </div>

        {error && <div className="bg-red-50 text-red-600 text-sm rounded-lg p-3 mb-4">{error}</div>}

        <button onClick={handleGoogle}
          className="w-full py-2.5 rounded-lg border border-[var(--sc-border)] text-sm font-medium hover:bg-gray-50 mb-4 flex items-center justify-center gap-2">
          <svg width="18" height="18" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/><path fill="#FBBC05" d="M10.53 28.59a14.5 14.5 0 0 1 0-9.18l-7.98-6.19a24 24 0 0 0 0 21.56l7.98-6.19z"/><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/></svg>
          Continue with Google
        </button>

        <div className="flex items-center gap-3 my-4">
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
          <span className="text-xs text-[var(--sc-text-secondary)]">or create with email</span>
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
        </div>

        <form onSubmit={handleSubmit} className="space-y-3">
          <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)}
            required autoComplete="email" autoFocus
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          <input type="text" placeholder="Display Name" value={displayName} onChange={(e) => setDisplayName(e.target.value)}
            required maxLength={30}
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          <div>
            <input type="password" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
            {password && (
              <div className="mt-1.5">
                <div className="h-1.5 rounded-full bg-gray-200 overflow-hidden">
                  <div className="h-full rounded-full transition-all" style={{ width: `${strength.pct}%`, background: strength.color }} />
                </div>
                <p className="text-xs mt-0.5 font-semibold" style={{ color: strength.color }}>{strength.label}</p>
              </div>
            )}
          </div>
          <div>
            <input type="password" placeholder="Confirm Password" value={confirm} onChange={(e) => setConfirm(e.target.value)}
              required
              className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
            {confirm && (
              <p className={`text-xs mt-1 font-semibold ${password === confirm ? "text-green-600" : "text-red-500"}`}>
                {password === confirm ? "✓ Passwords match" : "✗ Passwords do not match"}
              </p>
            )}
          </div>
          <p className="text-xs text-[var(--sc-text-secondary)]">
            Password must contain: 8–32 chars, 1 uppercase &amp; 1 lowercase, 1 digit &amp; 1 special character
          </p>
          <label className="flex items-start gap-2 text-sm">
            <input type="checkbox" checked={terms} onChange={(e) => setTerms(e.target.checked)} className="mt-0.5" />
            <span className="text-[var(--sc-text-secondary)]">
              I agree to the <Link href="/how-to-play" className="text-[var(--sc-secondary)] underline">Terms & Rules</Link>
            </span>
          </label>
          <button type="submit" disabled={loading}
            className="w-full py-2.5 rounded-lg bg-[var(--sc-tertiary)] text-white text-sm font-semibold disabled:opacity-50">
            {loading ? "Creating account..." : "Create Account"}
          </button>
        </form>

        <hr className="my-4 border-[var(--sc-border)]" />
        <p className="text-center text-sm text-[var(--sc-text-secondary)]">
          Already have an account? <Link href="/login" className="text-[var(--sc-secondary)] font-medium hover:underline">Log In</Link>
        </p>
      </div>
    </div>
  );
}
