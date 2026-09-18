"use client";

import { useMemo, useState } from "react";
import { Card, Input, LoadingState, PageHeader, TransactionList, useBank } from "@minibank/shared/src/index";
import type { TransactionType } from "@minibank/shared/src/index";

const FILTERS: { value: "all" | TransactionType; label: string }[] = [
  { value: "all", label: "All" },
  { value: "transfer", label: "Transfers" },
  { value: "topup", label: "Top-ups" },
  { value: "deposit", label: "Deposits" },
  { value: "withdraw", label: "Withdrawals" },
];

export default function TransactionsPage(): React.JSX.Element {
  const { transactions, loading, error } = useBank();
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<(typeof FILTERS)[number]["value"]>("all");

  const visible = useMemo(() => {
    const q = query.trim().toLowerCase();
    return transactions.filter((t) => {
      if (filter !== "all" && t.type !== filter) return false;
      if (!q) return true;
      return (
        t.title.toLowerCase().includes(q) ||
        (t.counterparty ?? "").toLowerCase().includes(q) ||
        (t.accountNumber ?? "").toLowerCase().includes(q)
      );
    });
  }, [transactions, query, filter]);

  if (loading) return <LoadingState message="Loading transactions…" />;

  return (
    <>
      <PageHeader title="Transactions" subtitle="Every top-up and transfer, newest first." />
      <Card>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
          {FILTERS.map((f) => (
            <button
              key={f.value}
              type="button"
              onClick={() => setFilter(f.value)}
              aria-pressed={filter === f.value}
              style={{
                padding: "6px 12px",
                borderRadius: 999,
                border: "1px solid var(--color-border-soft)",
                background: filter === f.value ? "var(--color-primary)" : "transparent",
                color: filter === f.value ? "#fff" : "inherit",
                cursor: "pointer",
                fontSize: 13,
              }}
            >
              {f.label}
            </button>
          ))}
        </div>
        <Input
          label="Search transactions"
          placeholder="Name, account number, title…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </Card>
      <Card>
        {error ? (
          <p role="alert">{error}</p>
        ) : (
          <TransactionList transactions={visible} />
        )}
      </Card>
    </>
  );
}
