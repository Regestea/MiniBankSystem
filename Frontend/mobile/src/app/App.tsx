import "@minibank/shared/src/theme.css";
import { useMemo } from "react";
import { HashRouter, useLocation } from "react-router-dom";
import { getApiBankService } from "@minibank/shared/src/api/apiBankService";
import { AuthProvider } from "@minibank/shared/src/store/AuthContext";
import { BankProvider } from "@minibank/shared/src/store/BankContext";
import { AUTH_PATHS, AppRoutes } from "./routes";
import { BottomNavigation } from "./BottomNavigation";

function Shell(): React.JSX.Element {
  const location = useLocation();
  const service = useMemo(() => getApiBankService(), []);
  const isAuthRoute = AUTH_PATHS.some((p) => location.pathname === p || location.pathname.startsWith(`${p}/`));

  return (
    <div className="mobile-shell">
      <main className="mobile-main">
        {isAuthRoute ? (
          <AppRoutes />
        ) : (
          <BankProvider service={service}>
            <AppRoutes />
          </BankProvider>
        )}
      </main>
      {isAuthRoute ? null : <BottomNavigation />}
    </div>
  );
}

/**
 * Mobile shell: touch-friendly viewport, bottom navigation, feature routes.
 * HashRouter keeps deep-links working inside the Capacitor WebView.
 */
export function App(): React.JSX.Element {
  return (
    <AuthProvider>
      <HashRouter>
        <Shell />
      </HashRouter>
    </AuthProvider>
  );
}
