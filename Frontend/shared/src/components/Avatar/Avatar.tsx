import React from "react";
import styles from "./Avatar.module.css";
import { getInitials } from "../../utils/format";

const WAVES = ["waveA", "waveB", "waveC"] as const;

export function Avatar({ name, size = 48 }: { name: string; size?: number }): React.JSX.Element {
  const wave = WAVES[name.length % WAVES.length];
  return (
    <span
      className={`${styles.avatar} ${styles[wave]}`}
      style={{ width: size, height: size, fontSize: size * 0.34 }}
      aria-hidden="true"
    >
      {getInitials(name)}
    </span>
  );
}
