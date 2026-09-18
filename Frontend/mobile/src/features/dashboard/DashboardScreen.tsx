import { useState } from "react";
import { Link } from "react-router-dom";
import {
  BalanceCard,
  Button,
  Card,
  LoadingState,
  SectionHeader,
  TransactionList,
  getApiBankService,
  getGreeting,
  useBank,
} from "@minibank/shared/src/index";
import "./dashboard.css";

/** Feature: dashboard — greeting + balance + quick actions + recent activity (live API). */
export function DashboardScreen(): React.JSX.Element {
  const { profile, account, transactions, loading, error, refresh } = useBank();
  const [opening, setOpening] = useState(false);
  const [openError, setOpenError] = useState<string | null>(null);

  if (loading) return <LoadingState message="Loading your overview…" />;

  const needsAccount = !account || (error?.toLowerCase().includes("no bank account") ?? false);
  if (needsAccount && profile) {
    return (
      <div className="mobile-screen">
        <Card>
          <SectionHeader title="Welcome to Mini Bank" />
          <p>
            Hi <strong>{profile.fullName}</strong> — your profile is ready but you don&apos;t have a bank
            account yet.
          </p>
          {openError ? <p role="alert">{openError}</p> : null}
          <Button
            fullWidth
            loading={opening}
            onClick={() => {
              setOpening(true);
              setOpenError(null);
              getApiBankService()
                .openAccount("Current")
                .then(() => refresh())
                .catch((e: unknown) => setOpenError(e instanceof Error ? e.message : "Could not open an account."))
                .finally(() => setOpening(false));
            }}
          >
            Open my first account
          </Button>
        </Card>
      </div>
    );
  }

  if (error || !profile || !account) {
    return (
      <div className="mobile-screen">
        <Card>
          <p role="alert">{error ?? "Could not load banking data."}</p>
          <Button variant="secondary" onClick={() => void refresh()}>
            Retry
          </Button>
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
