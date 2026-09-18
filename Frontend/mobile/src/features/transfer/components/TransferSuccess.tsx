import { Link } from "react-router-dom";
import type { TransferFlow } from "@minibank/shared/src/hooks/useTransferFlow";
import { Button, SuccessState, formatCurrency, formatDisplayDate } from "@minibank/shared/src/index";

/** Feature-local: step 4 — success receipt. */
export function TransferSuccess({ flow }: { flow: TransferFlow }): React.JSX.Element {
  if (!flow.completed) return <p role="alert">Nothing to show.</p>;
  const c = flow.completed;
  return (
    <SuccessState title="Transfer Successful!" description="Your money has been sent successfully.">
      <dl className="transfer-rows">
        <div>
          <dt>Amount</dt>
          <dd>{formatCurrency(c.amount)}</dd>
        </div>
        <div>
          <dt>Recipient</dt>
          <dd>{c.recipientName}</dd>
        </div>
        <div>
          <dt>Account</dt>
          <dd className="transfer-mono">{c.accountNumber}</dd>
        </div>
        <div>
          <dt>Date</dt>
          <dd>{formatDisplayDate(c.date)}</dd>
        </div>
      </dl>
      <div className="transfer-btns">
        <Link to="/transactions" className="transfer-link">
          Transactions
        </Link>
        <Button onClick={flow.reset}>Done</Button>
      </div>
    </SuccessState>
  );
}
