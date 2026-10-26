using Clercq.It.Jobs.Jobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Clercq.It.Jobs.Tests;

public class JobsHostTests
{
    [Fact(DisplayName = "The host knows every job and can build each one with its services")]
    public void Create_RegistersEveryJob()
    {
        // Arrange: no real database or MyZen is needed to build the jobs.
        using var host = JobsHost.Create(
        [
            "--ConnectionStrings:DefaultConnection=Host=localhost;Database=none",
            "--MyZen:SessionKey=test-session-key",
        ]);
        using var scope = host.Services.CreateScope();

        // Act
        var jobs = scope.ServiceProvider.GetServices<IScheduledJob>().ToList();

        // Assert
        jobs.Should().ContainSingle(j => j is MyZenKeepAliveJob);
        jobs.Select(j => j.Name).Should().OnlyHaveUniqueItems();
        host.Services.GetService<JobRunner>().Should().NotBeNull();
    }

    [Fact(DisplayName = "Job names are lowercase kebab-case, as Terraform and the command line expect")]
    public void Create_JobNamesAreKebabCase()
    {
        using var host = JobsHost.Create(
        [
            "--ConnectionStrings:DefaultConnection=Host=localhost;Database=none",
            "--MyZen:SessionKey=test-session-key",
        ]);
        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetServices<IScheduledJob>().Select(j => j.Name)
            .Should().OnlyContain(name => System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z0-9]+(-[a-z0-9]+)*$"));
    }
}
