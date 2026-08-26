using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Abstractions;
using MiniBank.Infrastructure.Exceptions;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Persistence.Repositories;

namespace MiniBank.Infrastructure;

public static class Extensions
{
    /// <summary>
    /// Registers the write-side DbContext (PostgreSQL), repositories, unit-of-work,
    /// Dapper connection factory and domain exception middleware.
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

        // CQRS ports & adapters
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MiniBankDbContext>());
        builder.Services.AddScoped<ISqlConnectionFactory, NpgsqlConnectionFactory>();
        builder.Services.AddScoped<IAppUserDirectory, AppUserDirectory>();

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
