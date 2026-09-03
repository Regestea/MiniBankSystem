using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace MiniBank.Api.Auth;

/// <summary>
/// Blocks the default MapIdentityApi "/register" endpoint. Registration must go through
/// POST /auth/register, which atomically creates the IdentityUser AND the Customer aggregate.
/// The default endpoint would create an orphan IdentityUser with no Customer profile.
/// </summary>
public static class IdentityApiRouteGuard
{
    public static IEndpointConventionBuilder MapIdentityApiWithRegistrationGuard<TUser>(this IEndpointRouteBuilder endpoints)
        where TUser : class, new()
    {
        var group = endpoints.MapGroup("/");
        group.AddEndpointFilter(async (ctx, next) =>
        {
            if (HttpMethods.IsPost(ctx.HttpContext.Request.Method) &&
                ctx.HttpContext.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase))
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.NotFound(new
                {
                    ErrorCode = "endpoint_disabled",
                    Message = "Use POST /customers to create an account."
                });
            }

            return await next(ctx);
        });

        return group.MapIdentityApi<TUser>();
    }
}
