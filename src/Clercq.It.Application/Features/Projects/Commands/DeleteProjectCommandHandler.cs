using MediatR;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Projects.Commands;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return false;
        }

        await _projectRepository.DeleteAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
