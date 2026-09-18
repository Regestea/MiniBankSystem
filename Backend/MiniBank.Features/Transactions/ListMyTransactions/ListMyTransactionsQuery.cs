using FluentValidation;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Transactions.ListMyTransactions;

/// <summary>Customer transaction history across ALL of the caller's accounts (newest first).</summary>
public sealed record ListMyTransactionsQuery(int Page = 1, int PageSize = 20) : IQuery<MyTransactionsResponse>;

public sealed class ListMyTransactionsValidator : AbstractValidator<ListMyTransactionsQuery>
{
    public ListMyTransactionsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, BankingRules.MaxPageSize);
    }
}

public sealed record MyTransactionsResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<MyTransactionDto> Items);

public sealed record MyTransactionDto(
    Guid TransactionId,
    string Type,
    decimal Amount,
    string? SourceAccountNumber,
    string? DestinationAccountNumber,
    DateTimeOffset OccurredOn,
    string ReferenceId,
    string? Description);
