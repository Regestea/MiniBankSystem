namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public enum ExceptionStatusCode
{
    NotFound = 404,
    BadRequest = 400,
    [Obsolete("405 means HTTP method mismatch. Use Conflict (409) for state conflicts or UnprocessableEntity (422) for business-rule violations.")]
    NotAllowed = 405,
    UnprocessableEntity = 422,
    Conflict = 409,
    InternalServerError = 500,
    Unauthorized = 401,
    Forbidden = 403
}