"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import Link from "next/link";

export default function LoginPage() {
  const { signIn, signInWithGoogle } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await signIn(email, password);
      router.push("/dashboard");
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Login failed";
      setError(msg.includes("invalid-credential") ? "Invalid email or password" : msg);
    } finally {
      setLoading(false);
    }
  };

  const handleGoogle = async () => {
    try {
      await signInWithGoogle();
      router.push("/dashboard");
    } catch {
      setError("Google sign-in failed");
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm">
        <h1 className="text-2xl font-bold text-center mb-1">Welcome back</h1>
        <p className="text-sm text-[var(--sc-text-secondary)] text-center mb-6">Sign in to ScoreCast</p>

        {error && <div className="bg-red-50 text-red-600 text-sm rounded-lg p-3 mb-4">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)}
            required autoComplete="email"
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <input
            type="password" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)}
            required autoComplete="current-password"
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
          />
          <button
            type="submit" disabled={loading}
            className="w-full py-2.5 rounded-lg bg-[var(--sc-primary)] text-white text-sm font-semibold disabled:opacity-50"
          >
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <div className="flex items-center gap-3 my-4">
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
          <span className="text-xs text-[var(--sc-text-secondary)]">or</span>
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
        </div>

        <button
          onClick={handleGoogle}
          className="w-full py-2.5 rounded-lg border border-[var(--sc-border)] text-sm font-medium hover:bg-gray-50"
        >
          Continue with Google
        </button>

        <div className="mt-4 text-center text-sm space-y-2">
          <Link href="/forgot-password" className="text-[var(--sc-secondary)] hover:underline block">
            Forgot password?
          </Link>
          <p className="text-[var(--sc-text-secondary)]">
            No account? <Link href="/register" className="text-[var(--sc-secondary)] font-medium hover:underline">Sign up</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
