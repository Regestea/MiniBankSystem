using FluentValidation;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetAccounts;

public sealed record GetAccountsQuery(int Page = 1, int PageSize = 50) : IQuery<IReadOnlyList<AccountDto>>;

public sealed class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    public GetAccountsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, BankingRules.MaxPageSize);
    }
}

public sealed record AccountDto(
    Guid AccountId,
    string AccountNumber,
    string AccountType,
    string Status,
    decimal Balance,
    DateTimeOffset CreatedAt);
