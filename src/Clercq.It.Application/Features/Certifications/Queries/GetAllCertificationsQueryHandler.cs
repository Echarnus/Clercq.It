using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Certifications.Queries;

public class GetAllCertificationsQueryHandler : IRequestHandler<GetAllCertificationsQuery, IEnumerable<CertificationDto>>
{
    private readonly ICertificationRepository _certificationRepository;

    public GetAllCertificationsQueryHandler(ICertificationRepository certificationRepository)
    {
        _certificationRepository = certificationRepository;
    }

    public async Task<IEnumerable<CertificationDto>> Handle(GetAllCertificationsQuery request, CancellationToken cancellationToken)
    {
        var certifications = await _certificationRepository.GetAllAsync(cancellationToken);
        
        return certifications.Select(c => new CertificationDto(
            c.Id,
            c.Title,
            c.Issuer,
            c.IssueDate,
            c.ExpiryDate,
            c.CredentialId,
            c.CredentialUrl,
            c.Description,
            c.Image
        ));
    }
}
