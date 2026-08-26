using Microsoft.AspNetCore.Builder;
using MiniBank.Infrastructure.Exceptions;

namespace MiniBank.Infrastructure;

public static class DomainExceptionHandlingExtensions
{
    /// <summary>
    /// Translates DomainException (with its StatusCode) into a JSON error response.
    /// Register after UseAuthentication/UseAuthorization so it wraps the pipeline.
    /// </summary>
    public static IApplicationBuilder UseDomainExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
