using FluentAssertions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Tests.Messaging;

/// <summary>
/// Regression tests for the real <see cref="Mediator"/> dispatch (no mocks).
/// Previously the response-dispatch expression tree converted Task{TResponse}
/// itself to object, so EVERY query/command with a response threw:
/// "Expression of type 'System.Object' cannot be used for return type 'Task{object}'".
/// Unit tests never caught it because they call handlers directly and API tests
/// mock IMediator.
/// </summary>
public sealed class MediatorTests
{
    private sealed record PingQuery(string Name) : IQuery<string>;

    private sealed class PingHandler : IQueryHandler<PingQuery, string>
    {
        public Task<string> HandleAsync(PingQuery request, CancellationToken cancellationToken)
            => Task.FromResult($"pong:{request.Name}");
    }

    private sealed record SumCommand(int A, int B) : ICommand<int>;

    private sealed class SumHandler : ICommandHandler<SumCommand, int>
    {
        public Task<int> HandleAsync(SumCommand command, CancellationToken cancellationToken)
            => Task.FromResult(command.A + command.B);
    }

    private sealed record DoSomething(string Value) : IRequest;

    private sealed class DoSomethingHandler : IRequestHandler<DoSomething>
    {
        public static string? LastValue;

        public Task HandleAsync(DoSomething request, CancellationToken cancellationToken)
        {
            LastValue = request.Value;
            return Task.CompletedTask;
        }
    }

    private sealed record UnregisteredQuery : IQuery<string>;

    private sealed class StubProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Add<T>(T instance) => _services[typeof(T)] = instance!;

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private static IMediator CreateMediator()
    {
        // Mirrors ConfigureServices.AddFeatureServices: each handler is registered
        // under every handler interface it implements, including IRequestHandler<,>.
        var provider = new StubProvider();
        provider.Add<IRequestHandler<PingQuery, string>>(new PingHandler());
        provider.Add<IQueryHandler<PingQuery, string>>(new PingHandler());
        provider.Add<IRequestHandler<SumCommand, int>>(new SumHandler());
        provider.Add<ICommandHandler<SumCommand, int>>(new SumHandler());
        provider.Add<IRequestHandler<DoSomething>>(new DoSomethingHandler());
        return new Mediator(provider);
    }

    [Fact]
    public async Task Send_Query_ReturnsHandlerResponse()
    {
        var mediator = CreateMediator();

        var result = await mediator.Send(new PingQuery("sara"));

        result.Should().Be("pong:sara");
    }

    [Fact]
    public async Task Send_Command_ReturnsHandlerResponse()
    {
        var mediator = CreateMediator();

        var result = await mediator.Send(new SumCommand(2, 3));

        result.Should().Be(5);
    }

    [Fact]
    public async Task Send_QueryTwice_ReusesCompiledDispatcher()
    {
        var mediator = CreateMediator();

        (await mediator.Send(new PingQuery("a"))).Should().Be("pong:a");
        (await mediator.Send(new PingQuery("b"))).Should().Be("pong:b");
    }

    [Fact]
    public async Task SendAsync_VoidRequest_InvokesHandler()
    {
        var mediator = CreateMediator();
        DoSomethingHandler.LastValue = null;

        await mediator.SendAsync(new DoSomething("hello"));

        DoSomethingHandler.LastValue.Should().Be("hello");
    }

    [Fact]
    public async Task Send_QueryWithoutHandler_ThrowsHandlerNotFound()
    {
        var mediator = CreateMediator();

        var act = async () => await mediator.Send(new UnregisteredQuery());

        await act.Should().ThrowAsync<HandlerNotFoundException>();
    }
}
