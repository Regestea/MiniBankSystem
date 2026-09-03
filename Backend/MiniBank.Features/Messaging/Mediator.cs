using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBank.Features.Messaging;

/// <summary>Lightweight mediator — resolves handlers, runs validators, fans out notifications.</summary>
internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> ResponseDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> VoidDispatchers = new();

    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        await ValidateAsync(request, cancellationToken).ConfigureAwait(false);

        var dispatcher = VoidDispatchers.GetOrAdd(request.GetType(), requestType =>
        {
            var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"HandleAsync not found on '{handlerType.Name}'.");

            return (sp, req, ct) =>
            {
                var handler = sp.GetService(handlerType);
                HandlerNotFoundException.ThrowIfHandlerNull(handler, requestType.Name);
                return (Task)handleMethod.Invoke(handler!, [req, ct])!;
            };
        });

        await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        await ValidateAsync(request, cancellationToken).ConfigureAwait(false);

        var requestType = request.GetType();

        var dispatcher = ResponseDispatchers.GetOrAdd(requestType, rt =>
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(rt, typeof(TResponse));
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"HandleAsync not found on '{handlerType.Name}'.");

            // Compile (handler, request, ct) => (Task<TResponse>)handler.HandleAsync(request, ct)
            var handlerParam = Expression.Parameter(typeof(object), "handler");
            var requestParam = Expression.Parameter(typeof(object), "request");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var call = Expression.Call(
                Expression.Convert(handlerParam, handlerType),
                handleMethod,
                Expression.Convert(requestParam, rt),
                ctParam);

            var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<object>>>(
                Expression.Convert(call, typeof(object)),
                handlerParam, requestParam, ctParam);

            var compiled = lambda.Compile();

            return (sp, req, ct) =>
            {
                var handler = sp.GetService(handlerType);
                HandlerNotFoundException.ThrowIfHandlerNull(handler, rt.Name);
                return compiled(handler!, req, ct);
            };
        });

        var result = await dispatcher(serviceProvider, request, cancellationToken).ConfigureAwait(false);
        return (TResponse)result;
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
        var requestType = request.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(validatorType);

        var enumerable = serviceProvider.GetService(enumerableType) as System.Collections.IEnumerable;
        if (enumerable is null) return;

        foreach (var validatorObj in enumerable)
        {
            if (validatorObj is IValidator validator)
            {
                var context = new ValidationContext<object>(request);
                var result = await validator.ValidateAsync(context, ct).ConfigureAwait(false);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }
        }
    }
}
