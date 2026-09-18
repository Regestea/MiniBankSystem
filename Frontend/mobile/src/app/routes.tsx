import { Route, Routes } from "react-router-dom";
import { DashboardScreen } from "../features/dashboard/DashboardScreen";
import { TransactionsScreen } from "../features/transactions/TransactionsScreen";
import { TopUpScreen } from "../features/topup/TopUpScreen";
import { TransferScreen } from "../features/transfer/TransferScreen";
import { RecipientsScreen } from "../features/recipients/RecipientsScreen";
import { ProfileScreen } from "../features/profile/ProfileScreen";

export function AppRoutes(): React.JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<DashboardScreen />} />
      <Route path="/transactions" element={<TransactionsScreen />} />
      <Route path="/topup" element={<TopUpScreen />} />
      <Route path="/transfer" element={<TransferScreen />} />
      <Route path="/recipients" element={<RecipientsScreen />} />
      <Route path="/profile" element={<ProfileScreen />} />
      <Route path="*" element={<DashboardScreen />} />
    </Routes>
  );
}
