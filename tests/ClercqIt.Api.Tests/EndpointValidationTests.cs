using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Invalid input is rejected with 400 by the endpoint or by the request's
/// FluentValidation validator (MediatR pipeline), and nothing is stored.
/// </summary>
[Collection(ApiTestGroup.Name)]
public class EndpointValidationTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "POST /api/blogs without a short description returns 400 with the field error")]
    public async Task CreateBlog_EmptyShortDescription_ReturnsValidationProblem()
    {
        var client = await factory.CreateClientForUserAsync("blogger");
        using var form = Form(
            ("title", "Title"), ("shortDescription", ""), ("longDescription", "Body"), ("tags", "dotnet"));
        form.Add(Png(), "image", "header.png");

        var response = await client.PostAsync("/api/blogs", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorsAsync(response)).Should().ContainKey("ShortDescription");
        (await client.GetStringAsync("/api/blogs")).Should().Be("[]");
    }

    [Fact(DisplayName = "POST /api/blogs without a title returns 400 with the field error")]
    public async Task CreateBlog_EmptyTitle_ReturnsValidationProblem()
    {
        var client = await factory.CreateClientForUserAsync("blogger");
        using var form = Form(
            ("title", ""), ("shortDescription", "Short"), ("longDescription", "Body"), ("tags", "dotnet"));
        form.Add(Png(), "image", "header.png");

        var response = await client.PostAsync("/api/blogs", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorsAsync(response)).Should().ContainKey("Title");
    }

    [Fact(DisplayName = "POST /api/blogs without an image returns 400")]
    public async Task CreateBlog_NoImage_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForUserAsync("blogger");
        using var form = Form(
            ("title", "Title"), ("shortDescription", "Short"), ("longDescription", "Body"), ("tags", "dotnet"));

        var response = await client.PostAsync("/api/blogs", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/projects without a title returns 400 with the field error")]
    public async Task CreateProject_EmptyTitle_ReturnsValidationProblem()
    {
        var client = await factory.CreateClientForUserAsync("projects");
        using var form = Form(
            ("title", ""), ("shortDescription", "Short"), ("longDescription", "Body"),
            ("startDate", "2024-01-01"), ("endDate", "2024-06-01"), ("featured", "false"), ("skills", "C#"));

        var response = await client.PostAsync("/api/projects", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorsAsync(response)).Should().ContainKey("Title");
    }

    [Fact(DisplayName = "POST /api/projects with an invalid date returns 400")]
    public async Task CreateProject_InvalidDate_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForUserAsync("projects");
        using var form = Form(
            ("title", "Title"), ("shortDescription", "Short"), ("longDescription", "Body"),
            ("startDate", "not-a-date"), ("endDate", "2024-06-01"), ("skills", "C#"));

        var response = await client.PostAsync("/api/projects", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/certifications without a title returns 400 with the field error")]
    public async Task CreateCertification_EmptyTitle_ReturnsValidationProblem()
    {
        var client = await factory.CreateClientForUserAsync("certs");
        using var form = Form(("title", ""), ("issuer", "Microsoft"), ("issueDate", "2024-01-01"));
        form.Add(Png(), "image", "badge.png");

        var response = await client.PostAsync("/api/certifications", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorsAsync(response)).Should().ContainKey("Title");
    }

    [Fact(DisplayName = "POST /api/images with a non-image file returns 400")]
    public async Task UploadImage_NotAnImage_ReturnsBadRequest()
    {
        var client = await factory.CreateClientForUserAsync("blogger");
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent("not an image"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "image", "notes.txt");

        var response = await client.PostAsync("/api/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static MultipartFormDataContent Form(params (string Name, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            form.Add(new StringContent(value), name);
        }
        return form;
    }

    private static ByteArrayContent Png()
    {
        // 1x1 transparent PNG
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return content;
    }

    private static async Task<Dictionary<string, string[]>> ErrorsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }
}
