using Microsoft.Extensions.DependencyInjection;

namespace MiniBank.Features;

/// <summary>Legacy alias for AddFeatureServices.</summary>
public static class Extensions
{
    /// <summary>Legacy alias.</summary>
    [Obsolete("Use builder.AddFeatureServices() / services.AddFeatureServices() from ConfigureServices.cs instead.")]
    public static IServiceCollection AddMiniBankFeatures(this IServiceCollection services)
        => services.AddFeatureServices();
}
