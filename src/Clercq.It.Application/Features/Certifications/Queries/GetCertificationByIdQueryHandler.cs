using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Certifications.Queries;

public class GetCertificationByIdQueryHandler : IRequestHandler<GetCertificationByIdQuery, CertificationDto?>
{
    private readonly ICertificationRepository _certificationRepository;

    public GetCertificationByIdQueryHandler(ICertificationRepository certificationRepository)
    {
        _certificationRepository = certificationRepository;
    }

    public async Task<CertificationDto?> Handle(GetCertificationByIdQuery request, CancellationToken cancellationToken)
    {
        var certification = await _certificationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (certification == null)
        {
            return null;
        }

        return new CertificationDto(
            certification.Id,
            certification.Title,
            certification.Issuer,
            certification.IssueDate,
            certification.ExpiryDate,
            certification.CredentialId,
            certification.CredentialUrl,
            certification.Description,
            certification.Image
        );
    }
}
