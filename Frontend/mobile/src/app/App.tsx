import "@minibank/shared/src/theme.css";
import { BankProvider } from "@minibank/shared/src/store/BankContext";
import { HashRouter } from "react-router-dom";
import { AppRoutes } from "./routes";
import { BottomNavigation } from "./BottomNavigation";

/**
 * Mobile shell: touch-friendly viewport, bottom navigation, feature routes.
 * HashRouter keeps deep-links working inside the Capacitor WebView.
 */
export function App(): React.JSX.Element {
  return (
    <BankProvider>
      <HashRouter>
        <div className="mobile-shell">
          <main className="mobile-main">
            <AppRoutes />
          </main>
          <BottomNavigation />
        </div>
      </HashRouter>
    </BankProvider>
  );
}
