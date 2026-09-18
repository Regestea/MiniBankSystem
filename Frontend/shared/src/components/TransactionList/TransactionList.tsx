import React from "react";
import styles from "./TransactionList.module.css";
import type { Transaction } from "../../types";
import { TransactionItem } from "../TransactionItem/TransactionItem";
import { EmptyState } from "../EmptyState/EmptyState";

/** Data-driven list: `transactions.map(...)` — never duplicate markup per row. */
export function TransactionList({ transactions }: { transactions: Transaction[] }): React.JSX.Element {
  if (transactions.length === 0) {
    return (
      <EmptyState
        title="No transactions yet"
        description="Your top-ups and transfers will appear here."
      />
    );
  }
  return (
    <ul className={styles.list}>
      {transactions.map((tx) => (
        <TransactionItem key={tx.id} tx={tx} />
      ))}
    </ul>
  );
}
