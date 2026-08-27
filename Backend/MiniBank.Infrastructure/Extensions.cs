using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Abstractions;
using MiniBank.Infrastructure.Exceptions;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Persistence.Repositories;

namespace MiniBank.Infrastructure;

/// <summary>Legacy alias — use AddInfrastructureServices instead.</summary>
public static class Extensions
{
    /// <summary>Legacy alias for AddInfrastructureServices.</summary>
    [Obsolete("Use builder.AddInfrastructureServices(configuration) from ConfigureServices.cs instead.")]
    public static TBuilder AddMiniBankPersistence<TBuilder>(this TBuilder builder, string connectionName = "minibankdb")
        where TBuilder : IHostApplicationBuilder
        => builder.AddInfrastructureServices(builder.Configuration, connectionName);
}
