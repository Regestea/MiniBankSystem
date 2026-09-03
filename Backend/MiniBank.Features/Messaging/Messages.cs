namespace MiniBank.Features.Messaging;

/// <summary>Void request — no response.</summary>
public interface IRequest;

/// <summary>Request with typed response.</summary>
public interface IRequest<out TResponse> : IRequest;

/// <summary>Fan-out notification — 0..N handlers.</summary>
public interface INotification;

/// <summary>State-changing command.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>Read-only query.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
