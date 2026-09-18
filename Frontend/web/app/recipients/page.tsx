"use client";

import Link from "next/link";
import { useState } from "react";
import {
  AccountNumberInput,
  Button,
  Card,
  EmptyState,
  Input,
  LoadingState,
  PageHeader,
  RecipientCard,
  getApiBankService,
  useBank,
} from "@minibank/shared/src/index";
import { getAccountNumberError, normalizeAccountNumber } from "@minibank/shared/src/utils/validation";
import styles from "./recipients.module.css";

export default function RecipientsPage(): React.JSX.Element {
  const { recipients, loading, error, refresh } = useBank();
  const [accountNumber, setAccountNumber] = useState("");
  const [nickname, setNickname] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [removingId, setRemovingId] = useState<string | null>(null);

  if (loading) return <LoadingState message="Loading recipients…" />;

  const handleAdd = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    const numberError = getAccountNumberError(accountNumber);
    if (numberError) {
      setFormError(numberError);
      return;
    }
    setFormError(null);
    setSaving(true);
    try {
      const preview = await getApiBankService().previewRecipient(normalizeAccountNumber(accountNumber));
      if (!preview) {
        setFormError("We couldn't find an account with this number.");
        return;
      }
      await getApiBankService().saveRecipient(preview.accountNumber, nickname.trim() || preview.fullName);
      setAccountNumber("");
      setNickname("");
      await refresh();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Could not save the recipient.");
    } finally {
      setSaving(false);
    }
  };

  const handleRemove = async (id: string): Promise<void> => {
    setRemovingId(id);
    try {
      await getApiBankService().removeRecipient(id);
      await refresh();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Could not remove the recipient.");
    } finally {
      setRemovingId(null);
    }
  };

  return (
    <>
      <PageHeader title="Recipients" subtitle="Saved destinations — pick one to start a fast transfer." />
      <Card>
        <form onSubmit={(e) => void handleAdd(e)} noValidate>
          <div className={styles.formGrid}>
            <AccountNumberInput value={accountNumber} onChange={setAccountNumber} error={formError} />
            <Input
              label="Nickname (optional)"
              placeholder="e.g. Mom"
              value={nickname}
              onChange={(e) => setNickname(e.target.value)}
              hint="Leave empty to use the account holder name."
            />
          </div>
          <Button type="submit" loading={saving}>
            Save recipient
          </Button>
        </form>
      </Card>
      {error ? (
        <p role="alert">{error}</p>
      ) : recipients.length === 0 ? (
        <EmptyState
          title="No saved recipients"
          description="Save a destination above, or tick “Save this recipient” during a transfer."
        />
      ) : (
        <div className={styles.grid}>
          {recipients.map((r) => (
            <div key={r.id} className={styles.cardWrap}>
              <Link href={`/transfer?to=${encodeURIComponent(r.accountNumber)}`} className={styles.link}>
                <RecipientCard recipient={r} />
              </Link>
              <Button
                variant="ghost"
                loading={removingId === r.id}
                onClick={() => void handleRemove(r.id)}
              >
                Remove
              </Button>
            </div>
          ))}
        </div>
      )}
    </>
  );
}
