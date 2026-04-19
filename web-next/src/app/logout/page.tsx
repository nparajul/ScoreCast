"use client";

import { useAuth } from "@/contexts/auth-context";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function LogoutPage() {
  const { signOut } = useAuth();
  const router = useRouter();

  useEffect(() => {
    signOut().then(() => router.replace("/login"));
  }, [signOut, router]);

  return (
    <div className="flex flex-col items-center justify-center min-h-[80vh] text-white">
      <div className="animate-spin w-8 h-8 border-2 border-white/30 border-t-white rounded-full mb-4" />
      <p>Signing out...</p>
    </div>
  );
}
