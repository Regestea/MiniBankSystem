import React from "react";
import type { CSSProperties } from "react";
import styles from "./BalanceCard.module.css";
import { imageUrls } from "../../constants/imageUrls";
import { formatCurrency } from "../../utils/format";

/** Prominent balance display. Data-driven: pass `balance` from BankContext. */
export function BalanceCard({
  balance,
  accountNumber,
  compact = false,
}: {
  balance: number;
  accountNumber?: string;
  compact?: boolean;
}): React.JSX.Element {
  // Centralized scenic layer; gradient fallback remains if the URL fails.
  const scenic = { "--ocean-image": `url("${imageUrls.dashboardBackground}")` } as CSSProperties;
  return (
    <section
      className={`${styles.card} ocean-placeholder ${compact ? styles.compact : ""}`}
      style={scenic}
      aria-label="Account balance"
    >
      <div className={styles.inner}>
        <p className={styles.eyebrow}>Total Balance</p>
        <p className={styles.balance}>{formatCurrency(balance)}</p>
        {accountNumber ? <p className={styles.account}>{accountNumber} · USD</p> : null}
      </div>
      <div className={styles.wave} aria-hidden="true" />
    </section>
  );
}
