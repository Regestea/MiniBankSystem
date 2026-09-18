import React from "react";
import styles from "./Stepper.module.css";

export function Stepper({ steps, current }: { steps: string[]; current: number }): React.JSX.Element {
  return (
    <ol className={styles.list} aria-label="Progress">
      {steps.map((label, i) => {
        const state = i < current ? "done" : i === current ? "current" : "todo";
        return (
          <li key={label} className={`${styles.item} ${styles[state]}`} aria-current={state === "current" ? "step" : undefined}>
            <span className={styles.dot}>{i < current ? "✓" : i + 1}</span>
            <span className={styles.label}>{label}</span>
            {i < steps.length - 1 ? <span className={styles.line} aria-hidden="true" /> : null}
          </li>
        );
      })}
    </ol>
  );
}
