"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import Link from "next/link";

export default function ForgotPasswordPage() {
  const { resetPassword } = useAuth();
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      await resetPassword(email);
      setSent(true);
    } catch {
      setError("Failed to send reset email. Check your email address.");
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm">
        <h1 className="text-2xl font-bold text-center mb-1">Reset password</h1>
        <p className="text-sm text-[var(--sc-text-secondary)] text-center mb-6">
          {sent ? "Check your email for a reset link" : "Enter your email to receive a reset link"}
        </p>

        {error && <div className="bg-red-50 text-red-600 text-sm rounded-lg p-3 mb-4">{error}</div>}
        {sent && <div className="bg-green-50 text-green-700 text-sm rounded-lg p-3 mb-4">Reset email sent to {email}</div>}

        {!sent && (
          <form onSubmit={handleSubmit} className="space-y-4">
            <input
              type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)}
              required autoComplete="email"
              className="w-full px-3 py-2.5 rounded-lg border border-[var(--sc-border)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--sc-secondary)]"
            />
            <button type="submit" className="w-full py-2.5 rounded-lg bg-[var(--sc-primary)] text-white text-sm font-semibold">
              Send Reset Link
            </button>
          </form>
        )}

        <p className="mt-4 text-center text-sm">
          <Link href="/login" className="text-[var(--sc-secondary)] hover:underline">Back to login</Link>
        </p>
      </div>
    </div>
  );
}
