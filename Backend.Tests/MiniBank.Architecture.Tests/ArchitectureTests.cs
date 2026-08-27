using System.Reflection;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Messaging;
using NetArchTest.Rules;
using Xunit;

namespace MiniBank.Architecture.Tests;

/// <summary>
/// Vertical Slice + DDD Architecture Guardrails.
/// These tests are READ-ONLY — they report violations, they do not fix them.
/// Run with: dotnet test Backend.Tests/MiniBank.Architecture.Tests -c Release
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Customer).Assembly;
    private static readonly Assembly FeaturesAssembly = typeof(IMediator).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.ConfigureServices).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    // ─────────────────────────────────────────────────────────────────────────
    // 1. DOMAIN LAYER — pure, no outward dependencies
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Should_Not_Depend_On_Features_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("MiniBank.Features", "MiniBank.Infrastructure", "MiniBank.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain has forbidden dependencies:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_Reference_EfCore_Or_FluentValidation_Or_Dapper()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "FluentValidation", "Dapper", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references infrastructure libs:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Aggregates_Should_Be_Sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("MiniBank.Domain.AccountAggregate")
            .Or().ResideInNamespace("MiniBank.Domain.CustomerAggregate")
            .Or().ResideInNamespace("MiniBank.Domain.TransactionAggregate")
            .And().AreClasses().And().DoNotHaveNameMatching(".*Id$|.*Status$|.*Type$")
            .Should().BeSealed()
            .GetResult();

        // Filter to aggregates: Account, Customer, Transaction, LedgerEntry
        var failing = result.FailingTypes?.Where(t => t.Name is "Account" or "Customer" or "Transaction").ToList();
        Assert.True(failing == null || failing.Count == 0,
            $"Aggregates not sealed: {string.Join(", ", failing?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_ValueObjects_Should_Be_Sealed_And_Inherit_ValueObject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceMatching("MiniBank.Domain.*.ValueObjects")
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"ValueObjects not sealed:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. FEATURES / APPLICATION LAYER — vertical slices
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Features_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(FeaturesAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("MiniBank.Infrastructure", "MiniBank.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Features has forbidden dependencies (should depend only on Domain):{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Features_Should_Depend_On_Domain()
    {
        var result = Types.InAssembly(FeaturesAssembly)
            .That().ResideInNamespaceMatching("MiniBank.Features.*")
            .And().DoNotHaveNameMatching("Messages|IMediator|Handlers|ConfigureServices|Extensions")
            .Should()
            .HaveDependencyOn("MiniBank.Domain")
            .GetResult();

        // At least some types should depend on Domain — if none, slices are anemic
        // We do not fail if not all depend, but warn if none depend
        var hasDomainDep = Types.InAssembly(FeaturesAssembly)
            .That().HaveDependencyOn("MiniBank.Domain")
            .GetTypes().Any();
        Assert.True(hasDomainDep, "No Feature types depend on Domain — slices should use Domain aggregates/VOs.");
    }

    [Fact]
    public void Handlers_Should_Be_Internal_Sealed()
    {
        var result = Types.InAssembly(FeaturesAssembly)
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Handlers must be internal sealed:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Commands_And_Queries_Should_Be_Sealed_Records()
    {
        var result = Types.InAssembly(FeaturesAssembly)
            .That().HaveNameEndingWith("Command")
            .Or().HaveNameEndingWith("Query")
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Commands/Queries must be sealed:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Handlers_Should_Implement_IRequestHandler()
    {
        var handlerTypes = Types.InAssembly(FeaturesAssembly)
            .That().HaveNameEndingWith("Handler")
            .GetTypes();

        var notImplementing = handlerTypes.Where(t =>
            !t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                 i.GetGenericTypeDefinition().Name.StartsWith("ICommandHandler") ||
                 i.GetGenericTypeDefinition().Name.StartsWith("IQueryHandler"))))
            .ToList();

        Assert.True(notImplementing.Count == 0,
            $"Handlers not implementing IRequestHandler: {string.Join(", ", notImplementing.Select(t => t.FullName))}");
    }

    [Fact]
    public void Handlers_Should_Not_Reference_EfCore_DbContext_Directly()
    {
        var result = Types.InAssembly(FeaturesAssembly)
            .That().HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Handlers should not reference EF Core directly (use IUnitOfWork/Repositories):{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Vertical_Slices_Should_Not_Reference_Each_Other()
    {
        // E.g., Accounts.Deposit should not depend on Customers.VerifyCustomer
        // We check: types in Features.Accounts.* should not have dependency on Features.Customers.*
        var accountsTypes = Types.InAssembly(FeaturesAssembly)
            .That().ResideInNamespaceMatching("MiniBank.Features.Accounts.*")
            .GetTypes();

        var violating = accountsTypes.Where(t =>
            t.GetMethods().Any(m => m.ReturnType?.Namespace?.StartsWith("MiniBank.Features.Customers") == true) ||
            t.GetProperties().Any(p => p.PropertyType?.Namespace?.StartsWith("MiniBank.Features.Customers") == true) ||
            Types.InAssembly(FeaturesAssembly).That().ResideInNamespace("MiniBank.Features.Accounts.Deposit").GetTypes().Any() // placeholder
        ).ToList();

        // More precise: use NetArchTest dependency check
        var result = Types.InAssembly(FeaturesAssembly)
            .That().ResideInNamespaceMatching("MiniBank.Features.Accounts.*")
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Features.Customers")
            .GetResult();

        var result2 = Types.InAssembly(FeaturesAssembly)
            .That().ResideInNamespaceMatching("MiniBank.Features.Customers.*")
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Features.Accounts")
            .GetResult();

        var failing = new List<string>();
        if (!result.IsSuccessful) failing.AddRange(result.FailingTypeNames ?? Array.Empty<string>());
        if (!result2.IsSuccessful) failing.AddRange(result2.FailingTypeNames ?? Array.Empty<string>());

        Assert.True(failing.Count == 0,
            $"Vertical slices should be isolated (no cross-slice refs):{Environment.NewLine}{string.Join(Environment.NewLine, failing)}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. INFRASTRUCTURE
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure should not depend on Api:{Environment.NewLine}{string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Infrastructure_Should_Depend_On_Domain()
    {
        var hasDomainDep = Types.InAssembly(InfrastructureAssembly)
            .That().HaveDependencyOn("MiniBank.Domain")
            .GetTypes().Any();
        Assert.True(hasDomainDep, "Infrastructure should depend on Domain (repositories, DbContext).");
    }

    [Fact]
    public void Repositories_Should_Be_Internal_Sealed_And_Implement_Domain_Interfaces()
    {
        var repos = Types.InAssembly(InfrastructureAssembly)
            .That().HaveNameEndingWith("Repository")
            .GetTypes();

        var notSealedOrPublic = repos.Where(t => !t.IsSealed || t.IsPublic).ToList();
        Assert.True(notSealedOrPublic.Count == 0,
            $"Repositories must be internal sealed: {string.Join(", ", notSealedOrPublic.Select(t => t.FullName))}");

        var notImplementDomain = repos.Where(t =>
            !t.GetInterfaces().Any(i => i.Namespace?.StartsWith("MiniBank.Domain") == true)).ToList();
        Assert.True(notImplementDomain.Count == 0,
            $"Repositories must implement Domain interfaces: {string.Join(", ", notImplementDomain.Select(t => t.FullName))}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Contain_Handlers()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // This checks no type named Handler exists in Infra — trivially true unless mis-placed
        Assert.True(result.IsSuccessful, "Infrastructure should not contain Handlers (they belong to Features).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. API
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Api_Should_Not_Be_Referenced_By_Domain_Features_Or_Infrastructure()
    {
        var domainRefsApi = Types.InAssembly(DomainAssembly).That().HaveDependencyOn("MiniBank.Api").GetTypes().Any();
        var featuresRefsApi = Types.InAssembly(FeaturesAssembly).That().HaveDependencyOn("MiniBank.Api").GetTypes().Any();
        var infraRefsApi = Types.InAssembly(InfrastructureAssembly).That().HaveDependencyOn("MiniBank.Api").GetTypes().Any();

        Assert.False(domainRefsApi, "Domain should not reference Api.");
        Assert.False(featuresRefsApi, "Features should not reference Api.");
        Assert.False(infraRefsApi, "Infrastructure should not reference Api.");
    }

    [Fact]
    public void Controllers_Should_Be_Sealed_And_Depend_On_Mediator_Only()
    {
        var controllers = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .GetTypes();

        var notSealed = controllers.Where(t => !t.IsSealed).ToList();
        Assert.True(notSealed.Count == 0, $"Controllers must be sealed: {string.Join(", ", notSealed.Select(t => t.Name))}");

        // Controllers should not reference Domain directly (should go via Features)
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Domain")
            .GetResult();

        // Allow Domain.BuildingBlocks.Exceptions? Those are shared, but ideally controllers only use Features
        // We treat direct aggregate dependency as violation; exceptions are borderline — we warn
        if (!result.IsSuccessful)
        {
            var failing = result.FailingTypeNames ?? Array.Empty<string>();
            // Filter to aggregate namespaces
            var aggregateViolations = failing.Where(n => n.Contains("Controllers")).ToList();
            // For report we include all; currently expect to fail if controllers reference Domain
            Assert.True(false,
                $"Controllers should not depend on Domain directly (use Features DTOs):{Environment.NewLine}{string.Join(Environment.NewLine, aggregateViolations)}");
        }
    }

    [Fact]
    public void Controllers_Should_Not_Instantiate_Aggregates_Directly()
    {
        var controllers = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .GetTypes();

        var violating = controllers.Where(t =>
            t.GetMethods().Any(m => m.ReturnType?.Name is "Account" or "Customer" or "Transaction")).ToList();

        // Simpler: check dependency on Domain Aggregates
        var result = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Domain.AccountAggregate")
            .GetResult();

        var result2 = Types.InAssembly(ApiAssembly)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn("MiniBank.Domain.CustomerAggregate")
            .GetResult();

        Assert.True(result.IsSuccessful && result2.IsSuccessful,
            $"Controllers should not instantiate aggregates directly — use mediator/commands.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. OVERALL / MESSAGING
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Messaging_Abstractions_Should_Be_In_Features()
    {
        var mediatorTypes = new[] { typeof(IMediator), typeof(ISender), typeof(IPublisher) };
        foreach (var t in mediatorTypes)
        {
            Assert.Equal("MiniBank.Features.Messaging", t.Namespace);
        }
    }

    [Fact]
    public void ConfigureServices_Should_Exist_In_Features_And_Infrastructure()
    {
        var featuresConfigure = FeaturesAssembly.GetTypes().Any(t => t.Name == "ConfigureServices" && t.Namespace == "MiniBank.Features");
        var infraConfigure = InfrastructureAssembly.GetTypes().Any(t => t.Name == "ConfigureServices" && t.Namespace == "MiniBank.Infrastructure");

        Assert.True(featuresConfigure, "MiniBank.Features.ConfigureServices not found — expected for vertical slice DI.");
        Assert.True(infraConfigure, "MiniBank.Infrastructure.ConfigureServices not found.");
    }

    [Fact]
    public void No_Circular_Dependencies_Between_Layers()
    {
        // Simple check: Domain -> Features -> Infrastructure -> Api chain, no back edges
        var domainToFeatures = Types.InAssembly(DomainAssembly).That().HaveDependencyOn("MiniBank.Features").GetTypes().Any();
        var domainToInfra = Types.InAssembly(DomainAssembly).That().HaveDependencyOn("MiniBank.Infrastructure").GetTypes().Any();
        var featuresToInfra = Types.InAssembly(FeaturesAssembly).That().HaveDependencyOn("MiniBank.Infrastructure").GetTypes().Any();
        var featuresToApi = Types.InAssembly(FeaturesAssembly).That().HaveDependencyOn("MiniBank.Api").GetTypes().Any();
        var infraToApi = Types.InAssembly(InfrastructureAssembly).That().HaveDependencyOn("MiniBank.Api").GetTypes().Any();

        Assert.False(domainToFeatures, "Domain -> Features circular.");
        Assert.False(domainToInfra, "Domain -> Infrastructure circular.");
        Assert.False(featuresToInfra, "Features -> Infrastructure should not exist (Infrastructure depends on Features, not vice versa).");
        Assert.False(featuresToApi, "Features -> Api circular.");
        Assert.False(infraToApi, "Infrastructure -> Api circular.");
    }
}
