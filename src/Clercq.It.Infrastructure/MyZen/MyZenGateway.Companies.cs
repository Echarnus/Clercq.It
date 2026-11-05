using System.Globalization;
using Clercq.It.Application.Features.MyZen;
using Microsoft.Extensions.Logging;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// Creating and changing MyZen customers and projects (issue #495). Each write opens MyZen's
/// own edit form, keeps every field on it, changes only the fields given, and posts the form
/// back the way the browser does (mapped from the owner's browser on 2026-11-05). There are no deletes.
/// </summary>
public sealed partial class MyZenGateway
{
    private const string NewCustomerPath = "/Company/Customer/CompanyEdit";
    private const string NewProjectPath = "/Project/AddProject";

    public async Task<long?> CreateCustomerAsync(MyZenCustomerInput customer, CancellationToken cancellationToken)
    {
        var saved = await WithSession(session =>
            SaveFormAsync(session, NewCustomerPath, "Save", "customer", page =>
                {
                    ApplyCustomer(page.Form, customer);
                    return true;
                }, body => MyZenHtml.ParseCustomerDetails(body)?.Id, cancellationToken), cancellationToken);
        return saved.Id;
    }

    public async Task<bool> UpdateCustomerAsync(long id, MyZenCustomerInput customer, CancellationToken cancellationToken)
    {
        var path = $"/Company/Customer/{id.ToString(CultureInfo.InvariantCulture)}/CompanyEdit";
        var saved = await WithSession(session =>
            SaveFormAsync(session, path, "Save", "customer", form =>
            {
                // MyZen sends an unknown ID to its no-access page, which has no customer form.
                if (MyZenHtml.ParseCustomerDetails(form.Body)?.Id != id)
                {
                    return false;
                }
                ApplyCustomer(form.Form, customer);
                return true;
            }, _ => id, cancellationToken), cancellationToken);
        return saved.Found;
    }

    public async Task<long?> CreateProjectAsync(MyZenProjectInput project, string customerName, CancellationToken cancellationToken)
    {
        var saved = await WithSession(session =>
            SaveFormAsync(session, NewProjectPath, "BulkinputSaveReceipt", "project", page =>
                {
                    ApplyProject(page.Form, project, customerName);
                    return true;
                }, body => MyZenHtml.ParseProjectDetails(body)?.Id, cancellationToken), cancellationToken);
        return saved.Id;
    }

    public async Task<bool> UpdateProjectAsync(long id, MyZenProjectInput project, string? customerName, CancellationToken cancellationToken)
    {
        var path = $"/Project/{id.ToString(CultureInfo.InvariantCulture)}/EditProject";
        var saved = await WithSession(session =>
            SaveFormAsync(session, path, "BulkinputSaveReceipt", "project", form =>
            {
                // MyZen answers an unknown ID with an error page, which has no project form.
                if (MyZenHtml.ParseProjectDetails(form.Body)?.Id != id)
                {
                    return false;
                }
                ApplyProject(form.Form, project, customerName);
                return true;
            }, _ => id, cancellationToken), cancellationToken);
        return saved.Found;
    }

