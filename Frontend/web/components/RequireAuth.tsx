"use client";

import { useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { LoadingState } from "@minibank/shared/src/components/LoadingState/LoadingState";
import { useAuth } from "@minibank/shared/src/store/AuthContext";

/** Guards protected pages — unauthenticated visitors go to /login. */
export function RequireAuth({ children }: { children: ReactNode }): React.JSX.Element {
  const { isAuthenticated, initializing } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!initializing && !isAuthenticated) router.replace("/login");
  }, [initializing, isAuthenticated, router]);

  if (initializing) return <LoadingState message="Checking your session…" />;
  if (!isAuthenticated) return <LoadingState message="Redirecting to sign in…" />;
  return <>{children}</>;
}
