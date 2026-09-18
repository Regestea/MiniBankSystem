import { isValidAccountNumber, normalizeAccountNumber } from "./validation";

export function getEmailError(value: string): string | null {
  const v = value.trim().toLowerCase();
  if (!v) return "Email is required.";
  if (v.length > 254) return "Email is too long.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return "Enter a valid email address.";
  return null;
}

/** Mirrors RegisterCustomerValidator: 8+ chars with upper/lower/digit/special. */
export function getPasswordError(value: string): string | null {
  if (!value) return "Password is required.";
  if (value.length < 8) return "Password must be at least 8 characters.";
  if (value.length > 128) return "Password is too long.";
  if (!/[A-Z]/.test(value)) return "Password must contain at least one uppercase letter.";
  if (!/[a-z]/.test(value)) return "Password must contain at least one lowercase letter.";
  if (!/\d/.test(value)) return "Password must contain at least one digit.";
  if (!/[!@#$%^&*(),.?":{}|<>\-_=+[\]\\/`~]/.test(value))
    return "Password must contain at least one special character.";
  return null;
}

export function getFullNameError(value: string): string | null {
  const v = value.trim();
  if (!v) return "Full name is required.";
  if (v.length > 100) return "Full name is too long.";
  if (v.split(/\s+/).length < 2) return "Enter your first and last name.";
  return null;
}

/** Mirrors backend: digits only, 10–15 chars (normalize Persian digits + spaces/dashes). */
export function normalizePhone(value: string): string {
  const fa = "۰۱۲۳۴۵۶۷۸۹";
  const en = "0123456789";
  let out = value.trim();
  out = out
    .split("")
    .map((ch) => {
      const i = fa.indexOf(ch);
      return i >= 0 ? en[i] : ch;
    })
    .join("");
  return out.replace(/[\s\-()+]/g, "");
}

export function getPhoneError(value: string): string | null {
  const v = normalizePhone(value);
  if (!v) return "Phone number is required.";
  if (!/^\d{10,15}$/.test(v)) return "Phone number must be 10–15 digits.";
  return null;
}

export { isValidAccountNumber, normalizeAccountNumber };

export function getLoginErrors(email: string, password: string): { email?: string; password?: string } {
  const errors: { email?: string; password?: string } = {};
  const e = getEmailError(email);
  if (e) errors.email = e;
  if (!password) errors.password = "Password is required.";
  return errors;
}

export function getRegisterErrors(input: { email: string; password: string; fullName: string; phoneNumber: string }): {
  email?: string;
  password?: string;
  fullName?: string;
  phoneNumber?: string;
} {
  const errors: { email?: string; password?: string; fullName?: string; phoneNumber?: string } = {};
  const e = getEmailError(input.email);
  if (e) errors.email = e;
  const p = getPasswordError(input.password);
  if (p) errors.password = p;
  const n = getFullNameError(input.fullName);
  if (n) errors.fullName = n;
  const ph = getPhoneError(input.phoneNumber);
  if (ph) errors.phoneNumber = ph;
  return errors;
}
