/** Shared domain types for the Mini Bank frontend prototype (mock-data backed). */

export type TransactionType = "transfer" | "topup" | "deposit" | "withdraw";
export type TransactionDirection = "in" | "out";
export type TransactionStatus = "completed" | "pending" | "failed";

export interface UserProfile {
  fullName: string;
  firstName: string;
  lastName: string;
  accountNumber: string;
  phoneNumber: string;
  email: string;
}

export interface BankAccount {
  id: string;
  accountNumber: string;
  balance: number;
  currency: "USD";
}

export interface Transaction {
  id: string;
  title: string;
  amount: number;
  /** Signed display amount: positive = income, negative = expense. */
  direction: TransactionDirection;
  type: TransactionType;
  status: TransactionStatus;
  date: string; // ISO date, e.g. "2025-08-24"
  time: string; // e.g. "14:32"
  counterparty?: string;
  accountNumber?: string;
}

export interface Recipient {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  accountNumber: string;
}

export interface RecipientPreview {
  fullName: string;
  firstName: string;
  lastName: string;
  accountNumber: string;
  maskedName: string;
}

export type TransferStep =
  | "details"
  | "recipient"
  | "confirm"
  | "processing"
  | "success";

export type TopUpStep = "amount" | "processing" | "success";

export interface TransferDraft {
  accountNumber: string;
  amount: number | null;
  saveRecipient: boolean;
}

export interface CompletedTransfer {
  amount: number;
  recipientName: string;
  accountNumber: string;
  date: string;
}

export interface CompletedTopUp {
  amount: number;
  newBalance: number;
}
