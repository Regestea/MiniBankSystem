"use client";

import { Card, LoadingState, PageHeader, TransactionList, useBank } from "@minibank/shared/src/index";

export default function TransactionsPage(): React.JSX.Element {
  const { transactions, loading, error } = useBank();

  if (loading) return <LoadingState message="Loading transactions…" />;

  return (
    <>
      <PageHeader title="Transactions" subtitle="Every top-up and transfer, newest first." />
      <Card>
        {error ? (
          <p role="alert">{error}</p>
        ) : (
          <TransactionList transactions={transactions} />
        )}
      </Card>
    </>
  );
}
