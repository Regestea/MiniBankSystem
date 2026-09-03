using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features;

/// <summary>Composition root for the application layer.</summary>
public static class ConfigureServices
{
    /// <summary>Registers mediator, handlers and validators.</summary>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<Mediator>();
        services.AddScoped<IMediator>(sp => sp.GetRequiredService<Mediator>());
        services.AddScoped<ISender>(sp => sp.GetRequiredService<Mediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<Mediator>());
        services.AddScoped<ICustomerAccessGuard, CustomerAccessGuard>();

        var assembly = typeof(ConfigureServices).Assembly;
        var handlerInterfaces = new[]
        {
            typeof(IRequestHandler<>),          // void requests
            typeof(IRequestHandler<,>),         // request → response
            typeof(INotificationHandler<>),     // fan-out notifications
            typeof(ICommandHandler<,>),         // legacy alias
            typeof(IQueryHandler<,>)            // legacy alias
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

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
