using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MiniBank.Abstractions;
using MiniBank.Infrastructure.Identity;

namespace MiniBank.Api.Auth;

/// <summary>Current user context via HttpContext.</summary>
internal sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    UserManager<AppUser> userManager) : ICurrentUserContext
{
    private Guid? _userId;

    public Guid UserId
    {
        get
        {
            if (_userId.HasValue)
                return _userId.Value;

            var principal = httpContextAccessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException("No HTTP context.");

            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub")
                      ?? throw new UnauthorizedAccessException("User is not authenticated.");

            _userId = Guid.Parse(sub);
            return _userId.Value;
        }
    }

    public async Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(UserId.ToString());
        return user?.CustomerId?.Value;
    }
}
