using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Abstractions;
using MiniBank.Infrastructure.Exceptions;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Persistence.Repositories;

namespace MiniBank.Infrastructure;

/// <summary>Composition root for persistence, identity and cross-cutting concerns.</summary>
public static class ConfigureServices
{
    public static WebApplicationBuilder AddInfrastructureServices(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        string connectionName = "minibankdb")
    {
        ((IHostApplicationBuilder)builder).AddInfrastructureServices(configuration, connectionName);
        return builder;
    }

    /// <summary>Host-agnostic overload (works with any IHostApplicationBuilder).</summary>
    public static TBuilder AddInfrastructureServices<TBuilder>(
        this TBuilder builder,
        IConfiguration configuration,
        string connectionName = "minibankdb")
        where TBuilder : IHostApplicationBuilder
    {
        var connectionString = configuration.GetConnectionString(connectionName)
            ?? builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionName}' not found. Ensure AppHost wires .WithReference(postgresDb) or set ConnectionStrings:{connectionName}.");

        builder.Services.AddDbContext<MiniBankDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<ISqlConnectionFactory, NpgsqlConnectionFactory>();
        builder.Services.AddScoped<IAppUserDirectory, AppUserDirectory>();
        builder.Services.AddScoped<ExceptionMiddleware>();
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<AppUser>()
                        .AddRoles<IdentityRole>()
                        .AddEntityFrameworkStores<MiniBankDbContext>();
        builder.Services.AddScoped<AdminSeeder>();

        return builder;
    }

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();
        await db.Database.MigrateAsync();
    }
}
