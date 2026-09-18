import React from "react";
import styles from "./Button.module.css";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "md" | "sm";
  fullWidth?: boolean;
  loading?: boolean;
}

export function Button({
  variant = "primary",
  size = "md",
  fullWidth = false,
  loading = false,
  disabled,
  className = "",
  children,
  ...rest
}: ButtonProps): React.JSX.Element {
  const cls = [
    styles.button,
    styles[variant],
    size === "sm" ? styles.sm : "",
    fullWidth ? styles.fullWidth : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");
  return (
    <button className={cls} disabled={disabled || loading} aria-busy={loading} {...rest}>
      {loading ? <span className={styles.spinner} aria-hidden="true" /> : null}
      <span>{loading ? "Processing…" : children}</span>
    </button>
  );
}
