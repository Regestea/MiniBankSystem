import { useCallback, useState } from "react";
import { useBank } from "../store/BankContext";
import type { CompletedTransfer, RecipientPreview, TransferStep } from "../types";
import {
  getAccountNumberError,
  normalizeAccountNumber,
  validateTransferAmount,
} from "../utils/validation";

export interface TransferFlow {
  step: TransferStep;
  accountNumber: string;
  amountInput: string;
  amount: number | null;
  saveRecipient: boolean;
  recipient: RecipientPreview | null;
  fieldError: string | null;
  resolving: boolean;
  processing: boolean;
  error: string | null;
  completed: CompletedTransfer | null;
  setAccountNumber: (v: string) => void;
  setAmountInput: (v: string) => void;
  setSaveRecipient: (v: boolean) => void;
  /** Step 1 → validate + resolve recipient (mock preview-by-number). */
  continueToRecipient: () => Promise<void>;
  backToDetails: () => void;
  goToConfirm: () => void;
  backToRecipient: () => void;
  /** Step 3 → execute transfer. */
  confirmTransfer: () => Promise<void>;
  /** Pre-fill from a saved recipient (Recipients → transfer shortcut). */
  startFromRecipient: (accountNumber: string) => void;
  reset: () => void;
}

const initial = {
  accountNumber: "",
  amountInput: "",
  saveRecipient: true,
};

/**
 * Shared multi-step transfer state machine used by BOTH web and mobile:
 * details → recipient → confirm → processing → success.
 */
export function useTransferFlow(): TransferFlow {
  const { account, previewRecipient, transfer } = useBank();
  const balance = account?.balance ?? 0;

  const [step, setStep] = useState<TransferStep>("details");
  const [accountNumber, setAccountNumber] = useState(initial.accountNumber);
  const [amountInput, setAmountInput] = useState(initial.amountInput);
  const [saveRecipient, setSaveRecipient] = useState(true);
  const [recipient, setRecipient] = useState<RecipientPreview | null>(null);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [resolving, setResolving] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [completed, setCompleted] = useState<CompletedTransfer | null>(null);

  const amount = (() => {
    const n = Number(amountInput.replace(/[$,\s]/g, ""));
    return amountInput.trim() === "" || !Number.isFinite(n) ? null : Math.round(n * 100) / 100;
  })();

  const continueToRecipient = useCallback(async () => {
    setError(null);
    const numberError = getAccountNumberError(accountNumber);
    if (numberError) {
      setFieldError(numberError);
      return;
    }
    const amountCheck = validateTransferAmount(amount, balance);
    if (!amountCheck.valid) {
      setFieldError(amountCheck.error);
      return;
    }
    setFieldError(null);
    setResolving(true);
    try {
      const found = await previewRecipient(normalizeAccountNumber(accountNumber));
      if (!found) {
        setFieldError("We couldn't find an account with this number. Check it and try again.");
        return;
      }
      setRecipient(found);
      setStep("recipient");
    } catch {
      setFieldError("Something went wrong while resolving the recipient.");
    } finally {
      setResolving(false);
    }
  }, [accountNumber, amount, balance, previewRecipient]);

  const confirmTransfer = useCallback(async () => {
    if (!recipient || amount === null) return;
    setProcessing(true);
    setStep("processing");
    setError(null);
    try {
      const result = await transfer({
        accountNumber: recipient.accountNumber,
        amount,
        saveRecipient,
      });
      setCompleted(result);
      setStep("success");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Transfer failed. Please try again.");
      setStep("confirm");
    } finally {
      setProcessing(false);
    }
  }, [recipient, amount, saveRecipient, transfer]);

  const startFromRecipient = useCallback((number: string) => {
    setAccountNumber(number);
    setAmountInput("");
    setRecipient(null);
    setFieldError(null);
    setError(null);
    setCompleted(null);
    setStep("details");
  }, []);

  const reset = useCallback(() => {
    setStep("details");
    setAccountNumber(initial.accountNumber);
    setAmountInput(initial.amountInput);
    setSaveRecipient(true);
    setRecipient(null);
    setFieldError(null);
    setError(null);
    setCompleted(null);
  }, []);

  return {
    step,
    accountNumber,
    amountInput,
    amount,
    saveRecipient,
    recipient,
    fieldError,
    resolving,
    processing,
    error,
    completed,
    setAccountNumber,
    setAmountInput,
    setSaveRecipient,
    continueToRecipient,
    backToDetails: () => {
      setFieldError(null);
      setStep("details");
    },
    goToConfirm: () => setStep("confirm"),
    backToRecipient: () => setStep("recipient"),
    confirmTransfer,
    startFromRecipient,
    reset,
  };
}
