"use client";

import Link from "next/link";
import { useState } from "react";
import type { CSSProperties } from "react";
import {
  BalanceCard,
  Button,
  Card,
  LoadingState,
  SectionHeader,
  TransactionList,
  getApiBankService,
  getGreeting,
  imageUrls,
  useBank,
} from "@minibank/shared/src/index";
import styles from "./page.module.css";

const QUICK_ACTIONS = [
  { href: "/topup", label: "Top Up", icon: "+", desc: "Instant" },
  { href: "/transfer", label: "Transfer", icon: "➤", desc: "By account no." },
  { href: "/transactions", label: "Activity", icon: "⇄", desc: "History" },
] as const;

export default function DashboardPage(): React.JSX.Element {
  const { profile, account, transactions, loading, error, refresh } = useBank();
  const [opening, setOpening] = useState(false);
  const [openError, setOpenError] = useState<string | null>(null);

  if (loading) return <LoadingState message="Loading your banking overview…" />;

  const needsAccount =
    !account || (error?.toLowerCase().includes("no bank account") ?? false);

  if (needsAccount && profile) {
    const handleOpen = async (): Promise<void> => {
      setOpening(true);
      setOpenError(null);
      try {
        await getApiBankService().openAccount("Current");
        await refresh();
      } catch (e) {
        setOpenError(e instanceof Error ? e.message : "Could not open an account.");
      } finally {
        setOpening(false);
      }
    };
    return (
      <Card>
        <SectionHeader title="Welcome to Mini Bank" />
        <p>
          Hi <strong>{profile.fullName}</strong> — your profile is ready but you don&apos;t have a bank
          account yet. Open your first account to start.
        </p>
        {openError ? (
          <p role="alert">{openError}</p>
        ) : null}
        <Button loading={opening} onClick={() => void handleOpen()}>
          Open my first account
        </Button>
      </Card>
    );
  }

  if (error || !profile || !account) {
    return (
      <Card>
        <p role="alert">{error ?? "Could not load banking data."}</p>
        <Button variant="secondary" onClick={() => void refresh()}>
          Retry
        </Button>
      </Card>
    );
  }

  return (
    <>
      <header className={styles.greeting}>
        <p className={styles.hello}>
          {getGreeting()},<br />
          <strong>{profile.fullName}</strong>
        </p>
        <p className={styles.sub}>Here is your calm water-banking overview.</p>
      </header>

      <div className={styles.grid}>
        <div className={styles.main}>
          <BalanceCard balance={account.balance} accountNumber={account.accountNumber} />

          <section aria-label="Quick actions">
            <SectionHeader title="Quick Actions" />
            <div className={styles.actions}>
              {QUICK_ACTIONS.map((a) => (
                <Link key={a.href} href={a.href} className={styles.action}>
                  <span className={styles.actionIcon} aria-hidden="true">
                    {a.icon}
                  </span>
                  <span className={styles.actionLabel}>{a.label}</span>
                  <span className={styles.actionDesc}>{a.desc}</span>
                </Link>
              ))}
            </div>
          </section>

          <Card>
            <SectionHeader
              title="Recent Transactions"
              action={<Link href="/transactions">See All</Link>}
            />
            <TransactionList transactions={transactions.slice(0, 5)} />
          </Card>
        </div>

        <div className={styles.side}>
          <Card>
            <SectionHeader title="My Account" />
            <dl className={styles.facts}>
              <div>
                <dt>Account</dt>
                <dd>{account.accountNumber}</dd>
              </div>
              <div>
                <dt>Currency</dt>
                <dd>USD</dd>
              </div>
              <div>
                <dt>Phone</dt>
                <dd>{profile.phoneNumber}</dd>
              </div>
            </dl>
            <Link href="/transfer" className={styles.cta}>
              Send money →
            </Link>
          </Card>

          <div
            className={`${styles.oceanNote} ocean-placeholder`}
            style={{ "--ocean-image": `url("${imageUrls.oceanCard}")` } as CSSProperties}
          >
            <p className={styles.oceanTitle}>Calm, premium water banking</p>
            <p className={styles.oceanSub}>
              Live data — your balance and history update after every transfer and top-up.
            </p>
          </div>
        </div>
      </div>
    </>
  );
}
