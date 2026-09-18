import React from "react";
import styles from "./AccountNumberInput.module.css";
import { getAccountNumberError, isValidAccountNumber, normalizeAccountNumber } from "../../utils/validation";

interface AccountNumberInputProps {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  /** External error (e.g. recipient-not-found) takes precedence display-wise. */
  error?: string | null;
  showInlineValidation?: boolean;
  id?: string;
}

/** Reusable account-number field: IR-XXXXXXXXXX or 16 digits, with inline help. */
export function AccountNumberInput({
  label = "Account Number",
  value,
  onChange,
  error,
  showInlineValidation = true,
  id = "account-number",
}: AccountNumberInputProps): React.JSX.Element {
  const normalized = normalizeAccountNumber(value);
  const touched = value.trim().length > 0;
  const formatError = touched ? getAccountNumberError(value) : null;
  const valid = touched && isValidAccountNumber(value);
  const displayError = error ?? (showInlineValidation ? formatError : null);
  const errorId = `${id}-error`;

  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <div className={styles.wrap}>
        <input
          id={id}
          className={`${styles.input} ${displayError ? styles.invalid : ""} ${valid ? styles.valid : ""}`}
          autoComplete="off"
          spellCheck={false}
          placeholder="IR-XXXXXXXXXX or 16-digit number"
          value={value}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          aria-invalid={Boolean(displayError)}
          aria-describedby={`${id}-hint ${displayError ? errorId : ""}`}
        />
        {valid && !displayError ? (
          <span className={styles.check} aria-label="Valid format">
            ✓
          </span>
        ) : null}
      </div>
      <p className={styles.hint} id={`${id}-hint`}>
        Accepted formats: <code>IR-1234567890</code> or <code>1234567890123456</code> (current:{" "}
        {normalized || "—"}).
      </p>
      {displayError ? (
        <p className={styles.error} id={errorId} role="alert">
          {displayError}
        </p>
      ) : null}
    </div>
  );
}
