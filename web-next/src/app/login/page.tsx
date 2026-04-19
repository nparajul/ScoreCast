"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { FirebaseError } from "firebase/app";

const firebaseErrorMap: Record<string, string> = {
  "auth/invalid-credential": "Invalid email or password",
  "auth/user-not-found": "No account found with this email",
  "auth/wrong-password": "Incorrect password",
  "auth/too-many-requests": "Too many attempts. Try again later.",
  "auth/user-disabled": "This account has been disabled",
  "auth/network-request-failed": "Network error. Check your connection.",
  "auth/popup-closed-by-user": "Sign-in cancelled",
};

function mapError(err: unknown): string {
  if (err instanceof FirebaseError) return firebaseErrorMap[err.code] ?? err.message;
  return err instanceof Error ? err.message : "Login failed";
}

export default function LoginPage() {
  const { signIn, signInWithGoogle, user, resetPassword } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  const redirectAfterLogin = () => {
    const u = user ?? null;
    if (u && !u.emailVerified) router.replace("/verify-email");
    else router.replace("/dashboard");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(""); setSuccess("");
    setLoading(true);
    try {
      await signIn(email, password);
      // Check email verification after sign in
      const { auth } = await import("@/lib/firebase");
      const u = auth.currentUser;
      if (u && !u.emailVerified) router.replace("/verify-email");
      else router.replace("/dashboard");
    } catch (err) {
      setError(mapError(err));
    } finally { setLoading(false); }
  };

  const handleGoogle = async () => {
    setError(""); setSuccess("");
    try {
      await signInWithGoogle();
      redirectAfterLogin();
    } catch (err) {
      const msg = mapError(err);
      if (msg !== "Sign-in cancelled") setError(msg);
    }
  };

  const handleForgot = async () => {
    setError(""); setSuccess("");
    if (!email.trim()) { setError("Enter your email above, then click Forgot password"); return; }
    try {
      await resetPassword(email);
      setSuccess("Password reset email sent — check your inbox");
    } catch (err) { setError(mapError(err)); }
  };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm">
        <div className="text-center mb-6">
          <div className="text-4xl mb-2">⚽</div>
          <h1 className="text-2xl font-bold">Welcome Back</h1>
          <p className="text-sm text-[var(--sc-text-secondary)]">Sign in to make your predictions</p>
        </div>

        {error && <div className="bg-red-50 text-red-600 text-sm rounded-lg p-3 mb-4">{error}</div>}
        {success && <div className="bg-green-50 text-green-600 text-sm rounded-lg p-3 mb-4">{success}</div>}

        <button onClick={handleGoogle}
          className="w-full py-2.5 rounded-lg border border-[var(--sc-border)] text-sm font-medium hover:bg-gray-50 mb-4 flex items-center justify-center gap-2">
          <svg width="18" height="18" viewBox="0 0 48 48"><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/><path fill="#FBBC05" d="M10.53 28.59a14.5 14.5 0 0 1 0-9.18l-7.98-6.19a24 24 0 0 0 0 21.56l7.98-6.19z"/><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/></svg>
          Continue with Google
        </button>

        <div className="flex items-center gap-3 my-4">
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
          <span className="text-xs text-[var(--sc-text-secondary)]">or sign in with email</span>
          <div className="flex-1 h-px bg-[var(--sc-border)]" />
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)}
            required autoComplete="email" autoFocus
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          <input type="password" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)}
            required autoComplete="current-password"
            className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]" />
          <button type="submit" disabled={loading}
            className="w-full py-2.5 rounded-lg bg-[var(--sc-tertiary)] text-white text-sm font-semibold disabled:opacity-50">
            {loading ? "Signing in..." : "Log In"}
          </button>
        </form>

        <div className="flex justify-end mt-2">
          <button onClick={handleForgot} className="text-sm text-[var(--sc-secondary)] hover:underline">Forgot password?</button>
        </div>

        <hr className="my-4 border-[var(--sc-border)]" />
        <div className="text-center">
          <p className="text-sm text-[var(--sc-text-secondary)] mb-2">Don&apos;t have an account?</p>
          <Link href="/register" className="block w-full py-2.5 rounded-lg border border-[var(--sc-secondary)] text-[var(--sc-secondary)] text-sm font-semibold text-center">
            Create Account
          </Link>
        </div>
      </div>
    </div>
  );
}
