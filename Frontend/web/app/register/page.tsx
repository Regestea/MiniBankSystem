"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "@minibank/shared/src/components/Button/Button";
import { Card } from "@minibank/shared/src/components/Card/Card";
import { Input } from "@minibank/shared/src/components/Input/Input";
import { imageUrls } from "@minibank/shared/src/constants/imageUrls";
import { useAuth } from "@minibank/shared/src/store/AuthContext";
import { getRegisterErrors, normalizePhone } from "@minibank/shared/src/utils/authValidation";
import styles from "../auth.module.css";

export default function RegisterPage(): React.JSX.Element {
  const router = useRouter();
  const { register, loading } = useAuth();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    const errors = getRegisterErrors({ email, password, fullName, phoneNumber });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSubmitError(null);
    try {
      await register({
        email: email.trim().toLowerCase(),
        password,
        fullName: fullName.trim(),
        phoneNumber: normalizePhone(phoneNumber),
      });
      router.replace("/");
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Registration failed.");
    }
  };

  return (
    <div className={styles.wrap}>
      <div className={styles.brand}>
        <span
          className={`${styles.logo} ocean-placeholder`}
          style={{ "--ocean-image": `url("${imageUrls.loginBackground}")` } as CSSProperties}
          aria-hidden="true"
        >
          ≈
        </span>
        <h1 className={styles.title}>Create your account</h1>
        <p className={styles.sub}>Instant onboarding — your first account opens automatically.</p>
      </div>
      <Card>
        <form className={styles.form} onSubmit={(e) => void handleSubmit(e)} noValidate>
          <Input
            label="Full name"
            autoComplete="name"
            placeholder="Alex Carter"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            error={fieldErrors.fullName}
            hint="First and last name."
          />
          <Input
            label="Email"
            type="email"
            autoComplete="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={fieldErrors.email}
          />
          <Input
            label="Phone number"
            type="tel"
            autoComplete="tel"
            placeholder="09123456789"
            value={phoneNumber}
            onChange={(e) => setPhoneNumber(e.target.value)}
            error={fieldErrors.phoneNumber}
            hint="Digits only, 10–15 characters."
          />
          <Input
            label="Password"
            type="password"
            autoComplete="new-password"
            placeholder="8+ chars, upper + lower + digit + symbol"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={fieldErrors.password}
          />
          {submitError ? (
            <p className={styles.error} role="alert">
              {submitError}
            </p>
          ) : null}
          <Button type="submit" fullWidth loading={loading}>
            Create account
          </Button>
        </form>
        <p className={styles.switch}>
          Already have an account? <Link href="/login">Sign in</Link>
        </p>
      </Card>
    </div>
  );
}
