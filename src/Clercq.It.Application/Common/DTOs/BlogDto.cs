namespace Clercq.It.Application.Common.DTOs;

public record BlogDto(
    Guid Id,
    DateTime CreatedDate,
    DateTime PublishDate,
    string ShortDescription,
    string LongDescription,
    string Image,
    string[] Tags
);