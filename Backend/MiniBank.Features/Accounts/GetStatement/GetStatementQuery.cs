using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetStatement;

public sealed record GetStatementQuery(Guid AccountId, int Page = 1, int PageSize = 20) : IQuery<StatementResponse>;

public sealed class GetStatementQueryValidator : AbstractValidator<GetStatementQuery>
{
    public GetStatementQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed record StatementResponse(
    Guid AccountId,
    string AccountNumber,
    string Status,
    decimal Balance,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<StatementEntryDto> Entries);

public sealed record StatementEntryDto(
    Guid LedgerEntryId,
    string Type,
    decimal Amount,
    DateTimeOffset OccurredOn,
    string? ReferenceId,
    string? Description);
