import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { authApi } from "../api/authApi";
import { tokenStorage } from "../api/tokenStorage";
import type { UserProfile } from "../types";

interface AuthState {
  user: UserProfile | null;
  isAuthenticated: boolean;
  loading: boolean;
  initializing: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (input: { email: string; password: string; fullName: string; phoneNumber: string }) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

async function loadUserProfile(): Promise<UserProfile | null> {
  try {
    const overview = await authApi.getOverview();
    const parts = overview.fullName.trim().split(/\s+/);
    const primary = overview.accounts[0];
    return {
      fullName: overview.fullName,
      firstName: parts[0] ?? overview.fullName,
      lastName: parts.slice(1).join(" ") || "",
      accountNumber: primary?.accountNumber ?? "",
      phoneNumber: overview.phoneNumber,
      email: overview.email,
    };
  } catch {
    return null;
  }
}

/**
 * Single auth provider for BOTH apps.
 * Persist tokens via tokenStorage (localStorage), hydrate user via GET /customers/overview.
 */
export function AuthProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [initializing, setInitializing] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      if (!tokenStorage.hasTokens()) {
        if (!cancelled) setInitializing(false);
        return;
      }
      const profile = await loadUserProfile();
      if (!cancelled) {
        setUser(profile);
        if (!profile) tokenStorage.clear();
        setInitializing(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    setLoading(true);
    setError(null);
    try {
      await authApi.login({ email: email.trim(), password });
      const profile = await loadUserProfile();
      setUser(profile);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Sign in failed.");
      throw e;
    } finally {
      setLoading(false);
    }
  }, []);

  const register = useCallback(async (input: { email: string; password: string; fullName: string; phoneNumber: string }) => {
    setLoading(true);
    setError(null);
    try {
      await authApi.registerAndLogin({
        email: input.email.trim().toLowerCase(),
        password: input.password,
        fullName: input.fullName.trim(),
        phoneNumber: input.phoneNumber.trim(),
      });
      const profile = await loadUserProfile();
      setUser(profile);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Registration failed.");
      throw e;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    authApi.logout();
    setUser(null);
    setError(null);
  }, []);

  const refreshUser = useCallback(async () => {
    const profile = await loadUserProfile();
    setUser(profile);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isAuthenticated: Boolean(user) || tokenStorage.hasTokens(),
      loading,
      initializing,
      error,
      login,
      register,
      logout,
      refreshUser,
    }),
    [user, loading, initializing, error, login, register, logout, refreshUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>.");
  return ctx;
}
