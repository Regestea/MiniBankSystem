using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetStatement;

public sealed record GetStatementQuery(Guid AccountId, Guid RequesterUserId) : IQuery<StatementResponse>;

public sealed record StatementResponse(
    Guid AccountId,
    string AccountNumber,
    string Status,
    decimal Balance,
    IReadOnlyList<StatementEntryDto> Entries);

public sealed record StatementEntryDto(
    Guid LedgerEntryId,
    string Type,
    decimal Amount,
    DateTimeOffset OccurredOn,
    string? ReferenceId,
    string? Description);
