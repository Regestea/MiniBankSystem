import type { TransferFlow } from "@minibank/shared/src/hooks/useTransferFlow";
import { Avatar, Button, Checkbox, formatCurrency } from "@minibank/shared/src/index";

/** Feature-local: step 2 — resolved recipient + save checkbox. */
export function RecipientStep({ flow }: { flow: TransferFlow }): React.JSX.Element {
  if (!flow.recipient) return <p role="alert">Recipient not resolved.</p>;
  return (
    <div className="transfer-stack">
      <div className="transfer-hero">
        <Avatar name={flow.recipient.fullName} size={56} />
        <div>
          <p className="transfer-eyebrow">Recipient</p>
          <p className="transfer-big">{flow.recipient.fullName}</p>
          <p className="transfer-mono">{flow.recipient.accountNumber}</p>
        </div>
      </div>
      <p className="transfer-amount">
        Amount <strong>{formatCurrency(flow.amount ?? 0)}</strong>
      </p>
      <Checkbox
        label="Save this recipient for future transfers"
        checked={flow.saveRecipient}
        onChange={flow.setSaveRecipient}
      />
      <div className="transfer-btns">
        <Button variant="ghost" onClick={flow.backToDetails}>
          Back
        </Button>
        <Button onClick={flow.goToConfirm}>Continue</Button>
      </div>
    </div>
  );
}
