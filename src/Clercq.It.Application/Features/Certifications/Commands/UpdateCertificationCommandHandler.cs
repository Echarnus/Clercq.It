using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using MediatR;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class UpdateCertificationCommandHandler : IRequestHandler<UpdateCertificationCommand, CertificationDto?>
{
    private readonly ICertificationRepository _certificationRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCertificationCommandHandler(
        ICertificationRepository certificationRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _certificationRepository = certificationRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CertificationDto?> Handle(UpdateCertificationCommand request, CancellationToken cancellationToken)
    {
        var certification = await _certificationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (certification == null)
        {
            return null;
        }

        // Update image if provided, or remove it when asked
        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName) && !string.IsNullOrEmpty(request.ImageContentType))
        {
            var imageUrl = await _objectStorageService.UploadFileAsync(
                request.ImageFileName,
                request.ImageStream,
                request.ImageContentType,
                isInlineImage: false,
                cancellationToken);
            certification.ChangeImage(imageUrl);
        }
        else if (request.RemoveImage)
        {
            // A new image wins over a removal; without either the current image stays.
            certification.ChangeImage(string.Empty);
        }

        // Update other properties
        certification.Update(
            request.Title,
            request.Issuer,
            request.IssueDate,
            request.ExpiryDate,
            request.CredentialId,
            request.CredentialUrl,
            request.Description);

        await _certificationRepository.UpdateAsync(certification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
