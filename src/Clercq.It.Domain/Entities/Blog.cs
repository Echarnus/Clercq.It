using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public class Blog : IAggregateRoot
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime PublishDate { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Tags Tags { get; set; } = new(Array.Empty<string>());
}