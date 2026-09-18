"use client";

import type React from "react";
import { AuthProvider } from "@minibank/shared/src/store/AuthContext";

/** Root client providers — auth state shared by every page. Bank data provider lives in BankShell (needs auth first). */
export function Providers({ children }: { children: React.ReactNode }): React.JSX.Element {
  return <AuthProvider>{children}</AuthProvider>;
}
