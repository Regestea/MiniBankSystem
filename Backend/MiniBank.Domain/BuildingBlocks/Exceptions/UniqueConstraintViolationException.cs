namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public sealed class UniqueConstraintViolationException : DomainException
{
    public UniqueConstraintViolationException(string field, string details, Exception? innerException = null)
        : base(ExceptionStatusCode.Conflict, field, details, innerException)
    {
    }
}
