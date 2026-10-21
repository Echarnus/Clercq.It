using Clercq.It.Application.Common.DTOs;
using MediatR;

namespace Clercq.It.Application.Features.Certifications.Queries;

public record GetCertificationByIdQuery(Guid Id) : IRequest<CertificationDto?>;
