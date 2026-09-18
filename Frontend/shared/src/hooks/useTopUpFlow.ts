import { useCallback, useState } from "react";
import { useBank } from "../store/BankContext";
import type { CompletedTopUp, TopUpStep } from "../types";
import { validateTopUpAmount } from "../utils/validation";

export interface TopUpFlow {
  step: TopUpStep;
  amountInput: string;
  amount: number | null;
  fieldError: string | null;
  processing: boolean;
  error: string | null;
  completed: CompletedTopUp | null;
  setAmountInput: (v: string) => void;
  selectPreset: (v: number) => void;
  /** amount → processing → success (live API top-up). */
  submit: () => Promise<void>;
  reset: () => void;
}

/** Shared top-up state machine used by BOTH web and mobile. */
export function useTopUpFlow(): TopUpFlow {
  const { topUp } = useBank();
  const [step, setStep] = useState<TopUpStep>("amount");
  const [amountInput, setAmountInput] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [completed, setCompleted] = useState<CompletedTopUp | null>(null);

  const amount = (() => {
    const n = Number(amountInput.replace(/[$,\s]/g, ""));
    return amountInput.trim() === "" || !Number.isFinite(n) ? null : Math.round(n * 100) / 100;
  })();

  const submit = useCallback(async () => {
    setError(null);
    const check = validateTopUpAmount(amount);
    if (!check.valid) {
      setFieldError(check.error);
      return;
    }
    setFieldError(null);
    setProcessing(true);
    setStep("processing");
    try {
      const result = await topUp(amount as number);
      setCompleted(result);
      setStep("success");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Top-up failed. Please try again.");
      setStep("amount");
    } finally {
      setProcessing(false);
    }
  }, [amount, topUp]);

  return {
    step,
    amountInput,
    amount,
    fieldError,
    processing,
    error,
    completed,
    setAmountInput: (v: string) => setAmountInput(v),
    selectPreset: (v: number) => {
      setAmountInput(String(v));
      setFieldError(null);
    },
    submit,
    reset: () => {
      setStep("amount");
      setAmountInput("");
      setFieldError(null);
      setError(null);
      setCompleted(null);
    },
  };
}
