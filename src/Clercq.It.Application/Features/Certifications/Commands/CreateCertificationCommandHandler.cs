using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using MediatR;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class CreateCertificationCommandHandler : IRequestHandler<CreateCertificationCommand, CertificationDto>
{
    private readonly ICertificationRepository _certificationRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCertificationCommandHandler(
        ICertificationRepository certificationRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _certificationRepository = certificationRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CertificationDto> Handle(CreateCertificationCommand request, CancellationToken cancellationToken)
    {
        // Upload image to object storage
        var imageUrl = await _objectStorageService.UploadFileAsync(
            request.ImageFileName,
            request.ImageStream,
            request.ImageContentType,
            isInlineImage: false,
            cancellationToken);

        // Create certification entity
        var certification = Certification.Create(
            request.Title,
            request.Issuer,
            request.IssueDate,
            request.ExpiryDate,
            request.CredentialId,
            request.CredentialUrl,
            request.Description,
            imageUrl);

        // Add to repository
        await _certificationRepository.AddAsync(certification, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return DTO
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
