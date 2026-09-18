/**
 * Resolves the backend API base URL for BOTH frontends (Next.js web + Vite/Capacitor mobile).
 *
 * Priority:
 *   1. Explicit override via `NEXT_PUBLIC_API_URL` (web) or `VITE_API_URL` (mobile)
 *   2. Aspire service-discovery env (`services__api__http__0` / `API_HTTP`) when injected
 *   3. Local fallback `http://localhost:5194` (see Backend launchSettings + Backend.http)
 *
 * Keep this logic in ONE place so web and mobile never diverge.
 */

export const DEFAULT_API_BASE_URL = "http://localhost:5194";

function readProcessEnv(name: string): string | undefined {
  try {
    const proc = (globalThis as unknown as { process?: { env?: Record<string, string | undefined> } }).process;
    const value = proc?.env?.[name];
    return value && value.trim() ? value.trim() : undefined;
  } catch {
    return undefined;
  }
}

function readViteEnv(name: string): string | undefined {
  try {
    const meta = import.meta as unknown as { env?: Record<string, string | undefined> };
    const value = meta?.env?.[name];
    return value && value.trim() ? value.trim() : undefined;
  } catch {
    return undefined;
  }
}

function normalize(base: string): string {
  return base.replace(/\/+$/, "");
}

export function getApiBaseUrl(override?: string): string {
  if (override && override.trim()) return normalize(override);

  const fromNext = readProcessEnv("NEXT_PUBLIC_API_URL");
  if (fromNext) return normalize(fromNext);

  const fromVite = readViteEnv("VITE_API_URL");
  if (fromVite) return normalize(fromVite);

  // Aspire injects service-discovery variables for the referenced `api` resource.
  const fromAspire =
    readProcessEnv("services__api__http__0") ??
    readProcessEnv("API_HTTP") ??
    readViteEnv("API_HTTP");
  if (fromAspire) return normalize(fromAspire);

  const generic = readProcessEnv("MINIBANK_API_URL");
  if (generic) return normalize(generic);

  return DEFAULT_API_BASE_URL;
}
