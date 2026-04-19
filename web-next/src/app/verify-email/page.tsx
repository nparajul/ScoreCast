"use client";

import { useAuth } from "@/contexts/auth-context";
import { sendEmailVerification } from "firebase/auth";
import { useRouter } from "next/navigation";
import { useEffect, useState, useCallback } from "react";

export default function VerifyEmailPage() {
  const { user, signOut } = useAuth();
  const router = useRouter();
  const [msg, setMsg] = useState("");
  const [cooldown, setCooldown] = useState(0);

  const checkVerified = useCallback(async () => {
    if (!user) return;
    await user.reload();
    if (user.emailVerified) router.replace("/dashboard");
  }, [user, router]);

  useEffect(() => {
    if (!user) { router.replace("/login"); return; }
    checkVerified();
  }, [user, router, checkVerified]);

  useEffect(() => {
    if (cooldown <= 0) return;
    const t = setTimeout(() => setCooldown(c => c - 1), 1000);
    return () => clearTimeout(t);
  }, [cooldown]);

  const resend = async () => {
    if (!user) return;
    try {
      await sendEmailVerification(user);
      setMsg("Verification email sent!");
      setCooldown(60);
    } catch { setMsg("Failed to send email. Try again later."); }
  };

  const handleSignOut = async () => {
    await signOut();
    router.replace("/");
  };

  return (
    <div className="max-w-sm mx-auto mt-16 text-white">
      <div className="bg-white/5 rounded-xl p-6 text-center">
        <div className="text-5xl mb-4">📧</div>
        <h1 className="text-xl font-bold mb-2">Check your email</h1>
        <p className="text-sm opacity-60 mb-4">We sent a verification link to your email address. Click the link to activate your account.</p>

        <div className="bg-blue-500/10 border border-blue-500/30 rounded-lg p-3 text-sm text-left mb-4">
          <p className="font-semibold">Can&apos;t find the email?</p>
          <p className="text-xs opacity-70">Check your Spam or Junk folder — the email comes from verify@scorecast.uk</p>
        </div>

        {msg && <p className="text-sm text-[#FF6B35] mb-3">{msg}</p>}

        <button onClick={checkVerified} className="w-full bg-[#0A1929] border border-[#FF6B35] rounded-lg py-2 font-bold mb-2">
          I&apos;ve verified — continue
        </button>
        <button onClick={resend} disabled={cooldown > 0}
          className="w-full border border-white/20 rounded-lg py-2 font-bold mb-4 disabled:opacity-40">
          {cooldown > 0 ? `Resend in ${cooldown}s` : "Resend verification email"}
        </button>

        <hr className="border-white/10 mb-4" />
        <button onClick={handleSignOut} className="text-sm opacity-60 hover:opacity-100">Sign out</button>
      </div>
    </div>
  );
}
