using Clercq.It.ContentImport;
using Clercq.It.Domain.Entities;
using Clercq.It.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clercq.It.ContentImport.Tests;

public sealed class ContentImporterTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("content-").FullName;
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly FakeImageStore _images = new();

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private ApplicationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_dbName).Options);

    private async Task<ImportResult> ImportAsync()
    {
        await using var db = NewDb();
        return await new ContentImporter(db, _images, NullLogger<ContentImporter>.Instance).ImportAsync(_root);
    }

    private void Write(string relativePath, string text)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private const string Project = """
        ---
        title: "Clercq.It"
        shortDescription: "My personal website"
        startDate: 2025-01-01
        endDate: 2025-06-30
        featured: true
        image: images/clercqit.png
        skills:
          - .NET
          - Next.js
        ---
        Long **Markdown** description.
        """;

    [Fact(DisplayName = "A new project file is created with a stable id and its fields")]
    public async Task Import_NewProject_CreatesRow()
    {
        Write("projects/clercqit.md", Project);
        Write("images/clercqit.png", "png-bytes");

        var result = await ImportAsync();

        result.Should().Be(new ImportResult(1, 0, 0));
        await using var db = NewDb();
        var project = await db.Projects.SingleAsync();
        project.Id.Should().Be(StableId.For("project", "clercqit"));
        project.Title.Should().Be("Clercq.It");
        project.Featured.Should().BeTrue();
        project.LongDescription.Should().Be("Long **Markdown** description.");
        project.StartDate.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        project.Skills.Values.Should().Equal(".NET", "Next.js");
        project.Image.Should().Be("https://images.test/clercqit.png");
    }

    [Fact(DisplayName = "Running the import twice changes nothing the second time")]
    public async Task Import_Twice_IsIdempotent()
    {
        Write("projects/clercqit.md", Project);
        Write("images/clercqit.png", "png-bytes");
        Write("blogs/hello.md", "---\nshortDescription: Hello\ncreatedDate: 2025-02-01\ntags: [intro]\n---\nBody");
        Write("certifications/az-204.md", "---\ntitle: AZ-204\nissuer: Microsoft\nissueDate: 2024-05-01\n---\n");

        (await ImportAsync()).Should().Be(new ImportResult(3, 0, 0));
        (await ImportAsync()).Should().Be(new ImportResult(0, 0, 3));
    }

    [Fact(DisplayName = "Changing a file updates the existing row")]
    public async Task Import_ChangedFile_UpdatesRow()
    {
        Write("certifications/az-204.md", "---\ntitle: AZ-204\nissuer: Microsoft\nissueDate: 2024-05-01\n---\n");
        await ImportAsync();

        Write("certifications/az-204.md", "---\ntitle: AZ-204 Developer\nissuer: Microsoft\nissueDate: 2024-05-01\n---\n");
        var result = await ImportAsync();

        result.Should().Be(new ImportResult(0, 1, 0));
        await using var db = NewDb();
        (await db.Certifications.SingleAsync()).Title.Should().Be("AZ-204 Developer");
    }

    [Fact(DisplayName = "Rows created in the admin UI are left alone")]
    public async Task Import_LeavesAdminRowsAlone()
    {
        var adminId = Guid.NewGuid();
        await using (var db = NewDb())
        {
            db.Projects.Add(new Project { Id = adminId, Title = "From admin", ShortDescription = "x", LongDescription = "x", Image = "x" });
            await db.SaveChangesAsync();
        }
        Write("projects/clercqit.md", Project);
        Write("images/clercqit.png", "png-bytes");

        await ImportAsync();

        await using var check = NewDb();
        (await check.Projects.CountAsync()).Should().Be(2);
        (await check.Projects.FindAsync(adminId))!.Title.Should().Be("From admin");
    }

    [Fact(DisplayName = "An invalid file stops the import before anything is written")]
    public async Task Import_InvalidFile_WritesNothing()
    {
        Write("certifications/az-204.md", "---\ntitle: AZ-204\nissuer: Microsoft\nissueDate: 2024-05-01\n---\n");
        Write("projects/broken.md", "---\ntitle: Missing fields\n---\n");

        var act = ImportAsync;

        await act.Should().ThrowAsync<ContentException>().WithMessage("*projects/broken.md*shortDescription*");
        await using var db = NewDb();
        (await db.Certifications.CountAsync()).Should().Be(0);
    }

    [Theory(DisplayName = "Absolute URLs and site paths are used as they are")]
    [InlineData("/placeholder.svg")]
    [InlineData("https://example.com/a.png")]
    public async Task Import_ExternalImage_NotUploaded(string image)
    {
        Write("certifications/c.md", $"---\ntitle: C\nissuer: I\nissueDate: 2024-01-01\nimage: \"{image}\"\n---\n");

        await ImportAsync();

        _images.Uploads.Should().BeEmpty();
        await using var db = NewDb();
        (await db.Certifications.SingleAsync()).Image.Should().Be(image);
    }

    [Fact(DisplayName = "An image path outside the content folder is rejected")]
    public async Task Import_ImageOutsideRoot_Throws()
    {
        Write("certifications/c.md", "---\ntitle: C\nissuer: I\nissueDate: 2024-01-01\nimage: ../secret.png\n---\n");

        var act = ImportAsync;

        await act.Should().ThrowAsync<ContentException>().WithMessage("*outside the content folder*");
    }

    [Fact(DisplayName = "The same slug always gives the same id")]
    public void StableId_IsDeterministic()
    {
        StableId.For("project", "clercqit").Should().Be(StableId.For("project", "ClercqIt"));
        StableId.For("project", "clercqit").Should().NotBe(StableId.For("blog", "clercqit"));
    }

    private sealed class FakeImageStore : IContentImageStore
    {
        public List<string> Uploads { get; } = [];

        public Task<string> UploadAsync(string localPath, CancellationToken cancellationToken)
        {
            Uploads.Add(localPath);
            return Task.FromResult($"https://images.test/{Path.GetFileName(localPath)}");
        }
    }
}
