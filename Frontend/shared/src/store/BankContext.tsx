import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type {
  BankAccount,
  CompletedTopUp,
  CompletedTransfer,
  Recipient,
  RecipientPreview,
  Transaction,
  UserProfile,
} from "../types";
import { mockBankService } from "../services/mockBankService";
import type { IBankService } from "../services/types";

interface BankState {
  profile: UserProfile | null;
  account: BankAccount | null;
  transactions: Transaction[];
  recipients: Recipient[];
  loading: boolean;
  error: string | null;
  /** Re-fetch profile/account/transactions/recipients from the service. */
  refresh: () => Promise<void>;
  previewRecipient: (accountNumber: string) => Promise<RecipientPreview | null>;
  transfer: (input: { accountNumber: string; amount: number; saveRecipient: boolean }) => Promise<CompletedTransfer>;
  topUp: (amount: number) => Promise<CompletedTopUp>;
}

const BankContext = createContext<BankState | null>(null);

export function BankProvider({
  children,
  service = mockBankService,
}: {
  children: React.ReactNode;
  /** Inject a real ApiBankService later without touching UI code. */
  service?: IBankService;
}): React.JSX.Element {
  const serviceRef = useRef(service);
  serviceRef.current = service;

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [account, setAccount] = useState<BankAccount | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    const svc = serviceRef.current;
    setLoading(true);
    setError(null);
    try {
      const [p, a, t, r] = await Promise.all([
        svc.getProfile(),
        svc.getAccount(),
        svc.getTransactions(),
        svc.getRecipients(),
      ]);
      setProfile(p);
      setAccount(a);
      setTransactions(t);
      setRecipients(r);
    } catch {
      setError("Something went wrong while loading your banking data.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const previewRecipient = useCallback(async (accountNumber: string) => {
    return serviceRef.current.previewRecipient(accountNumber);
  }, []);

  const transfer = useCallback(async (input: { accountNumber: string; amount: number; saveRecipient: boolean }) => {
    const result = await serviceRef.current.transfer(input);
    // Sync mock state back into context (balance, history, recipients).
    const [a, t, r] = await Promise.all([
      serviceRef.current.getAccount(),
      serviceRef.current.getTransactions(),
      serviceRef.current.getRecipients(),
    ]);
    setAccount(a);
    setTransactions(t);
    setRecipients(r);
    return result;
  }, []);

  const topUp = useCallback(async (amount: number) => {
    const result = await serviceRef.current.topUp(amount);
    const [a, t] = await Promise.all([
      serviceRef.current.getAccount(),
      serviceRef.current.getTransactions(),
    ]);
    setAccount(a);
    setTransactions(t);
    return result;
  }, []);

  const value = useMemo<BankState>(
    () => ({
      profile,
      account,
      transactions,
      recipients,
      loading,
      error,
      refresh,
      previewRecipient,
      transfer,
      topUp,
    }),
    [profile, account, transactions, recipients, loading, error, refresh, previewRecipient, transfer, topUp],
  );

  return <BankContext.Provider value={value}>{children}</BankContext.Provider>;
}

export function useBank(): BankState {
  const ctx = useContext(BankContext);
  if (!ctx) throw new Error("useBank must be used inside <BankProvider>.");
  return ctx;
}
