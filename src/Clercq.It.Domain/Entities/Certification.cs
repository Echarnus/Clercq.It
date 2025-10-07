using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Domain.Entities;

public class Certification : IAggregateRoot
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string CredentialId { get; set; } = string.Empty;
    public string CredentialUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
