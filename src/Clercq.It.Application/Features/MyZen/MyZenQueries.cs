using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

public record GetMyZenStatusQuery : IRequest<MyZenStatusDto>;

public record GetMyZenProjectsQuery : IRequest<IReadOnlyList<MyZenProjectDto>>;

public record GetMyZenTimeEntriesQuery(DateOnly From, DateOnly To) : IRequest<IReadOnlyList<MyZenTimeEntryDto>>;

public record GetMyZenTimeEntryQuery(long Id) : IRequest<MyZenTimeEntryDto?>;

public record SearchMyZenProjectsQuery(string Query) : IRequest<IReadOnlyList<MyZenProjectOptionDto>>;

public class SearchMyZenProjectsQueryValidator : AbstractValidator<SearchMyZenProjectsQuery>
{
    public SearchMyZenProjectsQueryValidator()
    {
        // MyZen's lookup only answers from a two-character prefix.
        RuleFor(x => x.Query)
            .Must(q => q?.Trim().Length is >= 2 and <= 50).WithMessage("Search must be 2 to 50 characters");
    }
}

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

public class SearchMyZenProjectsQueryHandler(IMyZenGateway gateway)
    : IRequestHandler<SearchMyZenProjectsQuery, IReadOnlyList<MyZenProjectOptionDto>>
{
    public Task<IReadOnlyList<MyZenProjectOptionDto>> Handle(SearchMyZenProjectsQuery request, CancellationToken cancellationToken) =>
        gateway.SearchProjectsAsync(request.Query.Trim(), cancellationToken);
}

public record GetMyZenProjectQuery(long Id) : IRequest<MyZenProjectDetailsDto?>;

public record GetMyZenCustomersQuery : IRequest<IReadOnlyList<MyZenCustomerSummaryDto>>;

/// <summary>Customers whose name, VAT number or company number matches <c>Query</c>.</summary>
public record SearchMyZenCustomersQuery(string Query) : IRequest<IReadOnlyList<MyZenCustomerSummaryDto>>;

public record GetMyZenCustomerQuery(long Id) : IRequest<MyZenCustomerDto?>;

public class GetMyZenProjectQueryValidator : AbstractValidator<GetMyZenProjectQuery>
{
    public GetMyZenProjectQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
    }
}

public class GetMyZenCustomerQueryValidator : AbstractValidator<GetMyZenCustomerQuery>
{
    public GetMyZenCustomerQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
    }
}

public class SearchMyZenCustomersQueryValidator : AbstractValidator<SearchMyZenCustomersQuery>
{
    public SearchMyZenCustomersQueryValidator()
    {
        RuleFor(x => x.Query)
            .Must(q => q?.Trim().Length is >= 2 and <= 50).WithMessage("Search must be 2 to 50 characters");
    }
}

public class GetMyZenProjectQueryHandler(IMyZenGateway gateway) : IRequestHandler<GetMyZenProjectQuery, MyZenProjectDetailsDto?>
{
    public Task<MyZenProjectDetailsDto?> Handle(GetMyZenProjectQuery request, CancellationToken cancellationToken) =>
        gateway.GetProjectAsync(request.Id, cancellationToken);
}

public class GetMyZenCustomersQueryHandler(IMyZenGateway gateway)
    : IRequestHandler<GetMyZenCustomersQuery, IReadOnlyList<MyZenCustomerSummaryDto>>
{
    public Task<IReadOnlyList<MyZenCustomerSummaryDto>> Handle(GetMyZenCustomersQuery request, CancellationToken cancellationToken) =>
        gateway.GetCustomersAsync(keyword: null, cancellationToken);
}

public class SearchMyZenCustomersQueryHandler(IMyZenGateway gateway)
    : IRequestHandler<SearchMyZenCustomersQuery, IReadOnlyList<MyZenCustomerSummaryDto>>
{
    public Task<IReadOnlyList<MyZenCustomerSummaryDto>> Handle(SearchMyZenCustomersQuery request, CancellationToken cancellationToken) =>
        gateway.GetCustomersAsync(MyZenVat.SearchKeyword(request.Query), cancellationToken);
}

public class GetMyZenCustomerQueryHandler(IMyZenGateway gateway) : IRequestHandler<GetMyZenCustomerQuery, MyZenCustomerDto?>
{
    public Task<MyZenCustomerDto?> Handle(GetMyZenCustomerQuery request, CancellationToken cancellationToken) =>
        gateway.GetCustomerAsync(request.Id, cancellationToken);
}
