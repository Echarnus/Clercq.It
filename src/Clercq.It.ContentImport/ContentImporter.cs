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
            var image = await ImageAsync(contentRoot, file, cancellationToken);
            var desired = Build(file, () => new Project(
                StableId.For("project", file.Slug),
                file.RequiredString("title"),
                file.RequiredString("shortDescription"),
                BodyOr(file, file.RequiredString("shortDescription")),
                image,
                file.RequiredDate("startDate"),
                file.OptionalDate("endDate") ?? file.RequiredDate("startDate"),
                file.OptionalBool("featured"),
                new Skills(file.StringList("skills"))));
            await UpsertAsync(db.Projects, desired, Copy, counts, file, cancellationToken);
        }

        foreach (var file in blogs)
        {
            var created = file.RequiredDate("createdDate");
            var image = await ImageAsync(contentRoot, file, cancellationToken);
            var desired = Build(file, () => new Blog(
                StableId.For("blog", file.Slug),
                created,
                file.OptionalDate("publishDate") ?? created,
                file.RequiredString("shortDescription"),
                BodyOr(file, file.RequiredString("shortDescription")),
                image,
                new Tags(file.StringList("tags"))));
            await UpsertAsync(db.Blogs, desired, Copy, counts, file, cancellationToken);
        }

        foreach (var file in certifications)
        {
            var image = await ImageAsync(contentRoot, file, cancellationToken);
            var desired = Build(file, () => new Certification(
                StableId.For("certification", file.Slug),
                file.RequiredString("title"),
                file.RequiredString("issuer"),
                file.RequiredDate("issueDate"),
                file.OptionalDate("expiryDate"),
                file.OptionalString("credentialId") ?? string.Empty,
                file.OptionalString("credentialUrl") ?? string.Empty,
                file.Body,
                image));
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

    // The aggregates enforce their own invariants; report a violation against the file it came from.
    private static T Build<T>(ContentFile file, Func<T> create)
    {
        try
        {
            return create();
        }
        catch (ArgumentException ex)
        {
            throw new ContentException(file.Path, ex.Message);
        }
    }

    // Each Copy updates the existing row only when the file differs, and returns whether it did.
    private static bool Copy(Project from, Project to)
    {
        var changed = false;
        if (from.Title != to.Title || from.ShortDescription != to.ShortDescription
            || from.LongDescription != to.LongDescription || from.StartDate != to.StartDate
            || from.EndDate != to.EndDate || from.Featured != to.Featured
            || !from.Skills.Values.SequenceEqual(to.Skills.Values))
        {
            to.Update(from.Title, from.ShortDescription, from.LongDescription, from.StartDate, from.EndDate,
                from.Featured, from.Skills);
            changed = true;
        }
        return ChangeImage(from.Image, to.Image, to.ChangeImage) || changed;
    }

    private static bool Copy(Blog from, Blog to)
    {
        var changed = false;
        if (from.ShortDescription != to.ShortDescription || from.LongDescription != to.LongDescription
            || !from.Tags.Values.SequenceEqual(to.Tags.Values))
        {
            to.Update(from.ShortDescription, from.LongDescription, from.Tags);
            changed = true;
        }
        if (from.CreatedDate != to.CreatedDate || from.PublishDate != to.PublishDate)
        {
            to.Reschedule(from.CreatedDate, from.PublishDate);
            changed = true;
        }
        return ChangeImage(from.Image, to.Image, to.ChangeImage) || changed;
    }

    private static bool Copy(Certification from, Certification to)
    {
        var changed = false;
        if (from.Title != to.Title || from.Issuer != to.Issuer || from.IssueDate != to.IssueDate
            || from.ExpiryDate != to.ExpiryDate || from.CredentialId != to.CredentialId
            || from.CredentialUrl != to.CredentialUrl || from.Description != to.Description)
        {
            to.Update(from.Title, from.Issuer, from.IssueDate, from.ExpiryDate, from.CredentialId, from.CredentialUrl,
                from.Description);
            changed = true;
        }
        return ChangeImage(from.Image, to.Image, to.ChangeImage) || changed;
    }

    private static bool ChangeImage(string desired, string current, Action<string> change)
    {
        if (desired == current) return false;
        change(desired);
        return true;
    }

    private sealed class Counts
    {
        public int Created;
        public int Updated;
        public int Unchanged;
    }
}
