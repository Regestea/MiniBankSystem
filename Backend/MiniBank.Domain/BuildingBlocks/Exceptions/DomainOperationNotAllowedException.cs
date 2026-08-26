
namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public class DomainOperationNotAllowedException : DomainException
{
    public DomainOperationNotAllowedException( string field, object details) : base(ExceptionStatusCode.NotAllowed,field, details)
    {
    }
}