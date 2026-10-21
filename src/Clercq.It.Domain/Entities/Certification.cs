using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Domain.Entities;

public class Certification : IAggregateRoot
{
    // For EF Core, which sets the private setters when it loads a row.
    private Certification() { }

    public Certification(Guid id, string title, string issuer, DateTime issueDate, DateTime? expiryDate,
        string credentialId, string credentialUrl, string description, string image)
    {
        Id = id;
        Update(title, issuer, issueDate, expiryDate, credentialId, credentialUrl, description);
        ChangeImage(image);
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string CredentialId { get; private set; } = string.Empty;
    public string CredentialUrl { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Image { get; private set; } = string.Empty;

    public static Certification Create(string title, string issuer, DateTime issueDate, DateTime? expiryDate,
        string credentialId, string credentialUrl, string description, string image) =>
        new(Guid.NewGuid(), title, issuer, issueDate, expiryDate, credentialId, credentialUrl, description, image);

    /// <exception cref="ArgumentException">The title or issuer is blank, or it expires before it was issued.</exception>
    public void Update(string title, string issuer, DateTime issueDate, DateTime? expiryDate, string credentialId,
        string credentialUrl, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentNullException.ThrowIfNull(credentialId);
        ArgumentNullException.ThrowIfNull(credentialUrl);
        ArgumentNullException.ThrowIfNull(description);
        if (expiryDate < issueDate)
        {
            throw new ArgumentException("A certification cannot expire before it was issued.", nameof(expiryDate));
        }

        Title = title;
        Issuer = issuer;
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        CredentialId = credentialId;
        CredentialUrl = credentialUrl;
        Description = description;
    }

    public void ChangeImage(string image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
    }
}
