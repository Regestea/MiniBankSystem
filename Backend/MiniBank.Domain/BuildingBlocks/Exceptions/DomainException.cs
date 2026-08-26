namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public abstract class DomainException : Exception
{
    public ExceptionStatusCode StatusCode { get; }
    public string Field { get; }
    public object Details { get; }

    protected DomainException(ExceptionStatusCode statusCode, string field, object details)
        : base($"{field}: {details}")
    {
        StatusCode = statusCode;
        Field = field;
        Details = details;
    }
}
