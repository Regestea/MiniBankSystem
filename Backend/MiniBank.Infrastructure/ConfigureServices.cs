using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.KycAggregate;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Abstractions;
using MiniBank.Infrastructure.Exceptions;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Messaging;
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
        builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
        builder.Services.AddScoped<IKycRepository, KycRepository>();
        builder.Services.AddScoped<IAuditRepository, AuditRepository>();
        builder.Services.AddScoped<IRiskRepository, RiskRepository>();
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<ISqlConnectionFactory, NpgsqlConnectionFactory>();
        builder.Services.AddScoped<IIdentityUserService, IdentityUserService>();
        builder.Services.AddScoped<ExceptionMiddleware>();
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<IdentityUser<Guid>>()
                        .AddRoles<IdentityRole<Guid>>()
                        .AddEntityFrameworkStores<MiniBankDbContext>();

        // Disable AutoSaveChanges on the Identity UserStore so that UserManager.CreateAsync
        // stages changes in the EF change tracker without committing. The actual persist
        // happens in IUnitOfWork.SaveChangesAsync, ensuring IdentityUser + Customer +
        // CustomerRisk are saved atomically in a single database transaction.
        builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserStore<IdentityUser<Guid>>>(sp =>
        {
            var db = sp.GetRequiredService<MiniBankDbContext>();
            var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<IdentityUser<Guid>, IdentityRole<Guid>, MiniBankDbContext, Guid>(db);
            store.AutoSaveChanges = false;
            return store;
        });
        builder.Services.AddScoped<AdminSeeder>();
        builder.Services.AddScoped<DemoSeeder>();

        // Outbox processor for reliable domain event dispatch
        builder.Services.AddHostedService<OutboxProcessor>();

        // Identity reconciliation for orphan IdentityUsers from failed two-phase registrations
        builder.Services.AddHostedService<IdentityReconciliationService>();

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
