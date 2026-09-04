namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string field, string details, Exception? innerException = null)
        : base(ExceptionStatusCode.Conflict, field, details, innerException)
    {
    }
}
