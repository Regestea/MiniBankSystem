import type { TransferFlow } from "@minibank/shared/src/hooks/useTransferFlow";
import { Button, TRANSFER_FEE, formatCurrency } from "@minibank/shared/src/index";

/** Feature-local: step 3 — explicit confirmation. */
export function TransferConfirmation({ flow }: { flow: TransferFlow }): React.JSX.Element {
  if (!flow.recipient) return <p role="alert">Recipient not resolved.</p>;
  return (
    <div className="transfer-stack">
      <h2 className="transfer-h2">Confirm Transfer</h2>
      <dl className="transfer-rows">
        <div>
          <dt>Recipient</dt>
          <dd>{flow.recipient.fullName}</dd>
        </div>
        <div>
          <dt>Account</dt>
          <dd className="transfer-mono">{flow.recipient.accountNumber}</dd>
        </div>
        <div>
          <dt>Amount</dt>
          <dd>{formatCurrency(flow.amount ?? 0)}</dd>
        </div>
        <div>
          <dt>Fee</dt>
          <dd>{formatCurrency(TRANSFER_FEE)}</dd>
        </div>
        <div>
          <dt>Total</dt>
          <dd>{formatCurrency(flow.amount ?? 0)}</dd>
        </div>
      </dl>
      {flow.error ? (
        <p className="transfer-error" role="alert">
          {flow.error}
        </p>
      ) : null}
      <div className="transfer-btns">
        <Button variant="ghost" onClick={flow.backToRecipient}>
          Back
        </Button>
        <Button loading={flow.processing} onClick={() => void flow.confirmTransfer()}>
          Transfer
        </Button>
      </div>
    </div>
  );
}
