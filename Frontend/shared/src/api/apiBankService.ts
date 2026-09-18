import type {
  BankAccount,
  CompletedTopUp,
  CompletedTransfer,
  Recipient,
  RecipientPreview,
  Transaction,
  TransactionDirection,
  TransactionStatus,
  TransactionType,
  UserProfile,
} from "../types";
import { maskName } from "../utils/format";
import { normalizeAccountNumber } from "../utils/validation";
import type { IBankService, TransferInput } from "../services/types";
import { extractErrorMessage, getApiClient } from "./client";
import type {
  AccountDto,
  BeneficiaryDto,
  CustomerOverviewDto,
  MyTransactionDto,
  PreviewTransferDto,
} from "./dto";

/**
 * Real backend implementation of IBankService — THE class both frontends use.
 * Drop-in replacement for MockBankService: same interface, live data.
 *
 * Mapping:
 *   getProfile      → GET /customers/overview (profile + accounts in one call)
 *   getAccount      → first Active account from overview (or GET /accounts fallback)
 *   getTransactions → GET /transactions/mine?pageSize=50
 *   getRecipients   → GET /beneficiaries
 *   previewRecipient→ POST /transfers/preview-by-number
 *   transfer        → POST /transfers/by-account-number {fromAccountId,toAccountNumber,amount,saveBeneficiary}
 *   topUp           → POST /accounts/{id}/topup {amount}
 *   saveRecipient   → POST /beneficiaries {accountNumber}
 */

function splitName(fullName: string): { firstName: string; lastName: string } {
  const parts = fullName.trim().split(/\s+/);
  return { firstName: parts[0] ?? fullName, lastName: parts.slice(1).join(" ") || "" };
}

function mapProfile(overview: CustomerOverviewDto, primary?: AccountDto | null): UserProfile {
  const accountNumber = primary?.accountNumber ?? overview.accounts[0]?.accountNumber ?? "";
  const { firstName, lastName } = splitName(overview.fullName);
  return {
    fullName: overview.fullName,
    firstName,
    lastName,
    accountNumber,
    phoneNumber: overview.phoneNumber,
    email: overview.email,
  };
}

function mapAccount(a: AccountDto | { accountId: string; accountNumber: string; balance: number }): BankAccount {
  return { id: a.accountId, accountNumber: a.accountNumber, balance: a.balance, currency: "USD" };
}

function mapTxType(raw: string): TransactionType {
  const t = raw.toLowerCase();
  if (t.includes("transfer")) return "transfer";
  if (t.includes("topup") || t.includes("top_up")) return "topup";
  if (t.includes("deposit")) return "deposit";
  if (t.includes("withdraw")) return "withdraw";
  return "transfer";
}

function mapTransaction(dto: MyTransactionDto, ownNumbers: Set<string>): Transaction {
  const occurred = new Date(dto.occurredOn);
  const date = Number.isNaN(occurred.getTime()) ? "" : occurred.toISOString().slice(0, 10);
  const time = Number.isNaN(occurred.getTime()) ? "" : occurred.toISOString().slice(11, 16);

  const isOut = dto.sourceAccountNumber != null && ownNumbers.has(dto.sourceAccountNumber);
  const isIn = dto.destinationAccountNumber != null && ownNumbers.has(dto.destinationAccountNumber);
  const direction: TransactionDirection = isOut && !isIn ? "out" : isIn && !isOut ? "in" : dto.amount < 0 ? "out" : "in";

  const counterparty =
    direction === "out" ? (dto.destinationAccountNumber ?? undefined) : (dto.sourceAccountNumber ?? undefined);
  const title =
    dto.description?.trim() ||
    (direction === "out"
      ? `Transfer to ${dto.destinationAccountNumber ?? "account"}`
      : `Received from ${dto.sourceAccountNumber ?? "account"}`);
  const status: TransactionStatus = "completed";

  return {
    id: dto.transactionId,
    title,
    amount: direction === "out" ? -Math.abs(dto.amount) : Math.abs(dto.amount),
    direction,
    type: mapTxType(dto.type),
    status,
    date,
    time,
    counterparty: counterparty ?? undefined,
    accountNumber: (direction === "out" ? dto.destinationAccountNumber : dto.sourceAccountNumber) ?? undefined,
  };
}

function mapRecipient(dto: BeneficiaryDto): Recipient {
  const { firstName, lastName } = splitName(dto.holderName);
  return {
    id: dto.beneficiaryId,
    firstName,
    lastName,
    fullName: dto.holderName,
    accountNumber: dto.accountNumber,
  };
}

function mapPreview(dto: PreviewTransferDto): RecipientPreview {
  const { firstName, lastName } = splitName(dto.holderFullName);
  return {
    fullName: dto.holderFullName,
    firstName,
    lastName,
    accountNumber: dto.accountNumber,
    maskedName: dto.maskedHolderName || maskName(dto.holderFullName),
  };
}

export class ApiBankService implements IBankService {
  private overviewCache: CustomerOverviewDto | null = null;

  private async loadOverview(force = false): Promise<CustomerOverviewDto> {
    if (this.overviewCache && !force) return this.overviewCache;
    try {
      const client = getApiClient();
      const res = await client.get<CustomerOverviewDto>("/customers/overview");
      this.overviewCache = res.data;
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not load your banking overview."));
    }
  }

  private pickPrimary(overview: CustomerOverviewDto): AccountDto {
    const active = overview.accounts.find((a) => a.status.toLowerCase() === "active") ?? overview.accounts[0];
    if (!active) throw new Error("No bank account found. Please open an account first.");
    return {
      accountId: active.accountId,
      accountNumber: active.accountNumber,
      accountType: active.accountType,
      status: active.status,
      balance: active.balance,
      createdAt: active.createdAt,
    };
  }

