using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;
using MediatR;

namespace Clercq.It.Application.Features.Projects.Commands;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
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
            project.ChangeImage(imageUrl);
        }
        else if (request.RemoveImage)
        {
            // A new image wins over a removal; without either the current image stays.
            project.ChangeImage(string.Empty);
        }

        // Update other properties
        project.Update(
            request.Title,
            request.Company,
            request.ShortDescription,
            request.LongDescription,
            request.StartDate,
            request.EndDate,
            request.Featured,
            new Skills(request.Skills));

        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id,
            project.StartDate,
            project.EndDate,
            project.ShortDescription,
            project.LongDescription,
            project.Image,
            project.Featured,
            project.Title,
            project.Skills.Values,
            project.Company
        );
    }
}
