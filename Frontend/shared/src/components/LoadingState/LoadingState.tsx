import React from "react";
import styles from "./LoadingState.module.css";

export function LoadingState({ message = "Loading…" }: { message?: string }): React.JSX.Element {
  return (
    <div className={styles.wrap} role="status" aria-live="polite">
      <span className={styles.ring} aria-hidden="true">
        <span className={styles.wave} />
      </span>
      <p className={styles.message}>{message}</p>
    </div>
  );
}
