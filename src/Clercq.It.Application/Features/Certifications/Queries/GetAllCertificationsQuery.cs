using MediatR;
using Clercq.It.Application.Common.DTOs;

namespace Clercq.It.Application.Features.Certifications.Queries;

public record GetAllCertificationsQuery() : IRequest<IEnumerable<CertificationDto>>;
