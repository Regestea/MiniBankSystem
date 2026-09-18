import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import { getApiBaseUrl } from "./config";
import { tokenStorage } from "./tokenStorage";

/**
 * Single shared Axios instance for web + mobile.
 * - Attaches `Authorization: Bearer <accessToken>` automatically
 * - On 401 tries ONE `POST /refresh` with the stored refresh token, then retries
 * - Normalizes backend ProblemDetails errors into plain Error messages
 */

let sharedClient: AxiosInstance | null = null;
let refreshPromise: Promise<string | null> | null = null;

export function extractErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }
      | undefined;
    if (data) {
      if (data.detail && typeof data.detail === "string") return data.detail;
      if (data.title && typeof data.title === "string" && error.response?.status !== 500) {
        const details = data.errors ? ` ${Object.values(data.errors).flat().join(" ")}` : "";
        return `${data.title}${details}`.trim();
      }
      if (data.message && typeof data.message === "string") return data.message;
    }
    if (error.response?.status === 401) return "Session expired. Please sign in again.";
    if (error.response?.status === 403) return "You are not allowed to do that.";
    if (error.response?.status === 404) return "Not found.";
    if (error.response?.status === 409) return "Conflict — this was already done.";
    if (!error.response) return "Cannot reach the server. Check your connection.";
  }
  if (error instanceof Error && error.message) return error.message;
  return fallback;
}

async function tryRefresh(baseURL: string): Promise<string | null> {
  if (refreshPromise) return refreshPromise;
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) return null;

  refreshPromise = (async () => {
    try {
      const res = await axios.post(`${baseURL}/refresh`, { refreshToken }, { timeout: 10000 });
      const accessToken = res.data?.accessToken as string | undefined;
      const nextRefresh = res.data?.refreshToken as string | undefined;
      if (accessToken && nextRefresh) {
        tokenStorage.save({ accessToken, refreshToken: nextRefresh, expiresIn: res.data?.expiresIn });
        return accessToken;
      }
      return null;
    } catch {
      tokenStorage.clear();
      return null;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

export function getApiClient(baseUrlOverride?: string): AxiosInstance {
  if (sharedClient && !baseUrlOverride) return sharedClient;

  const baseURL = getApiBaseUrl(baseUrlOverride);
  const client = axios.create({ baseURL, timeout: 15000 });

  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = tokenStorage.getAccessToken();
    if (token) {
      config.headers = config.headers ?? {};
      (config.headers as Record<string, string>)["Authorization"] = `Bearer ${token}`;
    }
    return config;
  });

  client.interceptors.response.use(
    (res) => res,
    async (error: AxiosError) => {
      const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
      if (error.response?.status === 401 && original && !original._retried && !original.url?.includes("/login") && !original.url?.includes("/refresh")) {
        original._retried = true;
        const next = await tryRefresh(baseURL);
        if (next) {
          original.headers = original.headers ?? {};
          (original.headers as Record<string, string>)["Authorization"] = `Bearer ${next}`;
          return client(original);
        }
      }
      return Promise.reject(error);
    },
  );

  if (!baseUrlOverride) sharedClient = client;
  return client;
}

/** Test helper — resets the singleton (e.g. after base URL change). */
export function resetApiClient(): void {
  sharedClient = null;
  refreshPromise = null;
}
