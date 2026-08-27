namespace MiniBank.Features.Messaging;

/// <summary>Thrown when no handler is registered for a request.</summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    public HandlerNotFoundException(string message) : base(message) { }

    public static void ThrowIfNull(object? handler, string requestName)
    {
        if (handler is null)
            throw new HandlerNotFoundException($"Handler not found for '{requestName}'.");
    }

    public static void ThrowIfHandlerNull(object? handler, string requestName) => ThrowIfNull(handler, requestName);
}
