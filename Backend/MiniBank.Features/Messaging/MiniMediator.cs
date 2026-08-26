using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBank.Features.Messaging;

/// <summary>
/// In-house mediator implementation.
/// - Resolves the handler interface (ICommandHandler&lt;,&gt; / IQueryHandler&lt;,&gt;) per request type.
/// - Caches the invocation strategy per request type (built once via reflection, then pure delegate calls).
/// - Runs the registered IValidator&lt;TRequest&gt; (if any) before the handler — our pipeline behavior.
/// </summary>
internal sealed class MiniMediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestInvoker> InvokerCache = new();

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => Invoke<TResponse>(command, cancellationToken);

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => Invoke<TResponse>(query, cancellationToken);

    private async Task<TResponse> Invoke<TResponse>(object request, CancellationToken cancellationToken)
    {
        var invoker = InvokerCache.GetOrAdd(request.GetType(),
            rt => RequestInvoker.Build(rt, typeof(TResponse)));

        await invoker.ValidateAsync(serviceProvider, request, cancellationToken);

        var result = await invoker.HandleAsync(serviceProvider, request, cancellationToken);
        return (TResponse)result!;
    }

    /// <summary>Cached per-request-type dispatch + validation strategy.</summary>
    internal sealed class RequestInvoker(
        Func<IServiceProvider, object, CancellationToken, Task<object?>> handleAsync,
        Action<IServiceProvider, object, CancellationToken>? validateAsync)
    {
        public Task<object?> HandleAsync(IServiceProvider sp, object request, CancellationToken ct)
            => handleAsync(sp, request, ct);

        public async Task ValidateAsync(IServiceProvider sp, object request, CancellationToken ct)
        {
            if (validateAsync is null) return;
            validateAsync(sp, request, ct);
            await Task.CompletedTask;
        }

        public static RequestInvoker Build(Type requestType, Type declaredResponseType)
        {
            // ---- discover the marker interface (ICommand<T> / IQuery<T>) ----
            var marker = requestType.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(ICommand<>) ||
                 i.GetGenericTypeDefinition() == typeof(IQuery<>)))
                ?? throw new InvalidOperationException(
                    $"'{requestType.Name}' must implement ICommand<T> or IQuery<T>.");

            var isCommand = marker.GetGenericTypeDefinition() == typeof(ICommand<>);
            var markerResponseType = marker.GetGenericArguments()[0];

            if (markerResponseType != declaredResponseType)
                throw new InvalidOperationException(
                    $"Response type mismatch for '{requestType.Name}': " +
                    $"marker declares '{markerResponseType.Name}' but Send<{declaredResponseType.Name}>() was called.");

            // ---- handler interface: ICommandHandler<TReq,TRes> | IQueryHandler<TReq,TRes> ----
            var handlerInterface = (isCommand ? typeof(ICommandHandler<,>) : typeof(IQueryHandler<,>))
                .MakeGenericType(requestType, markerResponseType);

            var handleMethod = handlerInterface.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Handle() not found on '{handlerInterface.Name}'.");

            Task<object?> HandleAsync(IServiceProvider sp, object req, CancellationToken ct)
            {
                var handler = sp.GetRequiredService(handlerInterface);
                var task = (Task?)handleMethod.Invoke(handler, new[] { req, ct })
                    ?? throw new InvalidOperationException($"Handle() returned null task for '{requestType.Name}'.");
                return AwaitResult(task);
            }

            // ---- optional validation hook: IValidator<requestType> registered in DI ----
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);

            void Validate(IServiceProvider sp, object req, CancellationToken ct)
            {
                if (sp.GetService(validatorType) is not IValidator validator)
                    return;

                var context = new ValidationContext<object>(req);
                var result = validator.Validate(context);

                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }

            return new RequestInvoker(HandleAsync, Validate);
        }

        private static async Task<object?> AwaitResult(Task task)
        {
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty is null || resultProperty.PropertyType == typeof(void)
                ? null
                : resultProperty.GetValue(task);
        }
    }
}
