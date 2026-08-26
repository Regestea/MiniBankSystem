namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public sealed class NotFoundException(string field, object details)
    : DomainException(ExceptionStatusCode.NotFound, field, details);
