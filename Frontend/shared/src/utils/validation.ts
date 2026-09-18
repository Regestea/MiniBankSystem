import { ACCOUNT_NUMBER_PATTERNS } from "../constants";

export function normalizeAccountNumber(value: string): string {
  return value.trim().toUpperCase().replace(/\s+/g, "");
}

/** Valid formats: IR-XXXXXXXXXX (10 digits) or exactly 16 digits. */
export function isValidAccountNumber(value: string): boolean {
  const v = normalizeAccountNumber(value);
  return ACCOUNT_NUMBER_PATTERNS.ir.test(v) || ACCOUNT_NUMBER_PATTERNS.plain16.test(v);
}

export function getAccountNumberError(value: string): string | null {
  const v = normalizeAccountNumber(value);
  if (!v) return "Account number is required.";
  if (!isValidAccountNumber(v)) {
    return "Enter a valid account number: IR-XXXXXXXXXX or a 16-digit number.";
  }
  return null;
}

export interface AmountValidation {
  valid: boolean;
  error: string | null;
}

export function validateTransferAmount(
  amount: number | null,
  balance: number,
): AmountValidation {
  if (amount === null || Number.isNaN(amount)) {
    return { valid: false, error: "Amount is required." };
  }
  if (amount <= 0) return { valid: false, error: "Amount must be greater than zero." };
  if (amount > balance) return { valid: false, error: "Insufficient balance." };
  return { valid: true, error: null };
}

export function validateTopUpAmount(amount: number | null): AmountValidation {
  if (amount === null || Number.isNaN(amount)) {
    return { valid: false, error: "Amount is required." };
  }
  if (amount <= 0) return { valid: false, error: "Amount must be greater than zero." };
  return { valid: true, error: null };
}

/** Parse a free-typed USD amount ("$250", "250.00") → number or null. */
export function parseAmountInput(raw: string): number | null {
  const cleaned = raw.replace(/[$,\s]/g, "");
  if (!cleaned) return null;
  const n = Number(cleaned);
  if (!Number.isFinite(n)) return null;
  return Math.round(n * 100) / 100;
}
