using Microsoft.AspNetCore.Identity;
using MiniBank.Api.Auth;
using MiniBank.Features;
using MiniBank.Features.Abstractions;
using MiniBank.Infrastructure;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Persistence;
using MiniBank.ServiceDefaults;
using Scalar.AspNetCore;

namespace MiniBank.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Persistence: PostgreSQL DbContext (connection string injected by Aspire) + repos/UoW/Dapper-factory
        builder.AddMiniBankPersistence();

        // Vertical slices: in-house mediator + handlers + validators
        builder.Services.AddMiniBankFeatures();

        // ASP.NET Core Identity — default tables + default APIs (/register /login /refresh …)
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<AppUser>()
                        .AddRoles<IdentityRole>()
                        .AddEntityFrameworkStores<MiniBankDbContext>();

        // Ambient current-user context for handlers (ownership checks)
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        builder.Services.AddControllers();

        // OpenAPI document (served at /openapi/v1.json)
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            await app.Services.MigrateDatabaseAsync();  // auto-apply migrations in Dev
            await AdminSeeder.SeedAsync(app.Services, app.Configuration); // Admin role + admin user from config

            // Scalar UI replaces .http files & Swagger UI — dark theme, interactive testing
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("MiniBank API")
                    .WithTheme(ScalarTheme.DeepSpace)   // dark theme
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .WithPersistentAuthentication();     // keeps Bearer token between requests
            });
        }

        app.UseHttpsRedirection();

        app.UseDomainExceptionHandling();   // DomainException/Validation → status-coded JSON
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapIdentityApi<AppUser>();      // Auth endpoints
        app.MapControllers();               // business features — Controllers only

        app.Run();
    }
}
