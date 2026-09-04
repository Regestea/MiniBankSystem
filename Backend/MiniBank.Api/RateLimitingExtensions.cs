using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MiniBank.Api;

public static class RateLimitingExtensions
{
    public static WebApplicationBuilder AddCustomRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    ErrorCode = "rate_limit_exceeded",
                    Message = "Too many requests. Please try again later."
                }, cancellationToken);
            };

            options.AddPolicy("fixed", httpContext =>
            {
                var identity = httpContext.User.Identity;
                var isAuth = identity?.IsAuthenticated ?? false;
                // NOTE: Identity.Name is usually null for Bearer tokens — partition by
                // sub/NameIdentifier claim (stable user id) with IP fallback.
                var userKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: isAuth
                        ? $"auth_{userKey}"
                        : $"anon_{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = isAuth ? 100 : 20,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            options.AddPolicy("auth_endpoints", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("admin_endpoints", httpContext =>
            {
                var identity = httpContext.User.Identity;
                var isAuth = identity?.IsAuthenticated ?? false;
                var userKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: isAuth
                        ? $"admin_{userKey}"
                        : $"admin_anon_{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = isAuth ? 50 : 5,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        });

        return builder;
    }
}
