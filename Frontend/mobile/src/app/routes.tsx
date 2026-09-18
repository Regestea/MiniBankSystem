import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { LoadingState, useAuth } from "@minibank/shared/src/index";
import { LoginScreen } from "../features/auth/LoginScreen";
import { RegisterScreen } from "../features/auth/RegisterScreen";
import { DashboardScreen } from "../features/dashboard/DashboardScreen";
import { TransactionsScreen } from "../features/transactions/TransactionsScreen";
import { TopUpScreen } from "../features/topup/TopUpScreen";
import { TransferScreen } from "../features/transfer/TransferScreen";
import { RecipientsScreen } from "../features/recipients/RecipientsScreen";
import { ProfileScreen } from "../features/profile/ProfileScreen";

export const AUTH_PATHS = ["/login", "/register"];

function Protected({ children }: { children: ReactNode }): React.JSX.Element {
  const { isAuthenticated, initializing } = useAuth();
  const location = useLocation();
  if (initializing) return <LoadingState message="Checking your session…" />;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <>{children}</>;
}

export function AppRoutes(): React.JSX.Element {
  return (
    <Routes>
      <Route path="/login" element={<LoginScreen />} />
      <Route path="/register" element={<RegisterScreen />} />
      <Route path="/" element={<Protected><DashboardScreen /></Protected>} />
      <Route path="/transactions" element={<Protected><TransactionsScreen /></Protected>} />
      <Route path="/topup" element={<Protected><TopUpScreen /></Protected>} />
      <Route path="/transfer" element={<Protected><TransferScreen /></Protected>} />
      <Route path="/recipients" element={<Protected><RecipientsScreen /></Protected>} />
      <Route path="/profile" element={<Protected><ProfileScreen /></Protected>} />
      <Route path="*" element={<Protected><DashboardScreen /></Protected>} />
    </Routes>
  );
}
