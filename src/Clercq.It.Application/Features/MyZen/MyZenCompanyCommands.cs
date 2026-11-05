using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

/// <summary>
/// Customer fields to write (issue #495). On create, null means MyZen's default; on update,
/// null leaves the field as it is. <c>Type</c> is Company or Private; <c>Vat</c> is normalized
/// before it reaches MyZen; <c>Country</c> is an ISO code such as BE; <c>Language</c> is NL, FR, EN or DE.
/// </summary>
public record MyZenCustomerInput(
    string? Name = null,
    string? Type = null,
    string? Vat = null,
    bool? VatLiable = null,
    string? CommercialName = null,
    string? Email = null,
    string? Phone = null,
    string? Mobile = null,
    string? Website = null,
    string? Language = null,
    string? Reference = null,
    string? ContactFirstName = null,
    string? ContactLastName = null,
    string? Street = null,
    string? StreetNumber = null,
    string? Box = null,
    string? Zipcode = null,
    string? City = null,
    string? Country = null)
{
    public bool IsEmpty => this == new MyZenCustomerInput();
}

/// <summary>
/// Project fields to write (issue #495). On update, null leaves the field as it is.
/// <c>Status</c> is a label or value from <see cref="MyZenProjectStatus"/>; amounts are in euro,
/// <c>TimeBudgetHours</c> in hours.
/// </summary>
public record MyZenProjectInput(
    string? Name = null,
    long? CustomerId = null,
    string? Status = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? Reference = null,
    string? Notes = null,
    decimal? Budget = null,
    decimal? TimeBudgetHours = null,
    decimal? Rate = null)
{
    public bool IsEmpty => this == new MyZenProjectInput();
}

/// <summary>Creates one customer; refused when a customer with the same VAT number (or, without one, the same name) exists.</summary>
public record CreateMyZenCustomerCommand(MyZenCustomerInput Customer) : IRequest<MyZenCustomerDto>;

/// <summary>Changes the given fields of one customer; null when MyZen has no customer with this ID.</summary>
public record UpdateMyZenCustomerCommand(long Id, MyZenCustomerInput Customer) : IRequest<MyZenCustomerDto?>;

/// <summary>Creates one project; refused when the customer already has a project with this name.</summary>
public record CreateMyZenProjectCommand(MyZenProjectInput Project) : IRequest<MyZenProjectDetailsDto>;

/// <summary>Changes the given fields of one project; null when MyZen has no project with this ID.</summary>
public record UpdateMyZenProjectCommand(long Id, MyZenProjectInput Project) : IRequest<MyZenProjectDetailsDto?>;

public partial class MyZenCustomerInputValidator : AbstractValidator<MyZenCustomerInput>
{
    public const int MaxLength = 200;
    private static readonly string[] Types = ["Company", "Private"];
    private static readonly string[] Languages = ["NL", "FR", "EN", "DE"];

    public MyZenCustomerInputValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).When(x => x.Name is not null)
            .WithMessage("Name must not be empty");
        RuleFor(x => x.Type).Must(t => Types.Contains(t, StringComparer.OrdinalIgnoreCase)).When(x => x.Type is not null)
            .WithMessage("Type must be Company or Private");
        RuleFor(x => x.Vat).Must(v => MyZenVat.IsValid(MyZenVat.Normalize(v!))).When(x => !string.IsNullOrWhiteSpace(x.Vat))
            .WithMessage(x => $"VAT number '{x.Vat}' is not valid; write it like BE 0825.170.684 (Belgian numbers must pass the modulo-97 check)");
        RuleFor(x => x.Email).Must(BeEmails).When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email must be a valid e-mail address (separate several with ;)");
        RuleFor(x => x.Website).Must(w => Uri.TryCreate(w!.Contains("://", StringComparison.Ordinal) ? w : "https://" + w, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Website)).WithMessage("Website must be a web address");
        RuleFor(x => x.Language).Must(l => Languages.Contains(l, StringComparer.OrdinalIgnoreCase)).When(x => x.Language is not null)
            .WithMessage("Language must be NL, FR, EN or DE");
        RuleFor(x => x.Country).Must(c => CountryCode().IsMatch(c!)).When(x => x.Country is not null)
            .WithMessage("Country must be a two-letter ISO code such as BE");
        RuleFor(x => x.Zipcode).Must(z => BelgianZip().IsMatch(z!.Trim())).When(x => x.Zipcode is not null && IsBelgium(x.Country))
            .WithMessage("A Belgian postal code has four digits");

        foreach (var (field, name) in new (Func<MyZenCustomerInput, string?> Field, string Name)[]
        {
            (x => x.Name, "Name"), (x => x.CommercialName, "Commercial name"), (x => x.Email, "Email"), (x => x.Phone, "Phone"),
            (x => x.Mobile, "Mobile"), (x => x.Website, "Website"), (x => x.Reference, "Reference"),
            (x => x.ContactFirstName, "Contact first name"), (x => x.ContactLastName, "Contact last name"), (x => x.Street, "Street"),
            (x => x.StreetNumber, "Street number"), (x => x.Box, "Box"), (x => x.Zipcode, "Postal code"), (x => x.City, "City"),
        })
        {
            RuleFor(x => field(x)).MaximumLength(MaxLength).WithName(name).WithMessage($"{name} must not exceed {MaxLength} characters");
        }
    }

    private static bool IsBelgium(string? country) => country is null || country.Equals("BE", StringComparison.OrdinalIgnoreCase);

    private static bool BeEmails(string? emails) =>
        emails!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: > 0 } list
        && list.All(e => EmailAddress().IsMatch(e));

    [GeneratedRegex(@"^[^@\s;]+@[^@\s;]+\.[^@\s;]+$")]
    private static partial Regex EmailAddress();

    [GeneratedRegex("^[A-Za-z]{2}$")]
    private static partial Regex CountryCode();

    [GeneratedRegex("^[0-9]{4}$")]
    private static partial Regex BelgianZip();
}

