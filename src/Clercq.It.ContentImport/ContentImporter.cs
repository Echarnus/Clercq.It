using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;
using Clercq.It.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clercq.It.ContentImport;

public sealed record ImportResult(int Created, int Updated, int Unchanged);

/// <summary>
/// Upserts content/projects, content/blogs and content/certifications into the
/// database. Each file maps to a fixed id (<see cref="StableId"/>), so running
/// the import again changes nothing. Rows created in the admin UI are left alone.
/// See docs/content.md for the file format.
/// </summary>
public sealed class ContentImporter(ApplicationDbContext db, IContentImageStore images, ILogger<ContentImporter> logger)
{
    public const string DefaultImage = "/placeholder.svg";

    public async Task<ImportResult> ImportAsync(string contentRoot, CancellationToken cancellationToken = default)
    {
        // Parse everything first, so one bad file stops the import before any write.
        var projects = Read(contentRoot, "projects");
        var blogs = Read(contentRoot, "blogs");
        var certifications = Read(contentRoot, "certifications");

        var counts = new Counts();

        foreach (var file in projects)
        {
            var desired = new Project
            {
                Id = StableId.For("project", file.Slug),
                Title = file.RequiredString("title"),
                ShortDescription = file.RequiredString("shortDescription"),
                LongDescription = BodyOr(file, file.RequiredString("shortDescription")),
                StartDate = file.RequiredDate("startDate"),
                EndDate = file.OptionalDate("endDate") ?? file.RequiredDate("startDate"),
                Featured = file.OptionalBool("featured"),
                Image = await ImageAsync(contentRoot, file, cancellationToken),
                Skills = new Skills(file.StringList("skills"))
            };
            await UpsertAsync(db.Projects, desired, Copy, counts, file, cancellationToken);
        }

        foreach (var file in blogs)
        {
            var created = file.RequiredDate("createdDate");
            var desired = new Blog
            {
                Id = StableId.For("blog", file.Slug),
                ShortDescription = file.RequiredString("shortDescription"),
                LongDescription = BodyOr(file, file.RequiredString("shortDescription")),
                CreatedDate = created,
                PublishDate = file.OptionalDate("publishDate") ?? created,
                Image = await ImageAsync(contentRoot, file, cancellationToken),
                Tags = new Tags(file.StringList("tags"))
            };
            await UpsertAsync(db.Blogs, desired, Copy, counts, file, cancellationToken);
        }

        foreach (var file in certifications)
        {
            var desired = new Certification
            {
                Id = StableId.For("certification", file.Slug),
                Title = file.RequiredString("title"),
                Issuer = file.RequiredString("issuer"),
                IssueDate = file.RequiredDate("issueDate"),
                ExpiryDate = file.OptionalDate("expiryDate"),
                CredentialId = file.OptionalString("credentialId") ?? string.Empty,
                CredentialUrl = file.OptionalString("credentialUrl") ?? string.Empty,
                Description = file.Body,
                Image = await ImageAsync(contentRoot, file, cancellationToken)
            };
            await UpsertAsync(db.Certifications, desired, Copy, counts, file, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var result = new ImportResult(counts.Created, counts.Updated, counts.Unchanged);
        logger.LogInformation("Content import: {Created} created, {Updated} updated, {Unchanged} unchanged",
            result.Created, result.Updated, result.Unchanged);
        return result;
    }

    private static List<ContentFile> Read(string contentRoot, string folder)
    {
        var dir = Path.Combine(contentRoot, folder);
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, "*.md")
            .Order(StringComparer.Ordinal)
            .Select(path => ContentFile.Parse(Path.GetRelativePath(contentRoot, path), File.ReadAllText(path)))
            .ToList();
    }

    private static string BodyOr(ContentFile file, string fallback) =>
        file.Body.Length > 0 ? file.Body : fallback;

    private async Task<string> ImageAsync(string contentRoot, ContentFile file, CancellationToken cancellationToken)
    {
        var image = file.OptionalString("image");
        if (string.IsNullOrEmpty(image)) return DefaultImage;

        // Absolute URLs and site paths (e.g. /placeholder.svg) are used as they are.
        if (image.StartsWith('/') || Uri.TryCreate(image, UriKind.Absolute, out _)) return image;

        var root = Path.GetFullPath(contentRoot);
        var local = Path.GetFullPath(Path.Combine(root, image));
        if (!local.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ContentException(file.Path, $"image '{image}' is outside the content folder");
        }
        if (!File.Exists(local))
        {
            throw new ContentException(file.Path, $"image '{image}' not found (paths are relative to the content folder)");
        }
        return await images.UploadAsync(local, cancellationToken);
    }

    private async Task UpsertAsync<T>(DbSet<T> set, T desired, Func<T, T, bool> copy, Counts counts, ContentFile file,
        CancellationToken cancellationToken) where T : class
    {
        var id = db.Entry(desired).Property<Guid>("Id").CurrentValue;
        var existing = await set.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            set.Add(desired);
            counts.Created++;
            logger.LogInformation("Created {File}", file.Path);
        }
        else if (copy(desired, existing))
        {
            counts.Updated++;
            logger.LogInformation("Updated {File}", file.Path);
        }
        else
        {
            counts.Unchanged++;
        }
    }

