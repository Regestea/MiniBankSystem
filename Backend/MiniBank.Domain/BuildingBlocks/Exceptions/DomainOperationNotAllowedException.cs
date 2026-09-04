
namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public class DomainOperationNotAllowedException : DomainException
{
    // NOTE: resource-state conflicts (frozen/closed/pending, wrong status) are 409 Conflict.
    // 405 is reserved for HTTP method mismatch and must never be returned for domain rules.
#pragma warning disable CS0618 // Kept for binary compat; new code should prefer DomainConflictException / DomainInvariantViolationException.
    public DomainOperationNotAllowedException(string field, object details) : base(ExceptionStatusCode.Conflict, field, details)
#pragma warning restore CS0618
    {
    }
}