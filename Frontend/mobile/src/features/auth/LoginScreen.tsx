import { useState } from "react";
import type { CSSProperties } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Button,
  Card,
  Input,
  getEmailError,
  imageUrls,
  useAuth,
} from "@minibank/shared/src/index";
import "./auth.css";

/** Mobile login — same AuthProvider + login() as web. */
export function LoginScreen(): React.JSX.Element {
  const navigate = useNavigate();
  const { login, loading } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

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
      navigate("/", { replace: true });
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Sign in failed.");
    }
  };

  return (
    <div className="mobile-screen auth-screen">
      <div className="auth-brand">
        <span
          className="auth-logo ocean-placeholder"
          style={{ "--ocean-image": `url("${imageUrls.loginBackground}")` } as CSSProperties}
          aria-hidden="true"
        >
          ≈
        </span>
        <h1>Welcome back</h1>
        <p>Sign in to your Mini Bank account.</p>
      </div>
      <Card>
        <form className="auth-form" onSubmit={(e) => void handleSubmit(e)} noValidate>
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
            <p className="auth-error" role="alert">
              {submitError}
            </p>
          ) : null}
          <Button type="submit" fullWidth loading={loading}>
            Sign in
          </Button>
        </form>
        <p className="auth-switch">
          New here? <Link to="/register">Create an account</Link>
        </p>
        <div className="auth-demo">
          Demo: <code>demo@minibank.local</code> / <code>Demo123!</code>
        </div>
      </Card>
    </div>
  );
}
