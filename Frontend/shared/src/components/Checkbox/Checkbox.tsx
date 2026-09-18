import React from "react";
import styles from "./Checkbox.module.css";

interface CheckboxProps {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  id?: string;
}

export function Checkbox({ label, checked, onChange, id = "checkbox" }: CheckboxProps): React.JSX.Element {
  return (
    <label className={styles.row} htmlFor={id}>
      <input
        id={id}
        type="checkbox"
        className={styles.box}
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
      />
      <span className={styles.custom} aria-hidden="true">
        {checked ? "✓" : ""}
      </span>
      <span className={styles.label}>{label}</span>
    </label>
  );
}
