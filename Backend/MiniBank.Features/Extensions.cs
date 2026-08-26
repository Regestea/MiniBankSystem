using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniBank.Features.Messaging;

namespace MiniBank.Features;

public static class Extensions
{
    /// <summary>
    /// Registers the in-house mediator, all command/query handlers and FluentValidation validators
    /// found in this assembly. Call once from the composition root (Api).
    /// </summary>
    public static IServiceCollection AddMiniBankFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IMediator, MiniMediator>();

        var assembly = typeof(Extensions).Assembly;

        // Handlers — ICommandHandler<,> / IQueryHandler<,> implementations
        var handlerInterfaces = new[]
        {
            typeof(Messaging.ICommandHandler<,>),
            typeof(Messaging.IQueryHandler<,>)
        };

        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType &&
                    handlerInterfaces.Contains(@interface.GetGenericTypeDefinition()))
                {
                    services.AddScoped(@interface, type);
                }
            }
        }

        // Validators — auto-executed by MiniMediator before handlers
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
