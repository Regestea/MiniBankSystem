"use client";

import {
  AmountInput,
  Button,
  Card,
  LoadingState,
  PageHeader,
  SuccessState,
  TOP_UP_PRESETS,
  formatCurrency,
  useBank,
  useTopUpFlow,
} from "@minibank/shared/src/index";
import styles from "./topup.module.css";

export default function TopUpPage(): React.JSX.Element {
  const { account } = useBank();
  const flow = useTopUpFlow();

  return (
    <>
      <PageHeader title="Top Up Account" subtitle="Fake payment gateway — enter an amount only. No card needed." />

      {flow.step === "amount" && (
        <Card>
          <div className={styles.stack}>
            <div className={styles.current}>
              <span>Current Balance</span>
              <strong>{formatCurrency(account?.balance ?? 0)}</strong>
            </div>
            <AmountInput
              label="Enter Amount"
              value={flow.amountInput}
              onChange={flow.setAmountInput}
              error={flow.fieldError}
            />
            <div className={styles.presets} role="group" aria-label="Quick amounts">
              {TOP_UP_PRESETS.map((p) => (
                <button
                  key={p}
                  type="button"
                  className={`${styles.preset} ${flow.amount === p ? styles.selected : ""}`}
                  onClick={() => flow.selectPreset(p)}
                >
                  ${p}
                </button>
              ))}
            </div>
            {flow.error ? (
              <p className={styles.error} role="alert">
                {flow.error}
              </p>
            ) : null}
            <Button fullWidth onClick={() => void flow.submit()}>
              Continue
            </Button>
          </div>
        </Card>
      )}

      {flow.step === "processing" && (
        <Card>
          <LoadingState message="Processing fake payment…" />
        </Card>
      )}

      {flow.step === "success" && flow.completed && (
        <Card>
          <SuccessState
            title="Top-Up Successful!"
            description={`Added ${formatCurrency(flow.completed.amount)} to your account.`}
          >
            <p className={styles.newBalance}>
              New Balance <strong>{formatCurrency(flow.completed.newBalance)}</strong>
            </p>
            <Button fullWidth onClick={flow.reset}>
              Done
            </Button>
          </SuccessState>
        </Card>
      )}
    </>
  );
}
