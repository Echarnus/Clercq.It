using Clercq.It.ContentImport;
using Clercq.It.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clercq.It.ContentImport.Tests;

/// <summary>
/// Imports the real content/ folder, so a content PR with a broken file fails
/// CI instead of the deploy.
/// </summary>
public class RepoContentTests
{
    [Fact(DisplayName = "Every file in content/ imports without errors")]
    public async Task RepoContent_Imports()
    {
        var root = Path.Combine(FindRepoRoot(), "content");
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var result = await new ContentImporter(db, new NoUploadStore(), NullLogger<ContentImporter>.Instance)
            .ImportAsync(root);

        result.Created.Should().BeGreaterThan(0);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Clercq.It.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    private sealed class NoUploadStore : IContentImageStore
    {
        public Task<string> UploadAsync(string localPath, CancellationToken cancellationToken) =>
            Task.FromResult($"https://images.test/{Path.GetFileName(localPath)}");
    }
}
