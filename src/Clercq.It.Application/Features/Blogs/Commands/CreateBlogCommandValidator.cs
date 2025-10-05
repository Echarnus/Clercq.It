using FluentValidation;

namespace Clercq.It.Application.Features.Blogs.Commands;

public class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.ShortDescription)
            .NotEmpty().WithMessage("Short description is required")
            .MaximumLength(500).WithMessage("Short description must not exceed 500 characters");

        RuleFor(x => x.LongDescription)
            .NotEmpty().WithMessage("Long description is required")
            .MaximumLength(50000).WithMessage("Long description must not exceed 50000 characters");

        RuleFor(x => x.ImageStream)
            .NotNull().WithMessage("Image is required");

        RuleFor(x => x.ImageFileName)
            .NotEmpty().WithMessage("Image file name is required")
            .Must(BeAValidImageFileName).WithMessage("Invalid image file extension. Only jpg, jpeg, png, gif, and webp are allowed");

        RuleFor(x => x.ImageContentType)
            .NotEmpty().WithMessage("Image content type is required")
            .Must(BeAValidImageContentType).WithMessage("Invalid image content type. Only image types are allowed");

        RuleFor(x => x.Tags)
            .NotNull().WithMessage("Tags are required")
            .Must(x => x != null && x.Length > 0).WithMessage("At least one tag is required")
            .Must(x => x != null && x.Length <= 10).WithMessage("Maximum 10 tags allowed");
    }

    private bool BeAValidImageFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return validExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAValidImageContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
