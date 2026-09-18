"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import type { CSSProperties } from "react";
import { Button } from "@minibank/shared/src/components/Button/Button";
import { Card } from "@minibank/shared/src/components/Card/Card";
import { Input } from "@minibank/shared/src/components/Input/Input";
import { LoadingState } from "@minibank/shared/src/components/LoadingState/LoadingState";
import { imageUrls } from "@minibank/shared/src/constants/imageUrls";
import { useAuth } from "@minibank/shared/src/store/AuthContext";
import { getEmailError } from "@minibank/shared/src/utils/authValidation";
import styles from "../auth.module.css";

function LoginForm(): React.JSX.Element {
  const router = useRouter();
  const params = useSearchParams();
  const { login, loading } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [submitError, setSubmitError] = useState<string | null>(
    params.get("registered") ? "Account created — sign in to continue." : null,
  );

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    const errors: { email?: string; password?: string } = {};
    const emailError = getEmailError(email);
    if (emailError) errors.email = emailError;
    if (!password) errors.password = "Password is required.";
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSubmitError(null);
    try {
      await login(email, password);
      router.replace("/");
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Sign in failed.");
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
        <h1 className={styles.title}>Welcome back</h1>
        <p className={styles.sub}>Sign in to your Mini Bank account.</p>
      </div>
      <Card>
        <form className={styles.form} onSubmit={(e) => void handleSubmit(e)} noValidate>
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
            label="Password"
            type="password"
            autoComplete="current-password"
            placeholder="••••••••"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={fieldErrors.password}
          />
          {submitError ? (
            <p className={styles.error} role={submitError.startsWith("Account created") ? "status" : "alert"}>
              {submitError}
            </p>
          ) : null}
          <Button type="submit" fullWidth loading={loading}>
            Sign in
          </Button>
        </form>
        <p className={styles.switch}>
          New to Mini Bank? <Link href="/register">Create an account</Link>
        </p>
        <div className={styles.demo}>
          Demo account: <code>demo@minibank.local</code> / <code>Demo123!</code>
        </div>
      </Card>
    </div>
  );
}

export default function LoginPage(): React.JSX.Element {
  return (
    <Suspense fallback={<LoadingState message="Loading sign in…" />}>
      <LoginForm />
    </Suspense>
  );
}
