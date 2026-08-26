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

        // Persistence: PostgreSQL DbContext (connection string injected by Aspire) + middleware DI
        builder.AddMiniBankPersistence();

        // ASP.NET Core Identity — default tables + default APIs (/register /login /refresh …)
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<AppUser>()
                        .AddEntityFrameworkStores<MiniBankDbContext>();

        builder.Services.AddControllers();

        // OpenAPI document (served at /openapi/v1.json)
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            await app.Services.MigrateDatabaseAsync(); // auto-apply migrations in Dev

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

        app.UseDomainExceptionHandling();   // DomainException → JSON status-coded response
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapIdentityApi<AppUser>();      // Auth endpoints
        app.MapControllers();

        app.Run();
    }
}
