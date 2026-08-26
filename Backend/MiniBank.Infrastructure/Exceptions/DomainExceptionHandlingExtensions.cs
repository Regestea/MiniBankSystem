using Microsoft.AspNetCore.Builder;
using MiniBank.Infrastructure.Exceptions;

namespace MiniBank.Infrastructure;

public static class DomainExceptionHandlingExtensions
{
    /// <summary>
    /// Translates DomainException (with its StatusCode) and FluentValidation failures
    /// into structured JSON error responses.
    /// Register after UseAuthentication/UseAuthorization so it wraps the pipeline.
    /// </summary>
    public static IApplicationBuilder UseDomainExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
