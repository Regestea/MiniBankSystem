import React from "react";
import type { CSSProperties } from "react";
import styles from "./EmptyState.module.css";
import { imageUrls } from "../../constants/imageUrls";

export function EmptyState({
  title,
  description,
}: {
  title: string;
  description?: string;
}): React.JSX.Element {
  const scenic = { "--ocean-image": `url("${imageUrls.oceanCard}")` } as CSSProperties;
  return (
    <div className={styles.wrap} role="status">
      <div className={`${styles.art} ocean-placeholder`} style={scenic} aria-hidden="true">
        <span>≈</span>
      </div>
      <p className={styles.title}>{title}</p>
      {description ? <p className={styles.desc}>{description}</p> : null}
    </div>
  );
}
