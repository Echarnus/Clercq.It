using MediatR;

namespace Clercq.It.Application.Features.Projects.Commands;

public record DeleteProjectCommand(Guid Id) : IRequest<bool>;
