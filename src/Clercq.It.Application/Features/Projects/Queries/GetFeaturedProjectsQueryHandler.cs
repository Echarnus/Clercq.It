using MediatR;
using Clercq.It.Application.Common.DTOs;
using Clercq.It.Domain.Abstractions;

namespace Clercq.It.Application.Features.Projects.Queries;

public class GetFeaturedProjectsQueryHandler : IRequestHandler<GetFeaturedProjectsQuery, IEnumerable<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public GetFeaturedProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<IEnumerable<ProjectDto>> Handle(GetFeaturedProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetFeaturedAsync(cancellationToken);
        
        return projects.Select(p => new ProjectDto(
            p.Id,
            p.StartDate,
            p.EndDate,
            p.ShortDescription,
            p.LongDescription,
            p.Image,
            p.Featured,
            p.Title,
            p.Skills.Values
        ));
    }
}