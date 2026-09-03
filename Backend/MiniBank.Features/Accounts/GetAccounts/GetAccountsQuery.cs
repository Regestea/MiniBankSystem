using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetAccounts;

public sealed record GetAccountsQuery : IQuery<IReadOnlyList<AccountDto>>;

public sealed record AccountDto(
    Guid AccountId,
    string AccountNumber,
    string AccountType,
    string Status,
    decimal Balance,
    DateTimeOffset CreatedAt);
