using FluentValidation;

namespace Clercq.It.Application.Features.Certifications.Commands;

public class UpdateCertificationCommandValidator : AbstractValidator<UpdateCertificationCommand>
{
    public UpdateCertificationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Certification ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Issuer)
            .NotEmpty().WithMessage("Issuer is required")
            .MaximumLength(200).WithMessage("Issuer must not exceed 200 characters");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("Issue date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Issue date cannot be in the future");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.IssueDate).WithMessage("Expiry date must be after issue date")
            .When(x => x.ExpiryDate.HasValue);

        RuleFor(x => x.CredentialId)
            .MaximumLength(200).WithMessage("Credential ID must not exceed 200 characters");

        RuleFor(x => x.CredentialUrl)
            .MaximumLength(500).WithMessage("Credential URL must not exceed 500 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.ImageFileName)
            .Must(BeAValidImageFileName)
            .When(x => !string.IsNullOrEmpty(x.ImageFileName))
            .WithMessage("Invalid image file extension. Only jpg, jpeg, png, gif, and webp are allowed");

        RuleFor(x => x.ImageContentType)
            .Must(BeAValidImageContentType)
            .When(x => !string.IsNullOrEmpty(x.ImageContentType))
            .WithMessage("Invalid image content type. Only image types are allowed");
    }

    private bool BeAValidImageFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return true;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return validExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAValidImageContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return true;

        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
