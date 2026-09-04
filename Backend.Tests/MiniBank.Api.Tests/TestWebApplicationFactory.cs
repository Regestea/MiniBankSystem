using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniBank.Features.Messaging;
using MiniBank.Infrastructure.Persistence;
using NSubstitute;

namespace MiniBank.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ForwardingMediator _forwarder = new();

    /// <summary>Per-test substitute. Replaced (not mutated) on <see cref="ResetMock"/> so stubs never leak across tests.</summary>
    public IMediator MockMediator => _forwarder.Current;

    /// <summary>Swaps in a fresh substitute; the registered forwarder keeps pointing at the latest one.</summary>
    public void ResetMock() => _forwarder.Current = Substitute.For<IMediator>();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices((context, services) =>
        {
            // Remove the real DbContext registration
            services.RemoveAll(typeof(DbContextOptions<MiniBankDbContext>));
            services.RemoveAll(typeof(MiniBankDbContext));

            // Add InMemory database
            services.AddDbContext<MiniBankDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            // Replace mediator
            services.RemoveAll(typeof(IMediator));
            services.RemoveAll(typeof(ISender));
            services.RemoveAll(typeof(IPublisher));

            services.AddSingleton<IMediator>(_forwarder);
            services.AddSingleton<ISender>(_forwarder);
            services.AddSingleton<IPublisher>(_forwarder);

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });

        return base.CreateHost(builder);
    }

    public HttpClient CreateAuthenticatedClient(Guid? userId = null, string role = "User")
    {
        var id = userId ?? Guid.NewGuid();
        var client = WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<TestAuthOptions>(options =>
                {
                    options.UserId = id;
                    options.Role = role;
                });
            });
        }).CreateClient();

        // Add header so TestAuthHandler knows to authenticate this request
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }
}

public class TestAuthOptions
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = "User";
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only authenticate when X-Test-Auth header is present
        if (!Request.Headers.ContainsKey("X-Test-Auth"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var opts = Context.RequestServices.GetService<IOptionsMonitor<TestAuthOptions>>()?.CurrentValue
                   ?? new TestAuthOptions();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, opts.UserId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("email", "test@example.com"),
            new Claim(ClaimTypes.Role, opts.Role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Stable singleton registered in DI that forwards every call to the current per-test
/// substitute. Lets <see cref="TestWebApplicationFactory.ResetMock"/> swap substitutes
/// without rebuilding the host (a replaced singleton instance would keep serving the old mock).
/// </summary>
internal sealed class ForwardingMediator : IMediator
{
    public IMediator Current { get; set; } = Substitute.For<IMediator>();

    public Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
        => Current.SendAsync(request, cancellationToken);

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => Current.SendAsync(request, cancellationToken);

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Current.PublishAsync(notification, cancellationToken);

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => Current.Send(command, cancellationToken);

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => Current.Send(query, cancellationToken);
}
