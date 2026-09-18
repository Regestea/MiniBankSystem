import { MOCK_LATENCY_MS } from "../constants";
import {
  mockAccount,
  mockAccountDirectory,
  mockRecipients,
  mockTransactions,
  mockUser,
} from "../mock-data/mock";
import type {
  BankAccount,
  CompletedTopUp,
  CompletedTransfer,
  Recipient,
  RecipientPreview,
  Transaction,
  UserProfile,
} from "../types";
import { formatDateTime, maskName } from "../utils/format";
import { normalizeAccountNumber } from "../utils/validation";
import type { IBankService, TransferInput } from "./types";

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

function clone<T>(value: T): T {
  return JSON.parse(JSON.stringify(value)) as T;
}

/**
 * In-memory fake backend. Holds balance / transactions / recipients state
 * so top-up and transfer flows feel interactive. Page refresh resets state
 * (no persistence in this prototype).
 */
export class MockBankService implements IBankService {
  private balance: number = mockAccount.balance;
  private transactions: Transaction[] = clone(mockTransactions);
  private recipients: Recipient[] = clone(mockRecipients);
  private txSeq = 100;

  async getProfile(): Promise<UserProfile> {
    await delay(MOCK_LATENCY_MS / 3);
    return clone(mockUser);
  }

  async getAccount(): Promise<BankAccount> {
    await delay(MOCK_LATENCY_MS / 3);
    return { ...clone(mockAccount), balance: this.balance };
  }

  async getTransactions(): Promise<Transaction[]> {
    await delay(MOCK_LATENCY_MS / 2);
    return clone(this.transactions);
  }

  async getRecipients(): Promise<Recipient[]> {
    await delay(MOCK_LATENCY_MS / 3);
    return clone(this.recipients);
  }

  /** Fake recipient lookup — mirrors preview-by-number semantics. */
  async previewRecipient(accountNumber: string): Promise<RecipientPreview | null> {
    await delay(MOCK_LATENCY_MS);
    const key = normalizeAccountNumber(accountNumber);
    // Saved recipients resolve first, then the static directory.
    const saved = this.recipients.find(
      (r) => normalizeAccountNumber(r.accountNumber) === key,
    );
    const found = saved
      ? { firstName: saved.firstName, lastName: saved.lastName }
      : mockAccountDirectory[key];
    if (!found) return null;
    const fullName = `${found.firstName} ${found.lastName}`;
    return {
      fullName,
      firstName: found.firstName,
      lastName: found.lastName,
      accountNumber: key,
      maskedName: maskName(fullName),
    };
  }

  async transfer(input: TransferInput): Promise<CompletedTransfer> {
    await delay(MOCK_LATENCY_MS);
    const key = normalizeAccountNumber(input.accountNumber);
    if (input.amount <= 0) throw new Error("Amount must be greater than zero.");
    if (input.amount > this.balance) throw new Error("Insufficient balance.");

    const preview = await this.previewRecipient(key);
    const recipientName = preview?.fullName ?? "Unknown recipient";

    this.balance = Math.round((this.balance - input.amount) * 100) / 100;
    const { date, time } = formatDateTime(new Date());
    this.transactions.unshift({
      id: `tx-mock-${this.txSeq++}`,
      title: `Transfer to ${recipientName}`,
      amount: -input.amount,
      direction: "out",
      type: "transfer",
      status: "completed",
      date,
      time,
      counterparty: recipientName,
      accountNumber: key,
    });

    if (input.saveRecipient && preview) {
      const exists = this.recipients.some(
        (r) => normalizeAccountNumber(r.accountNumber) === key,
      );
      if (!exists) {
        this.recipients.unshift({
          id: `rcp-mock-${Date.now()}`,
          firstName: preview.firstName,
          lastName: preview.lastName,
          fullName: preview.fullName,
          accountNumber: key,
        });
      }
    }

    return { amount: input.amount, recipientName, accountNumber: key, date };
  }

  async topUp(amount: number): Promise<CompletedTopUp> {
    // Fake payment gateway: always approves valid amounts, no card data.
    await delay(MOCK_LATENCY_MS);
    if (amount <= 0) throw new Error("Amount must be greater than zero.");
    this.balance = Math.round((this.balance + amount) * 100) / 100;
    const { date, time } = formatDateTime(new Date());
    this.transactions.unshift({
      id: `tx-mock-${this.txSeq++}`,
      title: "Top Up",
      amount,
      direction: "in",
      type: "topup",
      status: "completed",
      date,
      time,
    });
    return { amount, newBalance: this.balance };
  }

  async saveRecipient(accountNumber: string, fullName: string): Promise<Recipient> {
    await delay(MOCK_LATENCY_MS / 2);
    const key = normalizeAccountNumber(accountNumber);
    const parts = fullName.trim().split(/\s+/);
    const recipient: Recipient = {
      id: `rcp-mock-${Date.now()}`,
      firstName: parts[0] ?? fullName,
      lastName: parts.slice(1).join(" ") || "",
      fullName,
      accountNumber: key,
    };
    this.recipients.unshift(recipient);
    return clone(recipient);
  }
}

/** Shared singleton for the prototype (one mock ledger per app session). */
export const mockBankService = new MockBankService();
