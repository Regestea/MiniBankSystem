
namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public class DomainValidationException:DomainException
{
    public DomainValidationException( string field, object details) : base(ExceptionStatusCode.BadRequest,field, details)
    {
    }
}