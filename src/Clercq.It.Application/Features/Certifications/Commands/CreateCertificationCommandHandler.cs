using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;

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
        var certification = new Certification
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Issuer = request.Issuer,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            CredentialId = request.CredentialId,
            CredentialUrl = request.CredentialUrl,
            Description = request.Description,
            Image = imageUrl
        };

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
