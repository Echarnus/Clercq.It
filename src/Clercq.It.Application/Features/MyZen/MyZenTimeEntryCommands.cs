using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

/// <summary>Adds one entry; entries are never written in bulk without an explicit request.</summary>
public record CreateMyZenTimeEntryCommand(MyZenTimeEntryInput Entry) : IRequest;

/// <summary>Replaces one entry; the result is false when MyZen has no entry with this ID.</summary>
public record UpdateMyZenTimeEntryCommand(long Id, MyZenTimeEntryInput Entry) : IRequest<bool>;

/// <summary>Deletes one entry; the result is false when MyZen has no entry with this ID.</summary>
public record DeleteMyZenTimeEntryCommand(long Id) : IRequest<bool>;

public class MyZenTimeEntryInputValidator : AbstractValidator<MyZenTimeEntryInput>
{
    public const int MaxDescriptionLength = 1000;

    public MyZenTimeEntryInputValidator()
    {
        RuleFor(x => x.Date)
            .Must(d => d.Year is >= 2000 and <= 2100).WithMessage("Date must be between 2000 and 2100");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 24 * 60).WithMessage("Duration must be between 1 minute and 24 hours");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("Project is required");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).When(x => x.CustomerId is not null).WithMessage("Customer ID must be positive");

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description must not exceed {MaxDescriptionLength} characters");
    }
}

public class CreateMyZenTimeEntryCommandValidator : AbstractValidator<CreateMyZenTimeEntryCommand>
{
    public CreateMyZenTimeEntryCommandValidator()
    {
        RuleFor(x => x.Entry).NotNull().SetValidator(new MyZenTimeEntryInputValidator());
    }
}

public class UpdateMyZenTimeEntryCommandValidator : AbstractValidator<UpdateMyZenTimeEntryCommand>
{
    public UpdateMyZenTimeEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
        RuleFor(x => x.Entry).NotNull().SetValidator(new MyZenTimeEntryInputValidator());
    }
}

public class DeleteMyZenTimeEntryCommandValidator : AbstractValidator<DeleteMyZenTimeEntryCommand>
{
    public DeleteMyZenTimeEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
    }
}

public class CreateMyZenTimeEntryCommandHandler(IMyZenGateway gateway) : IRequestHandler<CreateMyZenTimeEntryCommand>
{
    public Task Handle(CreateMyZenTimeEntryCommand request, CancellationToken cancellationToken) =>
        gateway.CreateTimeEntryAsync(Normalize(request.Entry), cancellationToken);

    internal static MyZenTimeEntryInput Normalize(MyZenTimeEntryInput entry) =>
        entry with { Description = entry.Description?.Trim() ?? string.Empty };
}

public class UpdateMyZenTimeEntryCommandHandler(IMyZenGateway gateway) : IRequestHandler<UpdateMyZenTimeEntryCommand, bool>
{
    public Task<bool> Handle(UpdateMyZenTimeEntryCommand request, CancellationToken cancellationToken) =>
        gateway.UpdateTimeEntryAsync(request.Id, CreateMyZenTimeEntryCommandHandler.Normalize(request.Entry), cancellationToken);
}

public class DeleteMyZenTimeEntryCommandHandler(IMyZenGateway gateway) : IRequestHandler<DeleteMyZenTimeEntryCommand, bool>
{
    public Task<bool> Handle(DeleteMyZenTimeEntryCommand request, CancellationToken cancellationToken) =>
        gateway.DeleteTimeEntryAsync(request.Id, cancellationToken);
}
