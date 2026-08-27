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
        builder.AddServiceDefaults();
        builder.AddInfrastructureServices(builder.Configuration);
        builder.AddFeatureServices();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddTransient<IClaimsTransformation, UserRoleClaimsTransformation>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            await app.Services.MigrateDatabaseAsync();
            await using var scope = app.Services.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
            await seeder.SeedAsync();

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("MiniBank API")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
                    .WithPersistentAuthentication();
            });
        }

        app.UseHttpsRedirection();
        app.UseDomainExceptionHandling();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapIdentityApi<IdentityUser<Guid>>();
        app.MapControllers();

        app.Run();
    }
}
