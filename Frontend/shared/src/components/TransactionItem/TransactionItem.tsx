import React from "react";
import styles from "./TransactionItem.module.css";
import type { Transaction } from "../../types";
import { formatDisplayDate, formatSignedAmount, getInitials } from "../../utils/format";

export function TransactionItem({ tx }: { tx: Transaction }): React.JSX.Element {
  const income = tx.direction === "in";
  return (
    <li className={styles.row}>
      <span className={`${styles.icon} ${income ? styles.in : styles.out}`} aria-hidden="true">
        {income ? getInitials(tx.counterparty ?? "+") || "+" : getInitials(tx.counterparty ?? tx.title)}
      </span>
      <span className={styles.main}>
        <span className={styles.title}>{tx.title}</span>
        <span className={styles.meta}>
          {formatDisplayDate(tx.date)} · {tx.time} · {tx.type} · {tx.status}
        </span>
      </span>
      <span className={`${styles.amount} ${income ? styles.income : styles.expense}`}>
        {formatSignedAmount(tx.amount)}
      </span>
    </li>
  );
}
