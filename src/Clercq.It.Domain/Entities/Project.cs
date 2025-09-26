using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public record Project(
    Guid Id,
    DateTime StartDate,
    DateTime EndDate,
    string ShortDescription,
    string LongDescription,
    string Image,
    bool Featured,
    string Title,
    Skills Skills
) : IAggregateRoot;