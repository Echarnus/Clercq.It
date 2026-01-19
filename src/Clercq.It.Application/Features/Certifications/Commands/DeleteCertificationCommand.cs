using MediatR;

namespace Clercq.It.Application.Features.Certifications.Commands;

public record DeleteCertificationCommand(Guid Id) : IRequest<bool>;
