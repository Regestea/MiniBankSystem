import { useState } from "react";
import { useNavigate } from "react-router-dom";
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
import type { Recipient } from "@minibank/shared/src/types";

/** Feature: recipients — list + add + remove, tap to start a fast transfer (live API). */
export function RecipientsScreen(): React.JSX.Element {
  const { recipients, loading, error, refresh } = useBank();
  const navigate = useNavigate();
  const [accountNumber, setAccountNumber] = useState("");
  const [nickname, setNickname] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const openTransfer = (r: Recipient): void => {
    navigate(`/transfer?to=${encodeURIComponent(r.accountNumber)}`);
  };

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
    <div className="mobile-screen">
      <PageHeader title="Recipients" subtitle="Tap one to send money fast." />
      <Card>
        <form onSubmit={(e) => void handleAdd(e)} noValidate>
          <AccountNumberInput value={accountNumber} onChange={setAccountNumber} error={formError} />
          <Input
            label="Nickname (optional)"
            placeholder="e.g. Mom"
            value={nickname}
            onChange={(e) => setNickname(e.target.value)}
          />
          <div style={{ marginTop: 12 }}>
            <Button type="submit" fullWidth loading={saving}>
              Save recipient
            </Button>
          </div>
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
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {recipients.map((r) => (
            <Card key={r.id}>
              <RecipientCard recipient={r} onSelect={openTransfer} />
              <div style={{ marginTop: 8 }}>
                <Button variant="ghost" loading={removingId === r.id} onClick={() => void handleRemove(r.id)}>
                  Remove
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
