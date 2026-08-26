
namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public class DomainInvariantViolationException:DomainException
{
    public DomainInvariantViolationException( string field, object details) : base(ExceptionStatusCode.UnprocessableEntity,field, details)
    {
    }
}