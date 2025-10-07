using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Projects.Queries;

public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto?>;
