using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.Entities;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Application.Features.Projects.Commands;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // Upload image to object storage if provided
        string imageUrl = string.Empty;
        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName) && !string.IsNullOrEmpty(request.ImageContentType))
        {
            imageUrl = await _objectStorageService.UploadFileAsync(
                request.ImageFileName,
                request.ImageStream,
                request.ImageContentType,
                isInlineImage: false,
                cancellationToken);
        }

        // Create project entity
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ShortDescription = request.ShortDescription,
            LongDescription = request.LongDescription,
            Image = imageUrl,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Featured = request.Featured,
            Skills = new Skills(request.Skills)
        };

        // Add to repository
        await _projectRepository.AddAsync(project, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return DTO
        return new ProjectDto(
            project.Id,
            project.StartDate,
            project.EndDate,
            project.ShortDescription,
            project.LongDescription,
            project.Image,
            project.Featured,
            project.Title,
            project.Skills.Values
        );
    }
}
