using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class DeleteCertificationCommandHandler : IRequestHandler<DeleteCertificationCommand, bool>
{
    private readonly ICertificationRepository _certificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCertificationCommandHandler(
        ICertificationRepository certificationRepository,
        IUnitOfWork unitOfWork)
    {
        _certificationRepository = certificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCertificationCommand request, CancellationToken cancellationToken)
    {
        var certification = await _certificationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (certification == null)
        {
            return false;
        }

        await _certificationRepository.DeleteAsync(certification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
