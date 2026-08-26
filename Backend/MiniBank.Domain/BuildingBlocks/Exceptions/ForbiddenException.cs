namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public sealed class ForbiddenException(string field, object details)
    : DomainException(ExceptionStatusCode.Forbidden, field, details);
