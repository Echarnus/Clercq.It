using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class ProjectEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProjectEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(DisplayName = "GET /api/projects returns OK")]
    public async Task GetAllProjects_EndpointCalled_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "POST /api/projects returns Created with valid data and image")]
    public async Task CreateProject_WithImage_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateProjectFormContent(
            "Test Project", "Short desc", "Long description",
            "2024-01-01", "2024-12-31", "false", "csharp,dotnet");

        var response = await client.PostAsync("/api/projects", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        project.GetProperty("title").GetString().Should().Be("Test Project");
        project.GetProperty("featured").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "POST /api/projects returns Created without image")]
    public async Task CreateProject_WithoutImage_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent("No Image Project"), "title");
        formContent.Add(new StringContent("Short"), "shortDescription");
        formContent.Add(new StringContent("Long description"), "longDescription");
        formContent.Add(new StringContent("2024-01-01"), "startDate");
        formContent.Add(new StringContent("2024-12-31"), "endDate");
        formContent.Add(new StringContent("false"), "featured");
        formContent.Add(new StringContent("python"), "skills");

        var response = await client.PostAsync("/api/projects", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        project.GetProperty("title").GetString().Should().Be("No Image Project");
    }

    [Fact(DisplayName = "POST /api/projects returns Created with featured flag set to true")]
    public async Task CreateProject_AsFeatured_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateProjectFormContent(
            "Featured Project", "Featured short", "Featured long description",
            "2024-01-01", "2024-06-30", "true", "react,typescript");

        var response = await client.PostAsync("/api/projects", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        project.GetProperty("featured").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "POST /api/projects returns Unauthorized without authentication token")]
    public async Task CreateProject_NoAuthToken_ReturnsUnauthorized()
    {
        using var formContent = CreateProjectFormContent(
            "Unauth Project", "Short", "Long",
            "2024-01-01", "2024-12-31", "false", "java");

        var response = await _client.PostAsync("/api/projects", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/projects/featured returns only featured projects")]
    public async Task GetFeaturedProjects_MixedProjects_ReturnsOnlyFeatured()
    {
        var client = CreateAuthenticatedClient();

        // Create a featured project
        using var featuredContent = CreateProjectFormContent(
            "Featured Only", "Featured short", "Featured long",
            "2024-01-01", "2024-12-31", "true", "azure");
        var featuredResponse = await client.PostAsync("/api/projects", featuredContent);
        featuredResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create a non-featured project
        using var normalContent = CreateProjectFormContent(
            "Normal Only", "Normal short", "Normal long",
            "2024-01-01", "2024-12-31", "false", "aws");
        var normalResponse = await client.PostAsync("/api/projects", normalContent);
        normalResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync("/api/projects/featured");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);
        projects.Should().NotBeNull();
        projects!.Should().AllSatisfy(p =>
            p.GetProperty("featured").GetBoolean().Should().BeTrue());
    }

    [Fact(DisplayName = "GET /api/projects/{id} returns OK for existing project")]
    public async Task GetProjectById_ProjectExists_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateProjectFormContent(
            "Get By Id Project", "Short", "Long",
            "2024-01-01", "2024-12-31", "false", "go");

        var createResponse = await client.PostAsync("/api/projects", formContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProject = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var projectId = createdProject.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/projects/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var project = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
        project.GetProperty("title").GetString().Should().Be("Get By Id Project");
    }

    [Fact(DisplayName = "GET /api/projects/{id} returns NotFound for non-existent project")]
    public async Task GetProjectById_ProjectDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /api/projects/{id} returns OK when toggling featured flag")]
    public async Task UpdateProject_TogglingFeatured_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();

        // Create a non-featured project
        using var createContent = CreateProjectFormContent(
            "Toggle Project", "Short", "Long",
            "2024-01-01", "2024-12-31", "false", "rust");
        var createResponse = await client.PostAsync("/api/projects", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProject = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var projectId = createdProject.GetProperty("id").GetString();

        // Update to featured
        using var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("Toggle Project Updated"), "title");
        updateContent.Add(new StringContent("Short updated"), "shortDescription");
        updateContent.Add(new StringContent("Long updated"), "longDescription");
        updateContent.Add(new StringContent("2024-01-01"), "startDate");
        updateContent.Add(new StringContent("2024-12-31"), "endDate");
        updateContent.Add(new StringContent("true"), "featured");
        updateContent.Add(new StringContent("rust,wasm"), "skills");

        var updateResponse = await client.PutAsync($"/api/projects/{projectId}", updateContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProject = JsonSerializer.Deserialize<JsonElement>(
            await updateResponse.Content.ReadAsStringAsync(), JsonOptions);
        updatedProject.GetProperty("featured").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "DELETE /api/projects/{id} returns NoContent after successful deletion")]
    public async Task DeleteProject_ProjectExists_ReturnsNoContent()
    {
        var client = CreateAuthenticatedClient();

        using var createContent = CreateProjectFormContent(
            "Delete Me", "Short", "Long",
            "2024-01-01", "2024-12-31", "false", "swift");
        var createResponse = await client.PostAsync("/api/projects", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProject = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var projectId = createdProject.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/projects/{projectId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "DELETE /api/projects/{id} returns Unauthorized without authentication token")]
    public async Task DeleteProject_NoAuthToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync($"/api/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenHelper.GenerateToken("Admin.View", "Projects.Contributor");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static MultipartFormDataContent CreateProjectFormContent(
        string title, string shortDescription, string longDescription,
        string startDate, string endDate, string featured, string skills)
    {
        var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(title), "title");
        formContent.Add(new StringContent(shortDescription), "shortDescription");
        formContent.Add(new StringContent(longDescription), "longDescription");
        formContent.Add(new StringContent(startDate), "startDate");
        formContent.Add(new StringContent(endDate), "endDate");
        formContent.Add(new StringContent(featured), "featured");
        formContent.Add(new StringContent(skills), "skills");

        var imageBytes = CreateMinimalPngBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(imageContent, "image", "test-project-image.png");

        return formContent;
    }

    private static byte[] CreateMinimalPngBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02,
            0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00,
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
            0x60, 0x82
        ];
    }
}
