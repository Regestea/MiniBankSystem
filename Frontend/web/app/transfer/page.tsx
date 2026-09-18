"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect } from "react";
import {
  AccountNumberInput,
  AmountInput,
  Avatar,
  BalanceCard,
  Button,
  Card,
  Checkbox,
  LoadingState,
  PageHeader,
  Stepper,
  SuccessState,
  TRANSFER_FEE,
  formatCurrency,
  formatDisplayDate,
  useBank,
  useTransferFlow,
} from "@minibank/shared/src/index";
import styles from "./transfer.module.css";

const STEPS = ["Details", "Recipient", "Confirm", "Done"];

function stepIndex(step: string): number {
  if (step === "details") return 0;
  if (step === "recipient") return 1;
  if (step === "confirm" || step === "processing") return 2;
  return 3;
}

function TransferFlow(): React.JSX.Element {
  const params = useSearchParams();
  const { account } = useBank();
  const flow = useTransferFlow();

  const preselect = params.get("to");
  useEffect(() => {
    if (preselect) flow.startFromRecipient(preselect);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preselect]);

  return (
    <>
      <PageHeader title="Transfer Money" subtitle="Send USD by account number. Verify the recipient before confirming." />
      <Stepper steps={STEPS} current={stepIndex(flow.step)} />

      {flow.step === "details" && (
        <Card>
          <div className={styles.stack}>
            <AccountNumberInput
              value={flow.accountNumber}
              onChange={flow.setAccountNumber}
              error={flow.fieldError}
            />
            <AmountInput value={flow.amountInput} onChange={flow.setAmountInput} />
            <p className={styles.balanceHint}>
              Available: <strong>{formatCurrency(account?.balance ?? 0)}</strong>
            </p>
            <Button fullWidth loading={flow.resolving} onClick={() => void flow.continueToRecipient()}>
              Continue
            </Button>
          </div>
        </Card>
      )}

      {flow.step === "recipient" && flow.recipient && (
        <Card>
          <div className={styles.stack}>
            <div className={styles.recipientHero}>
              <Avatar name={flow.recipient.fullName} size={56} />
              <div>
                <p className={styles.eyebrow}>Recipient</p>
                <p className={styles.big}>{flow.recipient.fullName}</p>
                <p className={styles.mono}>{flow.recipient.accountNumber}</p>
              </div>
            </div>
            <dl className={styles.rows}>
              <div>
                <dt>Amount</dt>
                <dd>{formatCurrency(flow.amount ?? 0)}</dd>
              </div>
            </dl>
            <Checkbox
              label="Save this recipient for future transfers"
              checked={flow.saveRecipient}
              onChange={flow.setSaveRecipient}
            />
            <div className={styles.rowBtns}>
              <Button variant="ghost" onClick={flow.backToDetails}>
                Back
              </Button>
              <Button onClick={flow.goToConfirm}>Continue</Button>
            </div>
          </div>
        </Card>
      )}

      {flow.step === "confirm" && flow.recipient && (
        <Card>
          <div className={styles.stack}>
            <h2 className={styles.h2}>Confirm Transfer</h2>
            <dl className={styles.rows}>
              <div>
                <dt>Recipient</dt>
                <dd>{flow.recipient.fullName}</dd>
              </div>
              <div>
                <dt>Account Number</dt>
                <dd className={styles.mono}>{flow.recipient.accountNumber}</dd>
              </div>
              <div>
                <dt>Amount</dt>
                <dd>{formatCurrency(flow.amount ?? 0)}</dd>
              </div>
              <div>
                <dt>Fee</dt>
                <dd>{formatCurrency(TRANSFER_FEE)}</dd>
              </div>
              <div className={styles.total}>
                <dt>Total</dt>
                <dd>{formatCurrency(flow.amount ?? 0)}</dd>
              </div>
            </dl>
            {flow.error ? (
              <p className={styles.error} role="alert">
                {flow.error}
              </p>
            ) : null}
            <div className={styles.rowBtns}>
              <Button variant="ghost" onClick={flow.backToRecipient}>
                Back
              </Button>
              <Button loading={flow.processing} onClick={() => void flow.confirmTransfer()}>
                Transfer
              </Button>
            </div>
          </div>
        </Card>
      )}

      {flow.step === "processing" && <LoadingState message="Sending your money…" />}

      {flow.step === "success" && flow.completed && (
        <Card>
          <SuccessState
            title="Transfer Successful!"
            description="Your money has been sent successfully."
          >
            <dl className={styles.rows}>
              <div>
                <dt>Amount</dt>
                <dd>{formatCurrency(flow.completed.amount)}</dd>
              </div>
              <div>
                <dt>Recipient</dt>
                <dd>{flow.completed.recipientName}</dd>
              </div>
              <div>
                <dt>Account Number</dt>
                <dd className={styles.mono}>{flow.completed.accountNumber}</dd>
              </div>
              <div>
                <dt>Date</dt>
                <dd>{formatDisplayDate(flow.completed.date)}</dd>
              </div>
            </dl>
            <div className={styles.rowBtns}>
              <Link href="/transactions" className={styles.linkBtn}>
                View Transactions
              </Link>
              <Button
                onClick={() => {
                  flow.reset();
                }}
              >
                Done
              </Button>
            </div>
          </SuccessState>
        </Card>
      )}

      {account ? (
        <BalanceCard balance={account.balance} compact />
      ) : null}
    </>
  );
}

export default function TransferPage(): React.JSX.Element {
  return (
    <Suspense fallback={<LoadingState message="Loading transfer…" />}>
      <TransferFlow />
    </Suspense>
  );
}
