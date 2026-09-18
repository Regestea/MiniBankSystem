import { useNavigate } from "react-router-dom";
import { EmptyState, LoadingState, PageHeader, RecipientCard, useBank } from "@minibank/shared/src/index";
import type { Recipient } from "@minibank/shared/src/types";

/** Feature: recipients — saved destinations, tap to start a fast transfer. */
export function RecipientsScreen(): React.JSX.Element {
  const { recipients, loading, error } = useBank();
  const navigate = useNavigate();

  const openTransfer = (r: Recipient): void => {
    navigate(`/transfer?to=${encodeURIComponent(r.accountNumber)}`);
  };

  if (loading) return <LoadingState message="Loading recipients…" />;

  return (
    <div className="mobile-screen">
      <PageHeader title="Recipients" subtitle="Tap one to send money fast." />
      {error ? (
        <p role="alert">{error}</p>
      ) : recipients.length === 0 ? (
        <EmptyState
          title="No saved recipients"
          description="Tick “Save this recipient” during a transfer and they will appear here."
        />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {recipients.map((r) => (
            <RecipientCard key={r.id} recipient={r} onSelect={openTransfer} />
          ))}
        </div>
      )}
    </div>
  );
}
