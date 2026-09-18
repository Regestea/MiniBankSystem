import type { BankAccount, Recipient, Transaction, UserProfile } from "../types";
import { INITIAL_BALANCE } from "../constants";

/**
 * Dedicated mock-data layer. UI components must consume these objects
 * (via IBankService / BankContext) as if they came from the real API —
 * never hardcode "$12,845.32" style values in presentation components.
 */

export const mockUser: UserProfile = {
  fullName: "Alex Carter",
  firstName: "Alex",
  lastName: "Carter",
  accountNumber: "IR-1234567890",
  phoneNumber: "+98 912 345 6789",
  email: "alex.carter@minibank.local",
};

export const mockAccount: BankAccount = {
  id: "00000000-0000-0000-0000-000000000001",
  accountNumber: "IR-1234567890",
  balance: INITIAL_BALANCE,
  currency: "USD",
};

export const mockTransactions: Transaction[] = [
  {
    id: "tx-001",
    title: "Transfer to John Smith",
    amount: -250.0,
    direction: "out",
    type: "transfer",
    status: "completed",
    date: "2025-08-24",
    time: "14:32",
    counterparty: "John Smith",
    accountNumber: "1234567890123456",
  },
  {
    id: "tx-002",
    title: "Top Up",
    amount: 500.0,
    direction: "in",
    type: "topup",
    status: "completed",
    date: "2025-08-23",
    time: "10:15",
  },
  {
    id: "tx-003",
    title: "Transfer to Sarah Miller",
    amount: -120.0,
    direction: "out",
    type: "transfer",
    status: "completed",
    date: "2025-08-22",
    time: "18:05",
    counterparty: "Sarah Miller",
    accountNumber: "IR-5678901234",
  },
  {
    id: "tx-004",
    title: "Transfer to Ali Reza",
    amount: -75.5,
    direction: "out",
    type: "transfer",
    status: "completed",
    date: "2025-08-21",
    time: "09:41",
    counterparty: "Ali Reza",
    accountNumber: "IR-3218765432",
  },
  {
    id: "tx-005",
    title: "Top Up",
    amount: 1000.0,
    direction: "in",
    type: "topup",
    status: "completed",
    date: "2025-08-20",
    time: "16:20",
  },
];

export const mockRecipients: Recipient[] = [
  {
    id: "rcp-001",
    firstName: "Sarah",
    lastName: "Miller",
    fullName: "Sarah Miller",
    accountNumber: "IR-5678901234",
  },
  {
    id: "rcp-002",
    firstName: "John",
    lastName: "Smith",
    fullName: "John Smith",
    accountNumber: "1234567890123456",
  },
  {
    id: "rcp-003",
    firstName: "Ali",
    lastName: "Reza",
    fullName: "Ali Reza",
    accountNumber: "IR-3218765432",
  },
];

/**
 * Fake account-number directory backing `previewRecipient()`.
 * Later replaced by POST /transfers/preview-by-number.
 */
export const mockAccountDirectory: Record<string, { firstName: string; lastName: string }> = {
  "IR-5678901234": { firstName: "Sarah", lastName: "Miller" },
  "1234567890123456": { firstName: "John", lastName: "Smith" },
  "IR-3218765432": { firstName: "Ali", lastName: "Reza" },
  "IR-1234567890": { firstName: "Alex", lastName: "Carter" },
};
