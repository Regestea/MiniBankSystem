import { extractErrorMessage, getApiClient } from "./client";
import type { CustomerDetailDto, CustomerOverviewDto, CustomerResponseDto, LoginResponseDto, RegisterRequestDto } from "./dto";
import { tokenStorage } from "./tokenStorage";

/**
 * Auth surface shared by web + mobile.
 * - register → POST /customers/register (anonymous, then caller should login)
 * - login    → POST /login (Identity bearer: {email,password} → access+refresh tokens)
 * - refresh  → POST /refresh {refreshToken}
 * - logout   → client-side (bearer has no server session to revoke)
 * - profile/overview → authenticated GETs used to hydrate the UI after login
 */

export interface LoginInput {
  email: string;
  password: string;
}

export const authApi = {
  async register(input: RegisterRequestDto): Promise<CustomerResponseDto> {
    try {
      const client = getApiClient();
      const res = await client.post<CustomerResponseDto>("/customers/register", input);
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Registration failed. Please try again."));
    }
  },

  async login(input: LoginInput): Promise<LoginResponseDto> {
    try {
      const client = getApiClient();
      const res = await client.post<LoginResponseDto>("/login", {
        email: input.email,
        password: input.password,
      });
      tokenStorage.save({
        accessToken: res.data.accessToken,
        refreshToken: res.data.refreshToken,
        expiresIn: res.data.expiresIn,
      });
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Sign in failed. Check your email and password."));
    }
  },

  /** Register then immediately sign in — the onboarding flow both apps use. */
  async registerAndLogin(input: RegisterRequestDto): Promise<LoginResponseDto> {
    await this.register(input);
    return this.login({ email: input.email, password: input.password });
  },

  async refresh(): Promise<string | null> {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) return null;
    try {
      const client = getApiClient();
      const res = await client.post<LoginResponseDto>("/refresh", { refreshToken });
      tokenStorage.save({
        accessToken: res.data.accessToken,
        refreshToken: res.data.refreshToken,
        expiresIn: res.data.expiresIn,
      });
      return res.data.accessToken;
    } catch {
      tokenStorage.clear();
      return null;
    }
  },

  logout(): void {
    tokenStorage.clear();
  },

  isAuthenticated(): boolean {
    return tokenStorage.hasTokens();
  },

  async getProfile(): Promise<CustomerDetailDto> {
    try {
      const client = getApiClient();
      const res = await client.get<CustomerDetailDto>("/customers/profile");
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not load your profile."));
    }
  },

  async getOverview(): Promise<CustomerOverviewDto> {
    try {
      const client = getApiClient();
      const res = await client.get<CustomerOverviewDto>("/customers/overview");
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not load your banking overview."));
    }
  },

  async updateProfile(fullName: string, phoneNumber: string): Promise<CustomerResponseDto> {
    try {
      const client = getApiClient();
      const res = await client.put<CustomerResponseDto>("/customers/profile", { fullName, phoneNumber });
      return res.data;
    } catch (e) {
      throw new Error(extractErrorMessage(e, "Could not update your profile."));
    }
  },
};
