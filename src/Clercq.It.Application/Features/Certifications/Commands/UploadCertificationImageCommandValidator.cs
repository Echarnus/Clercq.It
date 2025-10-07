using FluentValidation;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class UploadCertificationImageCommandValidator : AbstractValidator<UploadCertificationImageCommand>
{
    public UploadCertificationImageCommandValidator()
    {
        RuleFor(x => x.ImageStream)
            .NotNull().WithMessage("Image is required");

        RuleFor(x => x.ImageFileName)
            .NotEmpty().WithMessage("Image file name is required")
            .Must(BeAValidImageFileName).WithMessage("Invalid image file extension. Only jpg, jpeg, png, gif, and webp are allowed");

        RuleFor(x => x.ImageContentType)
            .NotEmpty().WithMessage("Image content type is required")
            .Must(BeAValidImageContentType).WithMessage("Invalid image content type. Only image types are allowed");
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
