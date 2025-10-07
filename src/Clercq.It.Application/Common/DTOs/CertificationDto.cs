namespace Clercq.It.Application.Common.DTOs;

public record CertificationDto(
    Guid Id,
    string Title,
    string Issuer,
    DateTime IssueDate,
    DateTime? ExpiryDate,
    string CredentialId,
    string CredentialUrl,
    string Description,
    string Image
);
