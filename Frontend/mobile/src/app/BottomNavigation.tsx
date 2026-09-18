import { NavLink } from "react-router-dom";
import "./BottomNavigation.css";

const TABS = [
  { to: "/", label: "Home", end: true },
  { to: "/transactions", label: "Activity", end: false },
  { to: "/recipients", label: "Saved", end: false },
  { to: "/profile", label: "Profile", end: false },
];

export function BottomNavigation(): React.JSX.Element {
  return (
    <nav className="bottom-nav" aria-label="Primary">
      {TABS.map((t) => (
        <NavLink
          key={t.to}
          to={t.to}
          end={t.end}
          className={({ isActive }) => `bottom-nav__link${isActive ? " bottom-nav__link--active" : ""}`}
        >
          {t.label}
        </NavLink>
      ))}
    </nav>
  );
}
