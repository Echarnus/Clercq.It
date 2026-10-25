using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using NetArchTest.Rules;

namespace Clercq.It.Architecture.Tests;

/// <summary>
/// Clean Architecture layering: Domain ← Application ← Infrastructure ← Api.
/// See docs/architecture.md#architecture-rules.
/// </summary>
public class LayeringTests
{
    private const string Domain = "Clercq.It.Domain";
    private const string Application = "Clercq.It.Application";
    private const string Infrastructure = "Clercq.It.Infrastructure";
    private const string Api = "Clercq.It.Api";

    [Fact(DisplayName = "Domain depends on no other layer and no framework")]
    public void Domain_HasNoOutwardDependencies()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                Application, Infrastructure, Api,
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql",
                "MediatR", "FluentValidation", "Amazon")
            .GetResult();

        result.ShouldPass();
    }

    [Fact(DisplayName = "Application depends only on Domain, MediatR and FluentValidation")]
    public void Application_DependsOnlyOnDomain()
    {
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                Infrastructure, Api,
                "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "Amazon")
            .GetResult();

        result.ShouldPass();
    }

    [Fact(DisplayName = "Infrastructure does not depend on the Api")]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(Api)
            .GetResult();

        result.ShouldPass();
    }

    [Fact(DisplayName = "Api endpoints go through Application, not the DbContext")]
    public void ApiEndpoints_DoNotUseDbContext()
    {
        // Program.cs is the composition root and may register the DbContext;
        // the endpoint modules under Features may not touch it.
        var result = Types.InAssembly(Layers.Api)
            .That().ResideInNamespace($"{Api}.Features")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", $"{Infrastructure}.Data", $"{Infrastructure}.Repositories")
            .GetResult();

        result.ShouldPass();
    }

    [Fact(DisplayName = "Only composition roots reference the Infrastructure project")]
    public void Infrastructure_IsReferencedOnlyByCompositionRoots()
    {
        // Composition roots wire Infrastructure into a running process:
        // the API, the EF migrations tool, the content import tool and the scheduled jobs.
        string[] allowed = ["ClercqIt.Api", "Clercq.It.Infrastructure.EF.Migrations", "Clercq.It.ContentImport", "Clercq.It.Jobs"];

        var src = Path.Combine(RepoRoot(), "src");
        var referencing = Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(csproj => XDocument.Load(csproj)
                .Descendants("ProjectReference")
                .Any(r => ((string?)r.Attribute("Include") ?? "").Replace('\\', '/')
                    .EndsWith($"/{Infrastructure}.csproj", StringComparison.Ordinal)))
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        referencing.Should().BeSubsetOf(allowed,
            "Infrastructure is wired in only at composition roots; other code depends on Domain/Application abstractions");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Clercq.It.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}

internal static class Layers
{
    public static readonly Assembly Domain = typeof(Domain.Abstractions.IAggregateRoot).Assembly;
    public static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    public static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}

internal static class TestResultExtensions
{
    public static void ShouldPass(this TestResult result) =>
        result.IsSuccessful.Should().BeTrue(
            "these types break the rule: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
}
