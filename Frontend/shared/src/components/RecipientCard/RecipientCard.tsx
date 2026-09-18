import React from "react";
import styles from "./RecipientCard.module.css";
import type { Recipient } from "../../types";
import { Avatar } from "../Avatar/Avatar";

export function RecipientCard({
  recipient,
  onSelect,
  actionLabel = "Transfer",
}: {
  recipient: Recipient;
  onSelect?: (recipient: Recipient) => void;
  actionLabel?: string;
}): React.JSX.Element {
  const body = (
    <>
      <Avatar name={recipient.fullName} />
      <span className={styles.main}>
        <span className={styles.name}>{recipient.fullName}</span>
        <span className={styles.number}>{recipient.accountNumber}</span>
      </span>
      {onSelect ? <span className={styles.action}>{actionLabel} →</span> : null}
    </>
  );
  if (onSelect) {
    return (
      <button
        type="button"
        className={`${styles.card} ${styles.clickable}`}
        onClick={() => onSelect(recipient)}
        aria-label={`Transfer to ${recipient.fullName}`}
      >
        {body}
      </button>
    );
  }
  return <div className={styles.card}>{body}</div>;
}
