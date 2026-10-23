using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

public record GetMyZenStatusQuery : IRequest<MyZenStatusDto>;

public record GetMyZenProjectsQuery : IRequest<IReadOnlyList<MyZenProjectDto>>;

public record GetMyZenTimeEntriesQuery(DateOnly From, DateOnly To) : IRequest<IReadOnlyList<MyZenTimeEntryDto>>;

public record GetMyZenTimeEntryQuery(long Id) : IRequest<MyZenTimeEntryDto?>;

public class GetMyZenTimeEntriesQueryValidator : AbstractValidator<GetMyZenTimeEntriesQuery>
{
    /// <summary>Longest period one request may read; each page is a round trip to MyZen.</summary>
    public const int MaxDays = 93;

    public GetMyZenTimeEntriesQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("'to' must not be before 'from'");

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber < MaxDays)
            .WithName("to")
            .WithMessage($"The period must not exceed {MaxDays} days");
    }
}

public class GetMyZenTimeEntryQueryValidator : AbstractValidator<GetMyZenTimeEntryQuery>
{
    public GetMyZenTimeEntryQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
    }
}

public class GetMyZenStatusQueryHandler(IMyZenGateway gateway) : IRequestHandler<GetMyZenStatusQuery, MyZenStatusDto>
{
    public Task<MyZenStatusDto> Handle(GetMyZenStatusQuery request, CancellationToken cancellationToken) =>
        gateway.GetStatusAsync(cancellationToken);
}

public class GetMyZenProjectsQueryHandler(IMyZenGateway gateway)
    : IRequestHandler<GetMyZenProjectsQuery, IReadOnlyList<MyZenProjectDto>>
{
    public Task<IReadOnlyList<MyZenProjectDto>> Handle(GetMyZenProjectsQuery request, CancellationToken cancellationToken) =>
        gateway.GetProjectsAsync(cancellationToken);
}

public class GetMyZenTimeEntriesQueryHandler(IMyZenGateway gateway)
    : IRequestHandler<GetMyZenTimeEntriesQuery, IReadOnlyList<MyZenTimeEntryDto>>
{
    public Task<IReadOnlyList<MyZenTimeEntryDto>> Handle(GetMyZenTimeEntriesQuery request, CancellationToken cancellationToken) =>
        gateway.GetTimeEntriesAsync(request.From, request.To, cancellationToken);
}

public class GetMyZenTimeEntryQueryHandler(IMyZenGateway gateway) : IRequestHandler<GetMyZenTimeEntryQuery, MyZenTimeEntryDto?>
{
    public Task<MyZenTimeEntryDto?> Handle(GetMyZenTimeEntryQuery request, CancellationToken cancellationToken) =>
        gateway.GetTimeEntryAsync(request.Id, cancellationToken);
}
