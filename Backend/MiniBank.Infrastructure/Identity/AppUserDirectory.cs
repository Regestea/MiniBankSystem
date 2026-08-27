using Microsoft.AspNetCore.Identity;
using MiniBank.Abstractions;
using MiniBank.Infrastructure.Identity;

namespace MiniBank.Infrastructure.Identity;

internal sealed class AppUserDirectory(UserManager<AppUser> userManager) : IAppUserDirectory
{
    public async Task<UserSnapshot?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new UserSnapshot(Guid.Parse(user.Id), user.Email, user.CustomerId?.Value);
    }

    public async Task<bool> TryAttachCustomerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        user.CustomerId = customerId;   // persisted by the handler's IUnitOfWork.SaveChangesAsync
        return true;
    }

    public async Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        if (!await userManager.IsInRoleAsync(user, "User"))
            await userManager.AddToRoleAsync(user, "User");
    }
}
