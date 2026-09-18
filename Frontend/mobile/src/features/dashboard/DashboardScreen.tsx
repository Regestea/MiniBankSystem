import { Link } from "react-router-dom";
import {
  BalanceCard,
  Card,
  LoadingState,
  SectionHeader,
  TransactionList,
  getGreeting,
  useBank,
} from "@minibank/shared/src/index";
import "./dashboard.css";

/** Feature: dashboard — greeting + balance + quick actions + recent activity. */
export function DashboardScreen(): React.JSX.Element {
  const { profile, account, transactions, loading, error } = useBank();

  if (loading) return <LoadingState message="Loading your overview…" />;
  if (error || !profile || !account) {
    return (
      <div className="mobile-screen">
        <Card>
          <p role="alert">{error ?? "Could not load banking data."}</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="mobile-screen">
      <header>
        <p className="dash-hello">
          {getGreeting()}, <strong>{profile.firstName}</strong>
        </p>
      </header>

      <BalanceCard balance={account.balance} accountNumber={account.accountNumber} />

      <div className="dash-actions" role="group" aria-label="Quick actions">
        <Link to="/topup" className="dash-action">
          <span aria-hidden="true">+</span>Top Up
        </Link>
        <Link to="/transfer" className="dash-action dash-action--primary">
          <span aria-hidden="true">➤</span>Transfer
        </Link>
        <Link to="/transactions" className="dash-action">
          <span aria-hidden="true">⇄</span>Activity
        </Link>
      </div>

      <Card>
        <SectionHeader title="Recent Transactions" action={<Link to="/transactions">See All</Link>} />
        <TransactionList transactions={transactions.slice(0, 4)} />
      </Card>
    </div>
  );
}