public class CreateMyZenCustomerCommandValidator : AbstractValidator<CreateMyZenCustomerCommand>
{
    public CreateMyZenCustomerCommandValidator()
    {
        RuleFor(x => x.Customer).NotNull().SetValidator(new MyZenCustomerInputValidator());
        RuleFor(x => x.Customer.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name is required");
        RuleFor(x => x.Customer.Vat).Must(v => !string.IsNullOrWhiteSpace(v))
            .When(x => !"Private".Equals(x.Customer.Type, StringComparison.OrdinalIgnoreCase))
            .WithMessage("VAT number is required for a company; pass type Private for a private customer");
    }
}

public class UpdateMyZenCustomerCommandValidator : AbstractValidator<UpdateMyZenCustomerCommand>
{
    public UpdateMyZenCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
        RuleFor(x => x.Customer).NotNull().SetValidator(new MyZenCustomerInputValidator());
        RuleFor(x => x.Customer).Must(c => !c.IsEmpty).WithMessage("Pass at least one field to change");
    }
}

public class MyZenProjectInputValidator : AbstractValidator<MyZenProjectInput>
{
    public const int MaxNameLength = 200;
    public const int MaxNotesLength = 4000;

    public MyZenProjectInputValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).When(x => x.Name is not null).WithMessage("Name must not be empty");
        RuleFor(x => x.Name).MaximumLength(MaxNameLength).WithMessage($"Name must not exceed {MaxNameLength} characters");
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null).WithMessage("Customer ID must be positive");
        RuleFor(x => x.Status).Must(s => MyZenProjectStatus.ToValue(s) is not null).When(x => x.Status is not null)
            .WithMessage($"Status must be {MyZenProjectStatus.Accepted}");
        RuleFor(x => x.StartDate).Must(d => d!.Value.Year is >= 2000 and <= 2100).When(x => x.StartDate is not null)
            .WithMessage("Start date must be between 2000 and 2100");
        RuleFor(x => x.EndDate).Must(d => d!.Value.Year is >= 2000 and <= 2100).When(x => x.EndDate is not null)
            .WithMessage("End date must be between 2000 and 2100");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.StartDate is not null && x.EndDate is not null)
            .WithMessage("End date must not be before the start date");
        RuleFor(x => x.Reference).MaximumLength(MaxNameLength).WithMessage($"Reference must not exceed {MaxNameLength} characters");
        RuleFor(x => x.Notes).MaximumLength(MaxNotesLength).WithMessage($"Notes must not exceed {MaxNotesLength} characters");
        RuleFor(x => x.Budget).InclusiveBetween(0, 100_000_000).When(x => x.Budget is not null).WithMessage("Budget must be 0 or more");
        RuleFor(x => x.Rate).InclusiveBetween(0, 100_000).When(x => x.Rate is not null).WithMessage("Rate must be 0 or more");
        RuleFor(x => x.TimeBudgetHours).InclusiveBetween(0, 100_000).When(x => x.TimeBudgetHours is not null)
            .WithMessage("Time budget must be 0 hours or more");
    }
}

public class CreateMyZenProjectCommandValidator : AbstractValidator<CreateMyZenProjectCommand>
{
    public CreateMyZenProjectCommandValidator()
    {
        RuleFor(x => x.Project).NotNull().SetValidator(new MyZenProjectInputValidator());
        RuleFor(x => x.Project.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name is required");
        RuleFor(x => x.Project.CustomerId).NotNull().WithMessage("Customer ID is required; find it with search_myzen_customers");
        RuleFor(x => x.Project.StartDate).NotNull().WithMessage("Start date is required (yyyy-MM-dd)");
    }
}

public class UpdateMyZenProjectCommandValidator : AbstractValidator<UpdateMyZenProjectCommand>
{
    public UpdateMyZenProjectCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive");
        RuleFor(x => x.Project).NotNull().SetValidator(new MyZenProjectInputValidator());
        RuleFor(x => x.Project).Must(p => !p.IsEmpty).WithMessage("Pass at least one field to change");
    }
}

