using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniBank.Infrastructure.Exceptions;
using MiniBank.Infrastructure.Persistence;

namespace MiniBank.Infrastructure;

public static class Extensions
{
    /// <summary>
    /// Registers the write-side DbContext (PostgreSQL) and domain exception middleware.
    /// Connection string is provided by Aspire via WithReference(db).
    /// </summary>
    public static TBuilder AddMiniBankPersistence<TBuilder>(this TBuilder builder, string connectionName = "minibankdb")
        where TBuilder : IHostApplicationBuilder
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionName}' not found. Ensure AppHost wires .WithReference(postgresDb).");

        builder.Services.AddDbContext<MiniBankDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        builder.Services.AddScoped<ExceptionMiddleware>();

        return builder;
    }

    /// <summary>Applies pending migrations — call in Development only.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();
        await db.Database.MigrateAsync();
    }
}
