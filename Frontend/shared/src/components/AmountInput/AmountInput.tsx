import React from "react";
import styles from "./AmountInput.module.css";
import { CURRENCY_SYMBOL } from "../../constants";

interface AmountInputProps {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  error?: string | null;
  id?: string;
  placeholder?: string;
}

/** Reusable USD amount field with a prominent dollar affordance. */
export function AmountInput({
  label = "Amount",
  value,
  onChange,
  error,
  id = "amount",
  placeholder = "0.00",
}: AmountInputProps): React.JSX.Element {
  const errorId = `${id}-error`;
  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      <div className={`${styles.wrap} ${error ? styles.invalid : ""}`}>
        <span className={styles.symbol} aria-hidden="true">
          {CURRENCY_SYMBOL}
        </span>
        <input
          id={id}
          className={styles.input}
          inputMode="decimal"
          autoComplete="off"
          placeholder={placeholder}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? errorId : undefined}
        />
        <span className={styles.code}>USD</span>
      </div>
      {error ? (
        <p className={styles.error} id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