    // Each Copy assigns only the fields that differ and returns whether any did.
    private static bool Copy(Project from, Project to)
    {
        var changed = false;
        Set(from.Title, to.Title, v => to.Title = v, ref changed);
        Set(from.ShortDescription, to.ShortDescription, v => to.ShortDescription = v, ref changed);
        Set(from.LongDescription, to.LongDescription, v => to.LongDescription = v, ref changed);
        Set(from.StartDate, to.StartDate, v => to.StartDate = v, ref changed);
        Set(from.EndDate, to.EndDate, v => to.EndDate = v, ref changed);
        Set(from.Featured, to.Featured, v => to.Featured = v, ref changed);
        Set(from.Image, to.Image, v => to.Image = v, ref changed);
        if (!from.Skills.Values.SequenceEqual(to.Skills.Values)) { to.Skills = from.Skills; changed = true; }
        return changed;
    }

    private static bool Copy(Blog from, Blog to)
    {
        var changed = false;
        Set(from.ShortDescription, to.ShortDescription, v => to.ShortDescription = v, ref changed);
        Set(from.LongDescription, to.LongDescription, v => to.LongDescription = v, ref changed);
        Set(from.CreatedDate, to.CreatedDate, v => to.CreatedDate = v, ref changed);
        Set(from.PublishDate, to.PublishDate, v => to.PublishDate = v, ref changed);
        Set(from.Image, to.Image, v => to.Image = v, ref changed);
        if (!from.Tags.Values.SequenceEqual(to.Tags.Values)) { to.Tags = from.Tags; changed = true; }
        return changed;
    }

    private static bool Copy(Certification from, Certification to)
    {
        var changed = false;
        Set(from.Title, to.Title, v => to.Title = v, ref changed);
        Set(from.Issuer, to.Issuer, v => to.Issuer = v, ref changed);
        Set(from.IssueDate, to.IssueDate, v => to.IssueDate = v, ref changed);
        Set(from.ExpiryDate, to.ExpiryDate, v => to.ExpiryDate = v, ref changed);
        Set(from.CredentialId, to.CredentialId, v => to.CredentialId = v, ref changed);
        Set(from.CredentialUrl, to.CredentialUrl, v => to.CredentialUrl = v, ref changed);
        Set(from.Description, to.Description, v => to.Description = v, ref changed);
        Set(from.Image, to.Image, v => to.Image = v, ref changed);
        return changed;
    }

    private static void Set<TValue>(TValue desired, TValue current, Action<TValue> assign, ref bool changed)
    {
        if (EqualityComparer<TValue>.Default.Equals(desired, current)) return;
        assign(desired);
        changed = true;
    }

    private sealed class Counts
    {
        public int Created;
        public int Updated;
        public int Unchanged;
    }
}
