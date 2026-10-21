using Clercq.It.Domain.Abstractions;
using Clercq.It.Domain.ValueObjects;

namespace Clercq.It.Domain.Entities;

public class Project : IAggregateRoot
{
    // For EF Core, which sets the private setters when it loads a row.
    private Project() { }

    public Project(Guid id, string title, string shortDescription, string longDescription, string image,
        DateTime startDate, DateTime endDate, bool featured, Skills skills)
    {
        Id = id;
        Update(title, shortDescription, longDescription, startDate, endDate, featured, skills);
        ChangeImage(image);
    }

    public Guid Id { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string ShortDescription { get; private set; } = string.Empty;
    public string LongDescription { get; private set; } = string.Empty;
    public string Image { get; private set; } = string.Empty;
    public bool Featured { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Skills Skills { get; private set; } = new(Array.Empty<string>());

    public static Project Create(string title, string shortDescription, string longDescription, string image,
        DateTime startDate, DateTime endDate, bool featured, Skills skills) =>
        new(Guid.NewGuid(), title, shortDescription, longDescription, image, startDate, endDate, featured, skills);

    /// <summary>Replaces the project's details.</summary>
    /// <exception cref="ArgumentException">The title is blank or the project ends before it starts.</exception>
    public void Update(string title, string shortDescription, string longDescription, DateTime startDate,
        DateTime endDate, bool featured, Skills skills)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(shortDescription);
        ArgumentNullException.ThrowIfNull(longDescription);
        ArgumentNullException.ThrowIfNull(skills);
        if (endDate < startDate)
        {
            throw new ArgumentException("A project cannot end before it starts.", nameof(endDate));
        }

        Title = title;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        StartDate = startDate;
        EndDate = endDate;
        Featured = featured;
        Skills = skills;
    }

    public void Feature() => Featured = true;

    public void Unfeature() => Featured = false;

    /// <summary>Sets the project's image.</summary>
    /// <param name="image">The image URL, or empty for a project without an image.</param>
    public void ChangeImage(string image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
    }
}
