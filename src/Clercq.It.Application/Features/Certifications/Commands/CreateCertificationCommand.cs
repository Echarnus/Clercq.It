using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Certifications.Commands;

public record CreateCertificationCommand(
    string Title,
    string Issuer,
    DateTime IssueDate,
    DateTime? ExpiryDate,
    string CredentialId,
    string CredentialUrl,
    string Description,
    Stream ImageStream,
    string ImageFileName,
    string ImageContentType
) : IRequest<CertificationDto>;