  /** All Active accounts — used by dashboard onboarding + account switcher. */
  async listAccounts(): Promise<BankAccount[]> {
    const overview = await this.loadOverview(true);
    return overview.accounts.map((a) => mapAccount({ accountId: a.accountId, accountNumber: a.accountNumber, balance: a.balance }));
  }

  async openAccount(accountType: "Current" | "Savings" = "Current"): Promise<BankAccount> {
    try {
      const client = getApiClient();
      const res = await client.post<{ accountId: string; accountNumber: string }>(`/accounts`, { accountType });
      this.overviewCache = null;
      // Newly opened accounts are PendingApproval with zero balance — refresh for the real row.
      const overview = await this.loadOverview(true);
      const found = overview.accounts.find((a) => a.accountId === res.data.accountId);
      if (found) return mapAccount({ accountId: found.accountId, accountNumber: found.accountNumber, balance: found.balance });
      return { id: res.data.accountId, accountNumber: res.data.accountNumber, balance: 0, currency: "USD" };
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not open an account."));
    }
  }

  async getProfile(): Promise<UserProfile> {
    const overview = await this.loadOverview();
    let primary: AccountDto | null = null;
    try {
      primary = this.pickPrimary(overview);
    } catch {
      primary = null;
    }
    return mapProfile(overview, primary);
  }

  async getAccount(): Promise<BankAccount> {
    const overview = await this.loadOverview();
    return mapAccount(this.pickPrimary(overview));
  }

  async getTransactions(): Promise<Transaction[]> {
    try {
      const client = getApiClient();
      const overview = await this.loadOverview();
      const ownNumbers = new Set(overview.accounts.map((a) => a.accountNumber));
      const res = await client.get<{ items: MyTransactionDto[] }>("/transactions/mine", {
        params: { page: 1, pageSize: 50 },
      });
      const items = Array.isArray(res.data?.items) ? res.data.items : [];
      return items.map((d) => mapTransaction(d, ownNumbers));
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not load transactions."));
    }
  }

  async getRecipients(): Promise<Recipient[]> {
    try {
      const client = getApiClient();
      const res = await client.get<BeneficiaryDto[]>("/beneficiaries");
      return (res.data ?? []).map(mapRecipient);
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not load recipients."));
    }
  }

  async previewRecipient(accountNumber: string): Promise<RecipientPreview | null> {
    const key = normalizeAccountNumber(accountNumber);
    try {
      const client = getApiClient();
      const res = await client.post<PreviewTransferDto>("/transfers/preview-by-number", { accountNumber: key });
      return mapPreview(res.data);
    } catch (e) {
      if ((e as { response?: { status?: number } })?.response?.status === 404) return null;
      // Surface a friendly message but keep the flow contract (null = not found is handled by callers only for 404).
      throw new Error(extractErrorMessage(e, "Could not resolve the recipient."));
    }
  }

  async transfer(input: TransferInput): Promise<CompletedTransfer> {
    const overview = await this.loadOverview();
    const from = this.pickPrimary(overview);
    try {
      const client = getApiClient();
      await client.post("/transfers/by-account-number", {
        fromAccountId: from.accountId,
        toAccountNumber: normalizeAccountNumber(input.accountNumber),
        amount: input.amount,
        saveBeneficiary: input.saveRecipient,
      });
      this.overviewCache = null;
      const preview = await this.previewRecipient(input.accountNumber).catch(() => null);
      return {
        amount: input.amount,
        recipientName: preview?.fullName ?? "Recipient",
        accountNumber: normalizeAccountNumber(input.accountNumber),
        date: new Date().toISOString().slice(0, 10),
      };
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Transfer failed. Please try again."));
    }
  }

  async topUp(amount: number): Promise<CompletedTopUp> {
    const overview = await this.loadOverview();
    const target = this.pickPrimary(overview);
    try {
      const client = getApiClient();
      await client.post(`/accounts/${target.accountId}/topup`, { amount });
      this.overviewCache = null;
      const fresh = await this.loadOverview(true);
      const updated = fresh.accounts.find((a) => a.accountId === target.accountId);
      return { amount, newBalance: updated?.balance ?? target.balance + amount };
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Top-up failed. Please try again."));
    }
  }

  async saveRecipient(accountNumber: string, fullName: string): Promise<Recipient> {
    try {
      const client = getApiClient();
      const res = await client.post<BeneficiaryDto>("/beneficiaries", {
        accountNumber: normalizeAccountNumber(accountNumber),
        nickname: fullName?.trim() ? fullName.trim() : null,
      });
      return mapRecipient(res.data);
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not save the recipient."));
    }
  }

  async removeRecipient(beneficiaryId: string): Promise<void> {
    try {
      const client = getApiClient();
      await client.delete(`/beneficiaries/${beneficiaryId}`);
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not remove the recipient."));
    }
  }

  async updateProfile(fullName: string, phoneNumber: string): Promise<UserProfile> {
    try {
      const client = getApiClient();
      await client.put("/customers/profile", { fullName, phoneNumber });
      this.overviewCache = null;
      return this.getProfile();
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not update your profile."));
    }
  }

  invalidateCache(): void {
    this.overviewCache = null;
  }
}

let singleton: ApiBankService | null = null;
/** Shared instance — both apps import this so auth headers stay in sync. */
export function getApiBankService(): ApiBankService {
  if (!singleton) singleton = new ApiBankService();
  return singleton;
}