public class CreateMyZenCustomerCommandHandler(IMyZenGateway gateway) : IRequestHandler<CreateMyZenCustomerCommand, MyZenCustomerDto>
{
    public async Task<MyZenCustomerDto> Handle(CreateMyZenCustomerCommand request, CancellationToken cancellationToken)
    {
        var input = MyZenCompanyRules.Normalize(request.Customer);
        if (await MyZenCompanyRules.FindCustomerAsync(gateway, input.Vat, input.Name!, exceptId: null, cancellationToken) is { } existing)
        {
            throw new MyZenRejectedException(MyZenCompanyRules.DuplicateCustomer(existing, input.Vat));
        }

        var id = await gateway.CreateCustomerAsync(input, cancellationToken)
            ?? (await MyZenCompanyRules.FindCustomerAsync(gateway, input.Vat, input.Name!, exceptId: null, cancellationToken))?.Id;
        return (id is { } created ? await gateway.GetCustomerAsync(created, cancellationToken) : null)
            ?? throw new MyZenUnavailableException(
                "MyZen accepted the customer but did not show it afterwards. Check MyZen's customer list before trying again.");
    }
}

public class UpdateMyZenCustomerCommandHandler(IMyZenGateway gateway) : IRequestHandler<UpdateMyZenCustomerCommand, MyZenCustomerDto?>
{
    public async Task<MyZenCustomerDto?> Handle(UpdateMyZenCustomerCommand request, CancellationToken cancellationToken)
    {
        var input = MyZenCompanyRules.Normalize(request.Customer);
        if (input.Vat is not null
            && await MyZenCompanyRules.FindCustomerAsync(gateway, input.Vat, name: null, exceptId: request.Id, cancellationToken) is { } existing)
        {
            throw new MyZenRejectedException(MyZenCompanyRules.DuplicateCustomer(existing, input.Vat));
        }

        return await gateway.UpdateCustomerAsync(request.Id, input, cancellationToken)
            ? await gateway.GetCustomerAsync(request.Id, cancellationToken)
            : null;
    }
}

public class CreateMyZenProjectCommandHandler(IMyZenGateway gateway) : IRequestHandler<CreateMyZenProjectCommand, MyZenProjectDetailsDto>
{
    public async Task<MyZenProjectDetailsDto> Handle(CreateMyZenProjectCommand request, CancellationToken cancellationToken)
    {
        var input = MyZenCompanyRules.Normalize(request.Project) with { Status = MyZenProjectStatus.ToValue(request.Project.Status) ?? MyZenProjectStatus.Prospect };
        var customer = await MyZenCompanyRules.CustomerAsync(gateway, input.CustomerId!.Value, cancellationToken);
        if (await MyZenCompanyRules.FindProjectAsync(gateway, input.Name!, customer, exceptId: null, cancellationToken) is { } existing)
        {
            throw new MyZenRejectedException(MyZenCompanyRules.DuplicateProject(existing, customer));
        }

        var id = await gateway.CreateProjectAsync(input, customer.Name, cancellationToken)
            ?? (await MyZenCompanyRules.FindProjectAsync(gateway, input.Name!, customer, exceptId: null, cancellationToken))?.Id;
        return (id is { } created ? await gateway.GetProjectAsync(created, cancellationToken) : null)
            ?? throw new MyZenUnavailableException(
                "MyZen accepted the project but did not show it afterwards. Check MyZen's project list before trying again.");
    }
}

