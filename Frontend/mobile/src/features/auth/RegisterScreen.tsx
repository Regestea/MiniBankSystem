import { useState } from "react";
import type { CSSProperties } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Button,
  Card,
  Input,
  getRegisterErrors,
  imageUrls,
  normalizePhone,
  useAuth,
} from "@minibank/shared/src/index";
import "./auth.css";

/** Mobile register — same validation + registerAndLogin as web. */
export function RegisterScreen(): React.JSX.Element {
  const navigate = useNavigate();
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
      navigate("/", { replace: true });
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Registration failed.");
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
        <h1>Create your account</h1>
        <p>Instant onboarding — your first account opens automatically.</p>
      </div>
      <Card>
        <form className="auth-form" onSubmit={(e) => void handleSubmit(e)} noValidate>
          <Input
            label="Full name"
            autoComplete="name"
            placeholder="Alex Carter"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            error={fieldErrors.fullName}
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
            <p className="auth-error" role="alert">
              {submitError}
            </p>
          ) : null}
          <Button type="submit" fullWidth loading={loading}>
            Create account
          </Button>
        </form>
        <p className="auth-switch">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </Card>
    </div>
  );
}
