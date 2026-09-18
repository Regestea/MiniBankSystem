import React from "react";
import type { CSSProperties } from "react";
import styles from "./SuccessState.module.css";
import { imageUrls } from "../../constants/imageUrls";

export function SuccessState({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children?: React.ReactNode;
}): React.JSX.Element {
  const scenic = { "--ocean-image": `url("${imageUrls.successBackground}")` } as CSSProperties;
  return (
    <div className={styles.wrap} role="status">
      <div className={`${styles.badge} ocean-placeholder`} style={scenic} aria-hidden="true">
        <span>✓</span>
      </div>
      <h2 className={styles.title}>{title}</h2>
      {description ? <p className={styles.desc}>{description}</p> : null}
      {children}
    </div>
  );
}
