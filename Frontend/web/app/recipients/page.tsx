"use client";

import Link from "next/link";
import { EmptyState, LoadingState, PageHeader, RecipientCard, useBank } from "@minibank/shared/src/index";
import styles from "./recipients.module.css";

export default function RecipientsPage(): React.JSX.Element {
  const { recipients, loading, error } = useBank();

  if (loading) return <LoadingState message="Loading recipients…" />;

  return (
    <>
      <PageHeader title="Recipients" subtitle="Saved destinations — pick one to start a fast transfer." />
      {error ? (
        <p role="alert">{error}</p>
      ) : recipients.length === 0 ? (
        <EmptyState
          title="No saved recipients"
          description="Tick “Save this recipient” during a transfer and they will appear here."
        />
      ) : (
        <div className={styles.grid}>
          {recipients.map((r) => (
            <Link key={r.id} href={`/transfer?to=${encodeURIComponent(r.accountNumber)}`} className={styles.link}>
              <RecipientCard recipient={r} />
            </Link>
          ))}
        </div>
      )}
    </>
  );
}
