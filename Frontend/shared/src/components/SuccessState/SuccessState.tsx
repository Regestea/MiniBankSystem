import React from "react";
import styles from "./SuccessState.module.css";

export function SuccessState({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children?: React.ReactNode;
}): React.JSX.Element {
  return (
    <div className={styles.wrap} role="status">
      <div className={`${styles.badge} ocean-placeholder`} aria-hidden="true">
        <span>✓</span>
      </div>
      <h2 className={styles.title}>{title}</h2>
      {description ? <p className={styles.desc}>{description}</p> : null}
      {children}
    </div>
  );
}
