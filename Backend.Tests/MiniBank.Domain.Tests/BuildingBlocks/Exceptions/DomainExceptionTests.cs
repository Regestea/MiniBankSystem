
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.Tests.BuildingBlocks.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void DomainValidationException_HasBadRequestStatus()
    {
        var ex = new DomainValidationException("field", "details");
        Assert.Equal(ExceptionStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("field", ex.Field);
        Assert.Equal("details", ex.Details);
    }

    [Fact]
    public void DomainInvariantViolationException_HasUnprocessableEntityStatus()
    {
        var ex = new DomainInvariantViolationException("amount", "insufficient");
        Assert.Equal(ExceptionStatusCode.UnprocessableEntity, ex.StatusCode);
    }

    [Fact]
    public void DomainOperationNotAllowedException_HasNotAllowedStatus()
    {
        var ex = new DomainOperationNotAllowedException("status", "not allowed");
        Assert.Equal(ExceptionStatusCode.NotAllowed, ex.StatusCode);
    }

    [Fact]
    public void DomainException_IsException()
    {
        var ex = new DomainValidationException("f", "d");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public void ExceptionStatusCode_HasExpectedValues()
    {
        Assert.Equal(404, (int)ExceptionStatusCode.NotFound);
        Assert.Equal(400, (int)ExceptionStatusCode.BadRequest);
        Assert.Equal(405, (int)ExceptionStatusCode.NotAllowed);
        Assert.Equal(422, (int)ExceptionStatusCode.UnprocessableEntity);
        Assert.Equal(409, (int)ExceptionStatusCode.Conflict);
    }
}
