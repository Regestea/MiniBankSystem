namespace MiniBank.Features.Messaging;

/// <summary>Marks a request as a state-changing command (write side of CQRS — EF Core path).</summary>
public interface ICommand<out TResponse>;

/// <summary>Marks a request as a read-only query (read side of CQRS — Dapper path).</summary>
public interface IQuery<out TResponse>;
