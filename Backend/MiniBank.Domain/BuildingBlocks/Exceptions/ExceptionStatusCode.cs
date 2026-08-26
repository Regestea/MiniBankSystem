namespace MiniBank.Domain.BuildingBlocks.Exceptions;

public enum ExceptionStatusCode
{
    NotFound = 404,
    BadRequest = 400,
    NotAllowed = 405,
    UnprocessableEntity = 422,
    Conflict = 409,
    InternalServerError = 500,
    Unauthorized = 401,
    Forbidden = 403
}