    /// <summary>
    /// Opens the form at <paramref name="path"/>, lets <paramref name="apply"/> change it (false: the
    /// record does not exist), and posts it back to the same path. Null when MyZen showed its login page.
    /// </summary>
    private async Task<SaveResult?> SaveFormAsync(Session session, string path, string defaultAction, string what,
        Func<FormPage, bool> apply, Func<string, long?> readId, CancellationToken cancellationToken)
    {
        var page = await session.GetAsync(path, cancellationToken);
        if (page.IsLoginPage)
        {
            return null;
        }

        var form = MyZenForm.Parse(page.Body);
        if (form is null || !apply(new FormPage(form, page.Body)))
        {
            return SaveResult.NotFound;
        }

        form.Set("ButtonActionType", form.SaveAction ?? defaultAction);
        if (form.Token is { } token)
        {
            form.Set("__RequestVerificationToken", token);
        }

        var response = await session.PostAsync(path, form.Fields, headerToken: null, cancellationToken);
        if (response.IsLoginPage)
        {
            return null;
        }

        if ((int)response.Status is < 200 or > 299)
        {
            logger.LogWarning("MyZen answered {Status} to a {What} save", (int)response.Status, what);
            throw new MyZenUnavailableException($"MyZen answered {(int)response.Status} to the {what} save.");
        }

        switch (MyZenHtml.ParseFormSave(response.Body))
        {
            case { Error: { } error }:
                logger.LogWarning("MyZen refused a {What} save", what);
                throw new MyZenRejectedException(error);
            case { Saved: false }:
                logger.LogWarning("MyZen did not confirm a {What} save", what);
                throw new MyZenUnavailableException(
                    $"MyZen did not confirm the {what} save. Check MyZen before trying again.");
        }

        logger.LogInformation("MyZen {What} saved", what);
        return new SaveResult(true, readId(response.Body));
    }

    private static void ApplyCustomer(MyZenForm form, MyZenCustomerInput customer)
    {
        form.SetIfGiven("CustomerTypeTC", customer.Type)
            .SetIfGiven("Company.VAT", customer.Vat)
            .SetCheckbox("Company.VATLiable", customer.VatLiable)
            .SetIfGiven("Company.CompanyName", customer.Name)
            .SetIfGiven("Company.CommercialName", customer.CommercialName)
            .SetIfGiven("Company.Email", customer.Email)
            .SetIfGiven("Company.Phone", customer.Phone)
            .SetIfGiven("Company.Mobile", customer.Mobile)
            .SetIfGiven("Company.Website", customer.Website)
            .SetIfGiven("Company.LanguageTC", customer.Language)
            .SetIfGiven("Company.Reference", customer.Reference)
            .SetIfGiven("Company.ContactFirstName", customer.ContactFirstName)
            .SetIfGiven("Company.ContactLastName", customer.ContactLastName)
            .SetIfGiven("InvoiceAddress.Street", customer.Street)
            .SetIfGiven("InvoiceAddress.StreetNumber", customer.StreetNumber)
            .SetIfGiven("InvoiceAddress.Box", customer.Box)
            .SetIfGiven("InvoiceAddress.Zipcode", customer.Zipcode)
            .SetIfGiven("InvoiceAddress.City", customer.City)
            .SetIfGiven("InvoiceAddress.Country", customer.Country);
    }

    private static void ApplyProject(MyZenForm form, MyZenProjectInput project, string? customerName)
    {
        form.SetIfGiven("ProjectName", project.Name)
            .SetIfGiven("ProjectStatus", project.Status)
            .SetIfGiven("Reference", project.Reference)
            .SetIfGiven("Notes", project.Notes);
        if (project.CustomerId is { } customerId && customerName is not null)
        {
            form.Set("CustomerID", customerId.ToString(CultureInfo.InvariantCulture)).Set("CustomerName", customerName);
        }
        if (project.StartDate is { } start)
        {
            form.Set("StartDate", MyZenForm.Date(start));
        }
        if (project.EndDate is { } end)
        {
            form.Set("EndDate", MyZenForm.Date(end));
        }
        if (project.Budget is { } budget)
        {
            form.Set("Budget", MyZenForm.Amount(budget));
        }
        if (project.Rate is { } rate)
        {
            form.Set("Rate", MyZenForm.Amount(rate));
        }
        if (project.TimeBudgetHours is { } hours)
        {
            var minutes = (long)Math.Round(hours * 60m);
            form.Set("TimeBudget", (minutes * 60).ToString(CultureInfo.InvariantCulture))
                .Set("TimeBudgetInput", $"{minutes / 60:00}:{minutes % 60:00}");
        }
    }

    private sealed record FormPage(MyZenForm Form, string Body);

    /// <summary>The outcome of a form save; <c>Id</c> is the record MyZen's answer shows, if any.</summary>
    private sealed record SaveResult(bool Found, long? Id)
    {
        public static readonly SaveResult NotFound = new(false, null);
    }
}
