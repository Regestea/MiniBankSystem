import { Card, LoadingState, PageHeader, TransactionList, useBank } from "@minibank/shared/src/index";

/** Feature: transactions — full history list. */
export function TransactionsScreen(): React.JSX.Element {
  const { transactions, loading, error } = useBank();
  if (loading) return <LoadingState message="Loading transactions…" />;
  return (
    <div className="mobile-screen">
      <PageHeader title="Transactions" subtitle="Newest first." />
      <Card>
        {error ? <p role="alert">{error}</p> : <TransactionList transactions={transactions} />}
      </Card>
    </div>
  );
}
