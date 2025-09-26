namespace Clercq.It.Application.Common.DTOs;

public record ProjectDto(
    Guid Id,
    DateTime StartDate,
    DateTime EndDate,
    string ShortDescription,
    string LongDescription,
    string Image,
    bool Featured,
    string Title,
    string[] Skills
);