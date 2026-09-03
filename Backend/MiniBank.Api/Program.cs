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

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Apply migrations on every startup (per plan); seeding only where configured.
        await app.Services.MigrateDatabaseAsync();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            await using var scope = app.Services.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
            await seeder.SeedAsync();

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("MiniBank API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .EnablePersistentAuthentication();
            });
        }

        app.UseHttpsRedirection();
        app.UseDomainExceptionHandling();
        app.UseAuthentication();
        app.UseAuthorization();

        // Identity endpoints (login/refresh/2fa/forgot/reset/...) with /register disabled —
        // registration goes through POST /customers (atomic IdentityUser + Customer).
        app.MapIdentityApiWithRegistrationGuard<IdentityUser<Guid>>();
        app.MapControllers();

        app.Run();
    }
}
