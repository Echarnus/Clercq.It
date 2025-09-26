using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public class Project : IAggregateRoot
{
    public Guid Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public string Title { get; set; } = string.Empty;
    public Skills Skills { get; set; } = new(Array.Empty<string>());
}