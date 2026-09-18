import React from "react";
import styles from "./EmptyState.module.css";

export function EmptyState({
  title,
  description,
}: {
  title: string;
  description?: string;
}): React.JSX.Element {
  return (
    <div className={styles.wrap} role="status">
      <div className={`${styles.art} ocean-placeholder`} aria-hidden="true">
        <span>≈</span>
      </div>
      <p className={styles.title}>{title}</p>
      {description ? <p className={styles.desc}>{description}</p> : null}
    </div>
  );
}
