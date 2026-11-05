using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

public record GetMyZenInvoicesQuery(MyZenInvoiceFilter Filter) : IRequest<IReadOnlyList<MyZenInvoiceSummaryDto>>;

public record GetMyZenInvoiceQuery(string Direction, string Kind, long Id) : IRequest<MyZenInvoiceDto?>;

public class GetMyZenInvoicesQueryValidator : AbstractValidator<GetMyZenInvoicesQuery>
{
    /// <summary>Longest period one request may read; MyZen lists 20 invoices per round trip.</summary>
    public const int MaxDays = 366;

    public GetMyZenInvoicesQueryValidator()
    {
        RuleFor(x => x.Filter.Direction)
            .Must(MyZenInvoiceDirection.All.Contains).WithName("direction")
            .WithMessage("'direction' must be 'sales' or 'purchase'");

        RuleFor(x => x.Filter.Kind)
            .Must(MyZenInvoiceKind.All.Contains).WithName("kind")
            .WithMessage("'kind' must be 'invoice' or 'creditNote'");

        RuleFor(x => x.Filter.To)
            .GreaterThanOrEqualTo(x => x.Filter.From).WithName("to")
            .WithMessage("'to' must not be before 'from'");

        RuleFor(x => x.Filter)
            .Must(f => f.To.DayNumber - f.From.DayNumber < MaxDays).WithName("to")
            .WithMessage($"The period must not exceed {MaxDays} days");

        RuleFor(x => x.Filter.Status)
            .Must(s => s is null || MyZenInvoiceStatus.All.Contains(s)).WithName("status")
            .WithMessage("'status' must be one of " + string.Join(", ", MyZenInvoiceStatus.All));

        RuleFor(x => x.Filter.Search)
            .MaximumLength(100).WithName("search")
            .WithMessage("'search' must be at most 100 characters");

        RuleFor(x => x.Filter.Counterparty)
            .MaximumLength(100).WithName("counterparty")
            .WithMessage("'counterparty' must be at most 100 characters");

        RuleFor(x => x.Filter)
            .Must(f => f.MinAmount is null || f.MaxAmount is null || f.MinAmount <= f.MaxAmount).WithName("maxAmount")
            .WithMessage("'minAmount' must not be above 'maxAmount'");
    }
}

public class GetMyZenInvoiceQueryValidator : AbstractValidator<GetMyZenInvoiceQuery>
{
    public GetMyZenInvoiceQueryValidator()
    {
        RuleFor(x => x.Direction)
            .Must(MyZenInvoiceDirection.All.Contains).WithMessage("'direction' must be 'sales' or 'purchase'");
        RuleFor(x => x.Kind)
            .Must(MyZenInvoiceKind.All.Contains).WithMessage("'kind' must be 'invoice' or 'creditNote'");
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
    }
}

public class GetMyZenInvoicesQueryHandler(IMyZenInvoiceGateway gateway)
    : IRequestHandler<GetMyZenInvoicesQuery, IReadOnlyList<MyZenInvoiceSummaryDto>>
{
    public async Task<IReadOnlyList<MyZenInvoiceSummaryDto>> Handle(GetMyZenInvoicesQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter with
        {
            Search = string.IsNullOrWhiteSpace(request.Filter.Search) ? null : request.Filter.Search.Trim(),
        };
        var invoices = await gateway.GetInvoicesAsync(filter, cancellationToken);

        var counterparty = filter.Counterparty?.Trim();
        return invoices
            .Where(i => string.IsNullOrEmpty(counterparty)
                || i.Counterparty.Contains(counterparty, StringComparison.OrdinalIgnoreCase))
            .Where(i => filter.MinAmount is not { } min || i.TotalIncl >= min)
            .Where(i => filter.MaxAmount is not { } max || i.TotalIncl <= max)
            .ToList();
    }
}

public class GetMyZenInvoiceQueryHandler(IMyZenInvoiceGateway gateway) : IRequestHandler<GetMyZenInvoiceQuery, MyZenInvoiceDto?>
{
    public Task<MyZenInvoiceDto?> Handle(GetMyZenInvoiceQuery request, CancellationToken cancellationToken) =>
        gateway.GetInvoiceAsync(request.Direction, request.Kind, request.Id, cancellationToken);
}
