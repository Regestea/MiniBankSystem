"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { CSSProperties } from "react";
import { imageUrls } from "@minibank/shared/src/constants/imageUrls";
import { useAuth } from "@minibank/shared/src/store/AuthContext";
import styles from "./Sidebar.module.css";

const ITEMS = [
  { href: "/", label: "Dashboard", icon: "◈" },
  { href: "/transactions", label: "Transactions", icon: "⇄" },
  { href: "/transfer", label: "Transfer", icon: "➤" },
  { href: "/topup", label: "Top Up", icon: "+" },
  { href: "/recipients", label: "Recipients", icon: "◉" },
  { href: "/profile", label: "Profile", icon: "○" },
];

export function Sidebar(): React.JSX.Element {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();

  const handleLogout = (): void => {
    logout();
    router.replace("/login");
  };

  return (
    <>
      <aside className={styles.sidebar} aria-label="Primary">
        <div
          className={`${styles.brand} ocean-placeholder`}
          style={{ "--ocean-image": `url("${imageUrls.oceanCard}")` } as CSSProperties}
        >
          <span className={styles.logo} aria-hidden="true">
            ≈
          </span>
          <span className={styles.name}>Mini Bank</span>
        </div>
        <nav className={styles.nav}>
          {ITEMS.map((item) => {
            const active = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`${styles.link} ${active ? styles.active : ""}`}
                aria-current={active ? "page" : undefined}
              >
                <span aria-hidden="true">{item.icon}</span>
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className={styles.foot}>
          {user ? <p className={styles.note}>{user.email}</p> : null}
          <button type="button" className={styles.link} onClick={handleLogout}>
            <span aria-hidden="true">⎋</span>
            Sign out
          </button>
          <p className={styles.note}>Water banking · live API</p>
        </div>
      </aside>
      <nav className={styles.mobileNav} aria-label="Primary mobile">
        {[
          { href: "/", label: "Home" },
          { href: "/transactions", label: "Activity" },
          { href: "/transfer", label: "Transfer" },
          { href: "/recipients", label: "Saved" },
          { href: "/profile", label: "Profile" },
        ].map((item) => {
          const active = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={`${styles.mLink} ${active ? styles.mActive : ""}`}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </>
  );
}
