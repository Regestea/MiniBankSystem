using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniBank.Abstractions;

namespace MiniBank.Infrastructure.Identity;

/// <summary>
/// Background service that periodically reconciles orphan IdentityUsers created during
/// failed two-phase registration (IdentityUser created but Customer/Risk persistence failed).
/// Runs every 5 minutes by default.
/// </summary>
internal sealed class IdentityReconciliationService(
    IServiceProvider serviceProvider,
    ILogger<IdentityReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Identity reconciliation service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during identity reconciliation");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Identity reconciliation service stopped");
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityUserService>();

        var orphanIds = await identityService.GetOrphanUserIdsAsync(cancellationToken);
        if (orphanIds.Count == 0)
        {
            logger.LogDebug("No orphan IdentityUsers found");
            return;
        }

        logger.LogWarning("Found {Count} orphan IdentityUsers: {Ids}", orphanIds.Count, string.Join(", ", orphanIds));

        foreach (var userId in orphanIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                await identityService.DeleteUserAsync(userId, cancellationToken);
                logger.LogInformation("Deleted orphan IdentityUser {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete orphan IdentityUser {UserId}", userId);
            }
        }
    }
}