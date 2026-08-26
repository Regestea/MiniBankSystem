namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public sealed class DomainConflictException(string field, object details)
    : DomainException(ExceptionStatusCode.Conflict, field, details);
