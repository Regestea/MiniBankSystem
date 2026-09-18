"use client";

import { usePathname } from "next/navigation";
import type React from "react";
import { useMemo } from "react";
import { getApiBankService } from "@minibank/shared/src/api/apiBankService";
import { BankProvider } from "@minibank/shared/src/store/BankContext";
import { RequireAuth } from "./RequireAuth";
import { Sidebar } from "./Sidebar";
import styles from "../app/layout.module.css";

const AUTH_ROUTES = ["/login", "/register"];

/** App shell: auth pages render standalone; everything else gets sidebar + live API data. */
export function BankShell({ children }: { children: React.ReactNode }): React.JSX.Element {
  const pathname = usePathname();
  const service = useMemo(() => getApiBankService(), []);

  const isAuthRoute = AUTH_ROUTES.some((r) => pathname === r || pathname.startsWith(`${r}/`));
  if (isAuthRoute) {
    return <div className={styles.authWrap}>{children}</div>;
  }

  return (
    <RequireAuth>
      <BankProvider service={service}>
        <div className={styles.shell}>
          <Sidebar />
          <div className={styles.main}>
            <div className={styles.content}>{children}</div>
          </div>
        </div>
      </BankProvider>
    </RequireAuth>
  );
}
