/**
 * Token persistence shared by web + mobile.
 * Uses localStorage when available (browser), falls back to in-memory
 * so Next.js SSR / prerender never crashes.
 */

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn?: number;
}

const ACCESS_KEY = "minibank.accessToken";
const REFRESH_KEY = "minibank.refreshToken";

const memory = new Map<string, string>();

function storage(): Storage | null {
  try {
    if (typeof window !== "undefined" && window.localStorage) return window.localStorage;
  } catch {
    // private mode / SSR — fall through to memory
  }
  return null;
}

export const tokenStorage = {
  getAccessToken(): string | null {
    const s = storage();
    if (s) return s.getItem(ACCESS_KEY);
    return memory.get(ACCESS_KEY) ?? null;
  },
  getRefreshToken(): string | null {
    const s = storage();
    if (s) return s.getItem(REFRESH_KEY);
    return memory.get(REFRESH_KEY) ?? null;
  },
  save(tokens: AuthTokens): void {
    const s = storage();
    if (s) {
      s.setItem(ACCESS_KEY, tokens.accessToken);
      s.setItem(REFRESH_KEY, tokens.refreshToken);
    } else {
      memory.set(ACCESS_KEY, tokens.accessToken);
      memory.set(REFRESH_KEY, tokens.refreshToken);
    }
  },
  clear(): void {
    const s = storage();
    if (s) {
      s.removeItem(ACCESS_KEY);
      s.removeItem(REFRESH_KEY);
    } else {
      memory.delete(ACCESS_KEY);
      memory.delete(REFRESH_KEY);
    }
  },
  hasTokens(): boolean {
    return Boolean(this.getAccessToken());
  },
};
