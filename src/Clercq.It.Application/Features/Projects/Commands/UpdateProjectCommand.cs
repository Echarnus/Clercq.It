using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Projects.Commands;

public record UpdateProjectCommand(
    Guid Id,
    string Title,
    string ShortDescription,
    string LongDescription,
    Stream? ImageStream,
    string? ImageFileName,
    string? ImageContentType,
    DateTime StartDate,
    DateTime EndDate,
    bool Featured,
    string[] Skills
) : IRequest<ProjectDto?>;
