using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Certifications.Queries;

public record GetCertificationByIdQuery(Guid Id) : IRequest<CertificationDto?>;
