using Microsoft.AspNetCore.Builder;

namespace MiniBank.Infrastructure.Exceptions;

public static class DomainExceptionHandlingExtensions
{
    /// <summary>Maps domain and validation exceptions to JSON responses.</summary>
    public static IApplicationBuilder UseDomainExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
