using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public record Blog(
    Guid Id,
    DateTime CreatedDate,
    DateTime PublishDate,
    string ShortDescription,
    string LongDescription,
    string Image,
    Tags Tags
) : IAggregateRoot;