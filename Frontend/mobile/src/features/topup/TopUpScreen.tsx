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
import "./topup.css";

/** Feature: topup — fake gateway (amount only) → processing → success. */
export function TopUpScreen(): React.JSX.Element {
  const { account } = useBank();
  const flow = useTopUpFlow();

  return (
    <div className="mobile-screen">
      <PageHeader title="Top Up" subtitle="Fake gateway — amount only." />

      {flow.step === "amount" && (
        <Card>
          <div className="topup-stack">
            <p className="topup-balance">
              Current Balance <strong>{formatCurrency(account?.balance ?? 0)}</strong>
            </p>
            <AmountInput value={flow.amountInput} onChange={flow.setAmountInput} error={flow.fieldError} />
            <div className="topup-presets" role="group" aria-label="Quick amounts">
              {TOP_UP_PRESETS.map((p) => (
                <button
                  key={p}
                  type="button"
                  className={`topup-preset${flow.amount === p ? " topup-preset--on" : ""}`}
                  onClick={() => flow.selectPreset(p)}
                >
                  ${p}
                </button>
              ))}
            </div>
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
          <SuccessState title="Topped Up!" description={`Added ${formatCurrency(flow.completed.amount)}.`}>
            <p className="topup-new">
              New Balance <strong>{formatCurrency(flow.completed.newBalance)}</strong>
            </p>
            <Button fullWidth onClick={flow.reset}>
              Done
            </Button>
          </SuccessState>
        </Card>
      )}
    </div>
  );
}
