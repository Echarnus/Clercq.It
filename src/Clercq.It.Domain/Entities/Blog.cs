using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public class Blog : IAggregateRoot
{
    // For EF Core, which sets the private setters when it loads a row.
    private Blog() { }

    public Blog(Guid id, DateTime createdDate, DateTime publishDate, string title, string shortDescription,
        string longDescription, string image, Tags tags)
    {
        Id = id;
        Reschedule(createdDate, publishDate);
        Update(title, shortDescription, longDescription, tags);
        ChangeImage(image);
    }

    public Guid Id { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime PublishDate { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string LongDescription { get; private set; } = string.Empty;
    public string Image { get; private set; } = string.Empty;
    public Tags Tags { get; private set; } = new(Array.Empty<string>());

    /// <summary>A new blog, created and published now.</summary>
    public static Blog Create(string title, string shortDescription, string longDescription, string image, Tags tags,
        DateTime now) =>
        new(Guid.NewGuid(), now, now, title, shortDescription, longDescription, image, tags);

    public void Update(string title, string shortDescription, string longDescription, Tags tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortDescription);
        ArgumentNullException.ThrowIfNull(longDescription);
        ArgumentNullException.ThrowIfNull(tags);

        Title = title;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        Tags = tags;
    }

    public void Reschedule(DateTime createdDate, DateTime publishDate)
    {
        CreatedDate = createdDate;
        PublishDate = publishDate;
    }

    public void ChangeImage(string image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
    }
}