public class UpdateMyZenProjectCommandHandler(IMyZenGateway gateway) : IRequestHandler<UpdateMyZenProjectCommand, MyZenProjectDetailsDto?>
{
    public async Task<MyZenProjectDetailsDto?> Handle(UpdateMyZenProjectCommand request, CancellationToken cancellationToken)
    {
        if (await gateway.GetProjectAsync(request.Id, cancellationToken) is not { } current)
        {
            return null;
        }

        var input = MyZenCompanyRules.Normalize(request.Project) with { Status = MyZenProjectStatus.ToValue(request.Project.Status) };
        var start = input.StartDate ?? current.StartDate;
        var end = input.EndDate ?? current.EndDate;
        if (start is not null && end is not null && end < start)
        {
            throw new MyZenRejectedException($"The end date {end:yyyy-MM-dd} would be before the start date {start:yyyy-MM-dd}.");
        }

        string? customerName = null;
        if (input.CustomerId is { } customerId && customerId != current.CustomerId)
        {
            customerName = (await MyZenCompanyRules.CustomerAsync(gateway, customerId, cancellationToken)).Name;
        }

        if ((input.Name is not null && !MyZenCompanyRules.SameName(input.Name, current.Name)) || customerName is not null)
        {
            var customer = customerName is not null
                ? new MyZenCustomerSummaryDto(input.CustomerId!.Value, null, customerName, null, null, null, null, null)
                : new MyZenCustomerSummaryDto(current.CustomerId ?? 0, null, current.Customer ?? string.Empty, null, null, null, null, null);
            if (await MyZenCompanyRules.FindProjectAsync(gateway, input.Name ?? current.Name, customer, request.Id, cancellationToken) is { } existing)
            {
                throw new MyZenRejectedException(MyZenCompanyRules.DuplicateProject(existing, customer));
            }
        }

        return await gateway.UpdateProjectAsync(request.Id, input, customerName, cancellationToken)
            ? await gateway.GetProjectAsync(request.Id, cancellationToken)
            : null;
    }
}

/// <summary>The duplicate checks and clean-up shared by the customer and project commands.</summary>
internal static class MyZenCompanyRules
{
    public static MyZenCustomerInput Normalize(MyZenCustomerInput input) => input with
    {
        Name = input.Name?.Trim(),
        Type = input.Type is null ? null : "Private".Equals(input.Type.Trim(), StringComparison.OrdinalIgnoreCase) ? "Private" : "Company",
        Vat = string.IsNullOrWhiteSpace(input.Vat) ? null : MyZenVat.Normalize(input.Vat),
        Language = input.Language?.Trim().ToUpperInvariant(),
        Country = input.Country?.Trim().ToUpperInvariant(),
        Zipcode = input.Zipcode?.Trim(),
    };

    public static MyZenProjectInput Normalize(MyZenProjectInput input) => input with { Name = input.Name?.Trim() };

    public static bool SameName(string a, string b) =>
        string.Equals(Collapse(a), Collapse(b), StringComparison.OrdinalIgnoreCase);

    /// <summary>A customer with this VAT number or, without one, this exact name; <paramref name="exceptId"/> is skipped.</summary>
    public static async Task<MyZenCustomerSummaryDto?> FindCustomerAsync(IMyZenGateway gateway, string? vat, string? name, long? exceptId,
        CancellationToken cancellationToken)
    {
        if (vat is not null)
        {
            var matches = await gateway.GetCustomersAsync(vat, cancellationToken);
            return matches.FirstOrDefault(c => c.Id != exceptId && c.Vat is { } v && MyZenVat.Normalize(v) == vat);
        }

        if (name is null)
        {
            return null;
        }

        var named = await gateway.GetCustomersAsync(name, cancellationToken);
        return named.FirstOrDefault(c => c.Id != exceptId && SameName(c.Name, name));
    }

    public static async Task<MyZenCustomerSummaryDto> CustomerAsync(IMyZenGateway gateway, long id, CancellationToken cancellationToken)
    {
        var customer = await gateway.GetCustomerAsync(id, cancellationToken)
            ?? throw new MyZenRejectedException($"MyZen has no customer with id {id}. Find it with search_myzen_customers.");
        return new MyZenCustomerSummaryDto(customer.Id, customer.Number, customer.Name, customer.Vat, null, null, null, null);
    }

    /// <summary>A project of this customer with this name; <paramref name="exceptId"/> is skipped.</summary>
    public static async Task<MyZenProjectDto?> FindProjectAsync(IMyZenGateway gateway, string name, MyZenCustomerSummaryDto customer,
        long? exceptId, CancellationToken cancellationToken)
    {
        var projects = await gateway.GetProjectsAsync(cancellationToken);
        return projects.FirstOrDefault(p => p.Id != exceptId
            && SameName(p.Name, name)
            && (p.CustomerId == customer.Id || (p.CustomerId is null && SameName(p.Customer, customer.Name))));
    }

    public static string DuplicateCustomer(MyZenCustomerSummaryDto existing, string? vat) =>
        vat is null
            ? $"MyZen already has a customer named '{existing.Name}' (id {existing.Id}). Nothing was created; use update_myzen_customer to change it."
            : $"MyZen already has a customer with VAT number {vat}: '{existing.Name}' (id {existing.Id}). Nothing was created or changed; use update_myzen_customer to change that customer.";

    public static string DuplicateProject(MyZenProjectDto existing, MyZenCustomerSummaryDto customer) =>
        $"Customer '{customer.Name}' already has a project named '{existing.Name}' (id {existing.Id}). Nothing was created or changed; use update_myzen_project to change it.";

    private static string Collapse(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
