/** Shared constants — no hardcoded values inside presentation components. */

export const CURRENCY_SYMBOL = "$";
export const CURRENCY_CODE = "USD";

export const INITIAL_BALANCE = 12845.32;

export const TOP_UP_PRESETS = [10, 50, 100, 200] as const;

export const TRANSFER_FEE = 0;

/** IR-XXXXXXXXXX (IR- + exactly 10 digits) or exactly 16 digits. */
export const ACCOUNT_NUMBER_PATTERNS = {
  ir: /^IR-\d{10}$/,
  plain16: /^\d{16}$/,
} as const;

export const MOCK_LATENCY_MS = 900;

export const ROUTES = {
  dashboard: "/",
  transactions: "/transactions",
  transfer: "/transfer",
  topup: "/topup",
  recipients: "/recipients",
  profile: "/profile",
} as const;
