using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Projects.Queries;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (project == null)
        {
            return null;
        }
        
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
