using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace MiniBank.Api.Auth;

/// <summary>
/// Fallback that grants the implicit "User" role to every authenticated principal that has
/// no role claims yet. Since <see cref="MiniBank.Features.Customers.RegisterCustomer.RegisterCustomerHandler"/>
/// persists the "User" role atomically during registration via
/// <see cref="MiniBank.Abstractions.IIdentityUserService.EnsureUserRoleAsync"/>,
/// this transformation is a defense-in-depth fallback for tokens issued before the DB role was persisted
/// and for tests. Roles are primarily DB-backed via AspNetUserRoles.
/// </summary>
internal sealed class UserRoleClaimsTransformation : IClaimsTransformation
{
    public const string UserRole = "User";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true &&
            !principal.HasClaim(c => c.Type is ClaimTypes.Role or "role"))
        {
            var identity = (ClaimsIdentity)principal.Identity;
            identity.AddClaim(new Claim(ClaimTypes.Role, UserRole));
        }

        return Task.FromResult(principal);
    }
}
