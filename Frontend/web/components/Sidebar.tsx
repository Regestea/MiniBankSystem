"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./Sidebar.module.css";

const ITEMS = [
  { href: "/", label: "Dashboard", icon: "◈" },
  { href: "/transactions", label: "Transactions", icon: "⇄" },
  { href: "/transfer", label: "Transfer", icon: "➤" },
  { href: "/recipients", label: "Recipients", icon: "◉" },
  { href: "/profile", label: "Profile", icon: "○" },
];

export function Sidebar(): React.JSX.Element {
  const pathname = usePathname();
  return (
    <>
      <aside className={styles.sidebar} aria-label="Primary">
        <div className={`${styles.brand} ocean-placeholder`}>
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
          <p className={styles.note}>Water banking · mock prototype</p>
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
