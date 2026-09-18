import type {
  BankAccount,
  CompletedTopUp,
  CompletedTransfer,
  Recipient,
  RecipientPreview,
  Transaction,
  UserProfile,
} from "../types";

/**
 * Clean boundary between UI and data.
 *
 *   UI → IBankService → (today) MockBankService → (later) ApiBankService
 *
 * Maps 1:1 to the real backend endpoints (see Frontend README):
 * previewRecipient → POST /transfers/preview-by-number
 * transfer         → POST /transfers/by-account-number
 * topUp            → POST /accounts/{id}/topup
 * recipients       → GET/POST /beneficiaries
 */
export interface TransferInput {
  accountNumber: string;
  amount: number;
  saveRecipient: boolean;
}

export interface IBankService {
  getProfile(): Promise<UserProfile>;
  getAccount(): Promise<BankAccount>;
  getTransactions(): Promise<Transaction[]>;
  getRecipients(): Promise<Recipient[]>;
  /** Simulates resolving an account number to the holder name. Returns null when unknown. */
  previewRecipient(accountNumber: string): Promise<RecipientPreview | null>;
  transfer(input: TransferInput): Promise<CompletedTransfer>;
  topUp(amount: number): Promise<CompletedTopUp>;
  saveRecipient(accountNumber: string, fullName: string): Promise<Recipient>;
}
