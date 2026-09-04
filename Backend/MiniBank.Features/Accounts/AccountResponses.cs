using MiniBank.Domain.AccountAggregate;

namespace MiniBank.Features.Accounts;

public sealed record AccountResponse(
    Guid AccountId,
    string AccountNumber,
    string AccountType,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static AccountResponse From(Account account)
        => new(account.Id.Value, account.AccountNumber, account.AccountType.ToString(),
               account.Status.ToString(), account.CreatedAt);
}

public sealed record TransactionResponse(
    Guid TransactionId,
    string Type,
    decimal Amount,
    string ReferenceId,
    DateTimeOffset OccurredOn);

public sealed record TransferResponse(
    Guid TransactionId,
    decimal Amount,
    string ReferenceId,
    Guid FromAccountId,
    Guid ToAccountId,
    DateTimeOffset OccurredOn);

public sealed record AccountStatusResponse(Guid AccountId, string Status, int Version);
