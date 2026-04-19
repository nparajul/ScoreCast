"use client";

import { useAuth } from "@/contexts/auth-context";

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <div className="py-6">
      <h1 className="text-2xl font-bold mb-2">Dashboard</h1>
      <p className="text-[var(--sc-text-secondary)]">Welcome, {user?.displayName || user?.email}</p>
    </div>
  );
}
