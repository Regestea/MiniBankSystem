"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import type { CSSProperties } from "react";
import {
  Avatar,
  Button,
  Card,
  Input,
  LoadingState,
  PageHeader,
  getApiBankService,
  imageUrls,
  useAuth,
  useBank,
} from "@minibank/shared/src/index";
import { getFullNameError, getPhoneError, normalizePhone } from "@minibank/shared/src/utils/authValidation";
import styles from "./profile.module.css";

export default function ProfilePage(): React.JSX.Element {
  const { profile, loading, error, refresh } = useBank();
  const { logout } = useAuth();
  const router = useRouter();
  const [editing, setEditing] = useState(false);
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  if (loading) return <LoadingState message="Loading profile…" />;
  if (error || !profile) {
    return (
      <Card>
        <p role="alert">{error ?? "Could not load profile."}</p>
      </Card>
    );
  }

  const startEdit = (): void => {
    setFullName(profile.fullName);
    setPhoneNumber(profile.phoneNumber);
    setFieldErrors({});
    setSubmitError(null);
    setEditing(true);
  };

  const handleSave = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    const errors: Record<string, string> = {};
    const nameError = getFullNameError(fullName);
    if (nameError) errors.fullName = nameError;
    const phoneError = getPhoneError(phoneNumber);
    if (phoneError) errors.phoneNumber = phoneError;
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSaving(true);
    setSubmitError(null);
    try {
      await getApiBankService().updateProfile(fullName.trim(), normalizePhone(phoneNumber));
      await refresh();
      setEditing(false);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Could not update your profile.");
    } finally {
      setSaving(false);
    }
  };

  const handleLogout = (): void => {
    logout();
    router.replace("/login");
  };

  const rows = [
    { label: "Full Name", value: profile.fullName },
    { label: "Account Number", value: profile.accountNumber, mono: true },
    { label: "Phone Number", value: profile.phoneNumber },
    { label: "Email", value: profile.email },
  ];

  return (
    <>
      <PageHeader title="My Profile" subtitle="Personal banking information." />
      <Card>
        <div
          className={`${styles.hero} ocean-placeholder`}
          style={{ "--ocean-image": `url("${imageUrls.profileBackground}")` } as CSSProperties}
        >
          <Avatar name={profile.fullName} size={64} />
          <div className={styles.heroText}>
            <p className={styles.name}>{profile.fullName}</p>
            <p className={styles.account}>{profile.accountNumber}</p>
          </div>
        </div>
        {!editing ? (
          <>
            <dl className={styles.rows}>
              {rows.map((r) => (
                <div key={r.label}>
                  <dt>{r.label}</dt>
                  <dd className={r.mono ? styles.mono : ""}>{r.value}</dd>
                </div>
              ))}
            </dl>
            <div className={styles.actions}>
              <Button variant="secondary" onClick={startEdit}>
                Edit profile
              </Button>
              <Button variant="ghost" onClick={handleLogout}>
                Sign out
              </Button>
            </div>
          </>
        ) : (
          <form onSubmit={(e) => void handleSave(e)} noValidate>
            <div className={styles.formGrid}>
              <Input
                label="Full name"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                error={fieldErrors.fullName}
              />
              <Input
                label="Phone number"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                error={fieldErrors.phoneNumber}
              />
            </div>
            {submitError ? (
              <p role="alert" className={styles.error}>
                {submitError}
              </p>
            ) : null}
            <div className={styles.actions}>
              <Button type="submit" loading={saving}>
                Save changes
              </Button>
              <Button variant="ghost" onClick={() => setEditing(false)}>
                Cancel
              </Button>
            </div>
          </form>
        )}
      </Card>
    </>
  );
}
