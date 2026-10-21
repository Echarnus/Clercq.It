using Clercq.It.Application.Common.DTOs;
using MediatR;

namespace Clercq.It.Application.Features.Projects.Queries;

public record GetFeaturedProjectsQuery : IRequest<IEnumerable<ProjectDto>>;
