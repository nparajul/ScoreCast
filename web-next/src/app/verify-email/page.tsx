"use client";

import { useAuth } from "@/contexts/auth-context";
import { sendEmailVerification } from "firebase/auth";
import { useRouter } from "next/navigation";
import { useEffect, useState, useCallback, useRef } from "react";

export default function VerifyEmailPage() {
  const { user, signOut } = useAuth();
  const router = useRouter();
  const [msg, setMsg] = useState("");
  const [severity, setSeverity] = useState<"success" | "warning" | "error">("success");
  const [cooldown, setCooldown] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const checkVerified = useCallback(async () => {
    if (!user) return;
    await user.reload();
    if (user.emailVerified) router.replace("/dashboard");
  }, [user, router]);

  // Auto-check every 3s
  useEffect(() => {
    if (!user) { router.replace("/login"); return; }
    checkVerified();
    const id = setInterval(checkVerified, 3000);
    return () => clearInterval(id);
  }, [user, router, checkVerified]);

  // Cooldown timer
  useEffect(() => {
    if (cooldown <= 0) { if (intervalRef.current) clearInterval(intervalRef.current); return; }
    intervalRef.current = setInterval(() => setCooldown((c) => c - 1), 1000);
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, [cooldown > 0]); // eslint-disable-line react-hooks/exhaustive-deps

  const resend = async () => {
    if (!user) return;
    try {
      await sendEmailVerification(user);
      setMsg("Verification email sent!"); setSeverity("success");
      setCooldown(60);
    } catch { setMsg("Failed to send email. Try again later."); setSeverity("error"); }
  };

  const handleSignOut = async () => { await signOut(); router.replace("/"); };

  const severityClass = { success: "bg-green-50 text-green-700", warning: "bg-amber-50 text-amber-700", error: "bg-red-50 text-red-600" };

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-sm bg-[var(--sc-surface)] rounded-2xl p-6 shadow-sm text-center">
        <div className="text-5xl mb-4">📧</div>
        <h1 className="text-xl font-bold mb-2">Check your email</h1>
        <p className="text-sm text-[var(--sc-text-secondary)] mb-4">
          We sent a verification link to your email address.<br />Click the link to activate your account.
        </p>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 text-left text-sm mb-4">
          <p className="font-semibold text-blue-800">Can&apos;t find the email?</p>
          <p className="text-xs text-blue-700">Check your <strong>Spam</strong> or <strong>Junk</strong> folder — the email comes from <strong>verify@scorecast.uk</strong></p>
        </div>

        {msg && <div className={`text-sm rounded-lg p-3 mb-4 ${severityClass[severity]}`}>{msg}</div>}

        <button onClick={checkVerified}
          className="w-full py-2.5 rounded-lg bg-[var(--sc-primary)] text-white font-semibold text-sm mb-2">
          I&apos;ve verified — continue
        </button>
        <button onClick={resend} disabled={cooldown > 0}
          className="w-full py-2.5 rounded-lg border border-[var(--sc-border)] font-semibold text-sm mb-4 disabled:opacity-40">
          {cooldown > 0 ? `Resend in ${cooldown}s` : "Resend verification email"}
        </button>

        <hr className="border-[var(--sc-border)] mb-4" />
        <button onClick={handleSignOut} className="text-sm text-[var(--sc-text-secondary)] hover:underline">Sign out</button>
      </div>
    </div>
  );
}
