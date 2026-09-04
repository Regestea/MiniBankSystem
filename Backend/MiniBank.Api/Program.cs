using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MiniBank.Api.Auth;
using MiniBank.Features;
using MiniBank.Abstractions;
using MiniBank.Infrastructure;
using MiniBank.Infrastructure.Exceptions;
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

        // Keep Console/Debug/OpenTelemetry only — the default EventLog provider crashes
        // when the Windows Event Log service is unavailable (CI, containers, restricted hosts).
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.AddServiceDefaults();
        builder.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddFeatureServices();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddTransient<IClaimsTransformation, UserRoleClaimsTransformation>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                if (origins is null)
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        origins = ["http://localhost:3000", "http://localhost:5173"];
                    }
                    else
                    {
                        // In production without config: log warning at startup, don't add CORS policy.
                        // Cross-origin requests will be blocked by browsers (secure default).
                        // App starts successfully — ops can set CORS__AllowedOrigins and restart.
                        return; // Skip adding policy
                    }
                }

                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(origins ?? [])
                          .AllowCredentials()
                          .WithHeaders("Authorization", "Content-Type", "Accept", "X-CSRF-TOKEN")
                          .WithMethods("GET", "POST", "PUT", "DELETE");
                });
            });

        // Rate limiting protection
        builder.AddCustomRateLimiting();

        // NOTE: no antiforgery/CSRF here by design. This API authenticates with
        // Authorization: Bearer tokens (not cookies), so cross-site request forgery
        // does not apply. Adding [ValidateAntiForgeryToken] to bearer endpoints
        // would be cargo-cult security and would force a test/prod divergence.

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // OpenAPI document in ALL environments (reviewers/ops need it in Production too).
        // Scalar UI + auto-migrate + seed stay Development-only — never in Production.
        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            await app.Services.MigrateDatabaseAsync();
            await using var scope = app.Services.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
            await seeder.SeedAsync();
            var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
            await demoSeeder.SeedAsync();

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("MiniBank API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .EnablePersistentAuthentication();
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseDomainExceptionHandling();
        // Rate limiting is time-window based and would make API tests flaky/order-dependent
        // (e.g. 11 anonymous admin 401 probes vs a 5/min anon budget → 429). Skipped in Testing only.
        if (!app.Environment.IsEnvironment("Testing"))
            app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // Identity endpoints (login/refresh/2fa/forgot/reset/...) with /register disabled —
        // registration goes through POST /customers (two-phase IdentityUser + Customer, see RegisterCustomerHandler).
        app.MapIdentityApiWithRegistrationGuard<IdentityUser<Guid>>();
        app.MapControllers();

        app.Run();
    }
}
