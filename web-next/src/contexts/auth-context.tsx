"use client";

import { createContext, useContext, useEffect, useState, ReactNode, useRef, useCallback } from "react";
import {
  User,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  signOut as firebaseSignOut,
  GoogleAuthProvider,
  signInWithPopup,
  updateProfile,
  sendPasswordResetEmail,
  sendEmailVerification,
} from "firebase/auth";
import { auth } from "@/lib/firebase";
import { api } from "@/lib/api";
import type { UserProfileResult } from "@/lib/types";

interface AuthState {
  user: User | null;
  loading: boolean;
  synced: boolean;
  needsOnboarding: boolean;
  profile: UserProfileResult | null;
  completeOnboarding: () => void;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, displayName: string) => Promise<void>;
  signInWithGoogle: () => Promise<void>;
  signOut: () => Promise<void>;
  resetPassword: (email: string) => Promise<void>;
  getToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthState | null>(null);

async function syncUserToBackend(user: User) {
  const token = await user.getIdToken();
  const isGoogle = user.providerData.some((p) => p.providerId === "google.com");
  const body = {
    email: user.email ?? "",
    displayName: isGoogle ? null : user.displayName,
    isGoogleSignIn: isGoogle,
    firebaseUid: user.uid,
    appName: "SIGN UP",
  };
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  const profileRes = await fetch(`${base}/api/v1/users/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (profileRes.ok) {
    const text = await profileRes.text();
    if (text) {
      try {
        const data = JSON.parse(text);
        if (data?.success && data?.data) return data.data as UserProfileResult;
      } catch {}
    }
  }
  await fetch(`${base}/api/v1/users/sync`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  // Fetch profile after sync
  const p = await api.getMyProfile();
  return p.success && p.data ? p.data : null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [synced, setSynced] = useState(false);
  const [needsOnboarding, setNeedsOnboarding] = useState(false);
  const [profile, setProfile] = useState<UserProfileResult | null>(null);
  const syncedUidRef = useRef<string | null>(null);

  const completeOnboarding = useCallback(() => setNeedsOnboarding(false), []);

  useEffect(() => {
    return onAuthStateChanged(auth, async (u) => {
      setUser(u);
      setLoading(false);
      if (u && syncedUidRef.current !== u.uid) {
        syncedUidRef.current = u.uid;
        try {
          const p = await syncUserToBackend(u);
          if (p) {
            setProfile(p);
            setNeedsOnboarding(!p.hasCompletedOnboarding);
          }
        } catch (e) {
          console.error("User sync failed:", e);
        }
        setSynced(true);
      } else if (!u) {
        syncedUidRef.current = null;
        setSynced(false);
        setProfile(null);
        setNeedsOnboarding(false);
      } else if (u) {
        setSynced(true);
      }
    });
  }, []);

  const signIn = async (email: string, password: string) => {
    await signInWithEmailAndPassword(auth, email, password);
  };

  const signUp = async (email: string, password: string, displayName: string) => {
    const result = await createUserWithEmailAndPassword(auth, email, password);
    await updateProfile(result.user, { displayName });
    await sendEmailVerification(result.user);
  };

  const signInWithGoogle = async () => {
    await signInWithPopup(auth, new GoogleAuthProvider());
  };

  const signOut = async () => {
    await firebaseSignOut(auth);
  };

  const resetPassword = async (email: string) => {
    await sendPasswordResetEmail(auth, email);
  };

  const getToken = async () => (user ? user.getIdToken() : null);

  return (
    <AuthContext.Provider value={{ user, loading, synced, needsOnboarding, profile, completeOnboarding, signIn, signUp, signInWithGoogle, signOut, resetPassword, getToken }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
