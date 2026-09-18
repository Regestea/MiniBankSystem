import React from "react";
import styles from "./Card.module.css";

export function Card({
  children,
  className = "",
  padded = true,
}: {
  children: React.ReactNode;
  className?: string;
  padded?: boolean;
}): React.JSX.Element {
  return <section className={`${styles.card} ${padded ? "" : styles.flush} ${className}`}>{children}</section>;
}
