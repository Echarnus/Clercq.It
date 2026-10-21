using System.Reflection;
using System.Runtime.CompilerServices;
using Clercq.It.Domain.Abstractions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NetArchTest.Rules;

namespace Clercq.It.Architecture.Tests;

/// <summary>
/// DDD and naming rules. See docs/architecture.md#architecture-rules.
/// </summary>
public class DomainModelTests
{
    /// <summary>
    /// Aggregates that still expose public setters. EF Core, the admin handlers and
    /// the content import set their properties directly; moving them to constructors
    /// and behaviour methods is tracked in #277. New aggregates must not join this list.
    /// </summary>
    private static readonly string[] AggregatesWithPublicSetters =
    [
        "Clercq.It.Domain.Entities.Blog",
        "Clercq.It.Domain.Entities.Certification",
        "Clercq.It.Domain.Entities.Project",
    ];

    [Fact(DisplayName = "Aggregates live in Domain.Entities")]
    public void Aggregates_LiveInDomainEntities()
    {
        var result = Types.InAssemblies([Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api])
            .That().ImplementInterface(typeof(IAggregateRoot)).And().AreClasses()
            .Should().ResideInNamespace("Clercq.It.Domain.Entities")
            .GetResult();

        result.ShouldPass();
    }

    [Fact(DisplayName = "New aggregates have no public setters")]
    public void Aggregates_HaveNoPublicSetters()
    {
        var offenders = Layers.Domain.GetTypes()
            .Where(t => t.IsClass && typeof(IAggregateRoot).IsAssignableFrom(t))
            .Where(t => !AggregatesWithPublicSetters.Contains(t.FullName))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true } && !IsInitOnly(p))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        offenders.Should().BeEmpty("aggregates change state through constructors and methods, not public setters");
    }

    [Fact(DisplayName = "The public-setter allow-list only names existing aggregates")]
    public void AllowList_IsCurrent()
    {
        var aggregates = Layers.Domain.GetTypes()
            .Where(t => t.IsClass && typeof(IAggregateRoot).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        aggregates.Should().Contain(AggregatesWithPublicSetters, "remove an aggregate from the list once it is encapsulated or deleted");
    }

    [Fact(DisplayName = "Value objects are immutable records or sealed types")]
    public void ValueObjects_AreImmutable()
    {
        var types = Layers.Domain.GetTypes()
            .Where(t => t.Namespace == "Clercq.It.Domain.ValueObjects" && !IsCompilerGenerated(t))
            .ToList();

        types.Should().NotBeEmpty();
        foreach (var type in types)
        {
            (IsRecord(type) || type.IsSealed || type.IsValueType).Should().BeTrue($"{type.Name} must be a record or sealed");
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true } && !IsInitOnly(p))
                .Should().BeEmpty($"{type.Name} must not have public setters (init is fine)");
        }
    }

    [Fact(DisplayName = "Repository contracts are interfaces in Domain, implemented in Infrastructure")]
    public void Repositories_AreDomainInterfacesWithInfrastructureImplementations()
    {
        Types.InAssembly(Layers.Domain)
            .That().HaveNameEndingWith("Repository")
            .Should().BeInterfaces()
            .GetResult().ShouldPass();

        Types.InAssemblies([Layers.Application, Layers.Api])
            .That().HaveNameEndingWith("Repository").And().AreClasses()
            .GetTypes()
            .Should().BeEmpty("repository implementations belong in Infrastructure");

        var contracts = Layers.Domain.GetTypes().Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal));
        foreach (var contract in contracts)
        {
            Layers.Infrastructure.GetTypes()
                .Should().Contain(t => t.IsClass && !t.IsAbstract && contract.IsAssignableFrom(t),
                    $"{contract.Name} needs an implementation in Infrastructure");
        }
    }

    [Fact(DisplayName = "MediatR requests are named *Command or *Query and live in Application")]
    public void Requests_FollowNaming()
    {
        var requests = AllTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(IsMediatRRequest)).ToList();

        requests.Should().NotBeEmpty();
        requests.Should().OnlyContain(t => t.Assembly == Layers.Application, "requests belong in Application");
        requests.Select(t => t.Name).Should().OnlyContain(n => n.EndsWith("Command", StringComparison.Ordinal) || n.EndsWith("Query", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Request handlers are named *Handler and live in Application")]
    public void Handlers_FollowNaming()
    {
        var handlers = AllTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .ToList();

        handlers.Should().NotBeEmpty();
        handlers.Should().OnlyContain(t => t.Assembly == Layers.Application, "handlers belong in Application");
        handlers.Select(t => t.Name).Should().OnlyContain(n => n.EndsWith("Handler", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Validators are named *Validator and live in Application")]
    public void Validators_FollowNaming()
    {
        var validators = AllTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IValidator).IsAssignableFrom(t))
            .ToList();

        validators.Should().NotBeEmpty();
        validators.Should().OnlyContain(t => t.Assembly == Layers.Application, "validators belong in Application");
        validators.Select(t => t.Name).Should().OnlyContain(n => n.EndsWith("Validator", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> AllTypes() =>
        new[] { Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api }
            .SelectMany(a => a.GetTypes())
            .Where(t => !IsCompilerGenerated(t));

    private static bool IsMediatRRequest(Type i) =>
        i == typeof(IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)) == true;

    private static bool IsRecord(Type type) => type.GetMethod("<Clone>$") is not null || type.IsValueType;

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), false) || type.Name.Contains('<', StringComparison.Ordinal);
}
