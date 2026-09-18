"use client";

import { Avatar, Card, LoadingState, PageHeader, useBank } from "@minibank/shared/src/index";
import styles from "./profile.module.css";

export default function ProfilePage(): React.JSX.Element {
  const { profile, loading, error } = useBank();

  if (loading) return <LoadingState message="Loading profile…" />;
  if (error || !profile) {
    return (
      <Card>
        <p role="alert">{error ?? "Could not load profile."}</p>
      </Card>
    );
  }

  const rows = [
    { label: "Full Name", value: profile.fullName },
    { label: "Account Number", value: profile.accountNumber, mono: true },
    { label: "Phone Number", value: profile.phoneNumber },
    { label: "Email", value: profile.email },
  ];

  return (
    <>
      <PageHeader title="My Profile" subtitle="Read-only personal banking information." />
      <Card>
        <div className={`${styles.hero} ocean-placeholder`}>
          <Avatar name={profile.fullName} size={64} />
          <div className={styles.heroText}>
            <p className={styles.name}>{profile.fullName}</p>
            <p className={styles.account}>{profile.accountNumber}</p>
          </div>
        </div>
        <dl className={styles.rows}>
          {rows.map((r) => (
            <div key={r.label}>
              <dt>{r.label}</dt>
              <dd className={r.mono ? styles.mono : ""}>{r.value}</dd>
            </div>
          ))}
        </dl>
      </Card>
    </>
  );
}
