using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Projects.Commands;

public record CreateProjectCommand(
    string Title,
    string ShortDescription,
    string LongDescription,
    string ImageUrl,
    DateTime StartDate,
    DateTime EndDate,
    bool Featured,
    string[] Skills
) : IRequest<ProjectDto>;
