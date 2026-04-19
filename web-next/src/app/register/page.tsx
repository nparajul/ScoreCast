"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import Link from "next/link";

export default function RegisterPage() {
  const { signUp } = useAuth();
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (password.length < 6) { setError("Password must be at least 6 characters"); return; }
    if (password !== confirm) { setError("Passwords do not match"); return; }
    if (!displayName.trim()) { setError("Display name is required"); return; }

    setLoading(true);
    try {
      await signUp(email, password, displayName.trim());
      router.push("/dashboard");
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Registration failed";
      setError(msg.includes("email-already-in-use") ? "Email already in use" : msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm">
        <h1 className="text-2xl font-bold text-center mb-1">Create account</h1>
        <p className="text-sm text-[var(--sc-text-secondary)] text-center mb-6">Join ScoreCast — free forever</p>

        {error && <div className="bg-red-50 text-red-600 text-sm rounded-lg p-3 mb-4">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="text" placeholder="Display name" value={displayName} onChange={(e) => setDisplayName(e.target.value)}
            required className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <input
            type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)}
            required autoComplete="email"
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <input
            type="password" placeholder="Password (min 6 chars)" value={password} onChange={(e) => setPassword(e.target.value)}
            required className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <input
            type="password" placeholder="Confirm password" value={confirm} onChange={(e) => setConfirm(e.target.value)}
            required className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <button
            type="submit" disabled={loading}
            className="w-full py-2.5 rounded-lg bg-[var(--sc-primary)] text-white text-sm font-semibold disabled:opacity-50"
          >
            {loading ? "Creating account..." : "Sign Up"}
          </button>
        </form>

        <p className="mt-4 text-center text-sm text-[var(--sc-text-secondary)]">
          Already have an account? <Link href="/login" className="text-[var(--sc-secondary)] font-medium hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  );
}
