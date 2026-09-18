"use client";

import type React from "react";
import { BankProvider } from "@minibank/shared/src/store/BankContext";
import { Sidebar } from "./Sidebar";
import styles from "../app/layout.module.css";

/** Client shell: mock-state provider + sidebar + content area. */
export function BankShell({ children }: { children: React.ReactNode }): React.JSX.Element {
  return (
    <BankProvider>
      <div className={styles.shell}>
        <Sidebar />
        <div className={styles.main}>
          <div className={styles.content}>{children}</div>
        </div>
      </div>
    </BankProvider>
  );
}
