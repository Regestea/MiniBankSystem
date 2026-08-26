namespace MiniBank.Features.Messaging;

/// <summary>
/// Lightweight in-house mediator — zero external dependencies.
/// Dispatches commands/queries to their registered handlers, running FluentValidation
/// automatically when a matching IValidator&lt;TRequest&gt; is registered in DI.
/// </summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
