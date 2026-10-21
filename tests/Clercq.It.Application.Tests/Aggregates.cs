using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Tests;

/// <summary>
/// Builds valid aggregates for tests. Pass only the values a test cares about.
/// </summary>
internal static class Aggregates
{
    private static readonly DateTime DefaultDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Blog Blog(
        Guid? id = null,
        DateTime? createdDate = null,
        DateTime? publishDate = null,
        string shortDescription = "Short description",
        string longDescription = "Long description",
        string image = "https://example.com/blog.jpg",
        Tags? tags = null) =>
        new(id ?? Guid.NewGuid(), createdDate ?? DefaultDate, publishDate ?? createdDate ?? DefaultDate,
            shortDescription, longDescription, image, tags ?? new Tags([]));

    public static Project Project(
        Guid? id = null,
        string title = "Project",
        string shortDescription = "Short description",
        string longDescription = "Long description",
        string image = "",
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool featured = false,
        Skills? skills = null) =>
        new(id ?? Guid.NewGuid(), title, shortDescription, longDescription, image, startDate ?? DefaultDate,
            endDate ?? startDate ?? DefaultDate, featured, skills ?? new Skills([]));

    public static Certification Certification(
        Guid? id = null,
        string title = "Certification",
        string issuer = "Issuer",
        DateTime? issueDate = null,
        DateTime? expiryDate = null,
        string credentialId = "",
        string credentialUrl = "",
        string description = "",
        string image = "https://example.com/cert.jpg") =>
        new(id ?? Guid.NewGuid(), title, issuer, issueDate ?? DefaultDate, expiryDate, credentialId, credentialUrl,
            description, image);
}
