import type { TransferFlow } from "@minibank/shared/src/hooks/useTransferFlow";
import { AccountNumberInput, AmountInput, Button, formatCurrency, useBank } from "@minibank/shared/src/index";

/** Feature-local: step 1 — account number + amount. */
export function AccountNumberStep({ flow }: { flow: TransferFlow }): React.JSX.Element {
  const { account } = useBank();
  return (
    <div className="transfer-stack">
      <AccountNumberInput value={flow.accountNumber} onChange={flow.setAccountNumber} error={flow.fieldError} />
      <AmountInput value={flow.amountInput} onChange={flow.setAmountInput} />
      <p className="transfer-hint">
        Available: <strong>{formatCurrency(account?.balance ?? 0)}</strong>
      </p>
      <Button fullWidth loading={flow.resolving} onClick={() => void flow.continueToRecipient()}>
        Continue
      </Button>
    </div>
  );
}
