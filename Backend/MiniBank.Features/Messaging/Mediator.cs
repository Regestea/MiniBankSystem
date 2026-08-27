using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBank.Features.Messaging;

/// <summary>Lightweight mediator — resolves handlers, runs validators, fans out notifications.</summary>
internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypes = new();

    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        await ValidateAsync(request, cancellationToken).ConfigureAwait(false);

        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
        HandlerNotFoundException.ThrowIfHandlerNull(handler, request.GetType().Name);

        await handler!.HandleAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        await ValidateAsync((object)request, cancellationToken).ConfigureAwait(false);

        var requestType = request.GetType();

        if (!HandlerTypes.TryGetValue(requestType, out var handlerType))
        {
            handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            HandlerTypes.TryAdd(requestType, handlerType);
        }

        var handler = serviceProvider.GetService(handlerType);
        HandlerNotFoundException.ThrowIfHandlerNull(handler, requestType.Name);

        var method = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"HandleAsync not found on '{handlerType.Name}'.");

        var task = (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        return await task.ConfigureAwait(false);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
        if (handlers.Count == 0)
            return;

        var tasks = handlers.Select(h => h.HandleAsync(notification, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(command, cancellationToken);

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(query, cancellationToken);

    private async Task ValidateAsync<TRequest>(TRequest request, CancellationToken ct) where TRequest : IRequest
    {
        var validators = serviceProvider.GetServices<IValidator<TRequest>>().ToList();
        if (validators.Count == 0) return;

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(new ValidationContext<TRequest>(request), ct).ConfigureAwait(false);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }
    }

    private async Task ValidateAsync(object request, CancellationToken ct)
    {
        var requestType = request.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(validatorType);

        var enumerable = serviceProvider.GetService(enumerableType) as System.Collections.IEnumerable;
        if (enumerable is null) return;

        var hasAny = false;
        foreach (var validatorObj in enumerable)
        {
            hasAny = true;
            if (validatorObj is IValidator validator)
            {
                var context = new ValidationContext<object>(request);
                var result = await validator.ValidateAsync(context, ct).ConfigureAwait(false);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }
        }

        if (!hasAny)
        {
            var single = serviceProvider.GetService(validatorType) as IValidator;
            if (single is not null)
            {
                var ctx = new ValidationContext<object>(request);
                var r = await single.ValidateAsync(ctx, ct).ConfigureAwait(false);
                if (!r.IsValid) throw new ValidationException(r.Errors);
            }
        }
    }
}
