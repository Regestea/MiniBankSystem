import React from "react";
import styles from "./SectionHeader.module.css";

export function SectionHeader({
  title,
  action,
}: {
  title: string;
  action?: React.ReactNode;
}): React.JSX.Element {
  return (
    <div className={styles.row}>
      <h2 className={styles.title}>{title}</h2>
      {action ? <div className={styles.action}>{action}</div> : null}
    </div>
  );
}
