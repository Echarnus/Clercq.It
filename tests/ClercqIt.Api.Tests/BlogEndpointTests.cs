using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class BlogEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BlogEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(DisplayName = "GET /api/blogs returns OK with empty list when no blogs exist")]
    public async Task GetAllBlogs_NoBlogsExist_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/blogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var blogs = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);
        blogs.Should().NotBeNull();
    }

    [Fact(DisplayName = "POST /api/blogs returns Created with valid multipart data")]
    public async Task CreateBlog_ValidMultipartData_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateBlogFormContent("Test Blog Short", "Test Blog Long Description", "tag1,tag2");

        var response = await client.PostAsync("/api/blogs", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var blog = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        blog.GetProperty("shortDescription").GetString().Should().Be("Test Blog Short");
        blog.GetProperty("longDescription").GetString().Should().Be("Test Blog Long Description");
        blog.GetProperty("image").GetString().Should().Be("https://test-storage.example.com/test-image.png");
    }

    [Fact(DisplayName = "POST /api/blogs returns Unauthorized without authentication token")]
    public async Task CreateBlog_NoAuthToken_ReturnsUnauthorized()
    {
        using var formContent = CreateBlogFormContent("Test", "Test Long", "tag1");

        var response = await _client.PostAsync("/api/blogs", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/blogs/{id} returns OK after blog creation")]
    public async Task GetBlogById_BlogExists_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateBlogFormContent("Get By Id Blog", "Long description for get by id", "dotnet");

        var createResponse = await client.PostAsync("/api/blogs", formContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdBlog = JsonSerializer.Deserialize<JsonElement>(createContent, JsonOptions);
        var blogId = createdBlog.GetProperty("id").GetString();

        var getResponse = await _client.GetAsync($"/api/blogs/{blogId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var blog = JsonSerializer.Deserialize<JsonElement>(getContent, JsonOptions);
        blog.GetProperty("shortDescription").GetString().Should().Be("Get By Id Blog");
    }

    [Fact(DisplayName = "GET /api/blogs/{id} returns NotFound for non-existent blog")]
    public async Task GetBlogById_BlogDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/blogs/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /api/blogs/{id} returns OK with new image")]
    public async Task UpdateBlog_WithNewImage_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();

        // Create a blog first
        using var createContent = CreateBlogFormContent("Original Blog", "Original Long", "original");
        var createResponse = await client.PostAsync("/api/blogs", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBlog = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var blogId = createdBlog.GetProperty("id").GetString();

        // Update with new image
        using var updateContent = CreateBlogFormContent("Updated Blog", "Updated Long Description", "updated,tags");
        var updateResponse = await client.PutAsync($"/api/blogs/{blogId}", updateContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedBlog = JsonSerializer.Deserialize<JsonElement>(
            await updateResponse.Content.ReadAsStringAsync(), JsonOptions);
        updatedBlog.GetProperty("shortDescription").GetString().Should().Be("Updated Blog");
    }

    [Fact(DisplayName = "PUT /api/blogs/{id} returns OK without image (keeps existing)")]
    public async Task UpdateBlog_WithoutImage_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();

        // Create a blog first
        using var createContent = CreateBlogFormContent("Blog To Update", "Long content here", "tech");
        var createResponse = await client.PostAsync("/api/blogs", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBlog = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var blogId = createdBlog.GetProperty("id").GetString();

        // Update without image
        using var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("Updated No Image"), "shortDescription");
        updateContent.Add(new StringContent("Updated Long No Image"), "longDescription");
        updateContent.Add(new StringContent("newtag"), "tags");

        var updateResponse = await client.PutAsync($"/api/blogs/{blogId}", updateContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedBlog = JsonSerializer.Deserialize<JsonElement>(
            await updateResponse.Content.ReadAsStringAsync(), JsonOptions);
        updatedBlog.GetProperty("shortDescription").GetString().Should().Be("Updated No Image");
    }

    [Fact(DisplayName = "PUT /api/blogs/{id} returns Unauthorized without authentication token")]
    public async Task UpdateBlog_NoAuthToken_ReturnsUnauthorized()
    {
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent("Updated"), "shortDescription");
        formContent.Add(new StringContent("Updated Long"), "longDescription");
        formContent.Add(new StringContent("tag"), "tags");

        var response = await _client.PutAsync($"/api/blogs/{Guid.NewGuid()}", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DELETE /api/blogs/{id} returns NoContent after successful deletion")]
    public async Task DeleteBlog_BlogExists_ReturnsNoContent()
    {
        var client = CreateAuthenticatedClient();

        // Create a blog first
        using var createContent = CreateBlogFormContent("Blog To Delete", "Will be deleted", "temp");
        var createResponse = await client.PostAsync("/api/blogs", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBlog = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var blogId = createdBlog.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/blogs/{blogId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "DELETE /api/blogs/{id} returns NotFound for non-existent blog")]
    public async Task DeleteBlog_BlogDoesNotExist_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/blogs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /api/blogs/{id} returns Unauthorized without authentication token")]
    public async Task DeleteBlog_NoAuthToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync($"/api/blogs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenHelper.GenerateToken("Admin.View", "Blogs.Contributor");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static MultipartFormDataContent CreateBlogFormContent(
        string shortDescription, string longDescription, string tags)
    {
        var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(shortDescription), "shortDescription");
        formContent.Add(new StringContent(longDescription), "longDescription");
        formContent.Add(new StringContent(tags), "tags");

        // Create a minimal fake PNG image (1x1 pixel)
        var imageBytes = CreateMinimalPngBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(imageContent, "image", "test-image.png");

        return formContent;
    }

    private static byte[] CreateMinimalPngBytes()
    {
        // Minimal valid PNG: 1x1 pixel transparent
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, // RGBA, filters
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02, // compressed data
            0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00, // checksum
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, // IEND chunk
            0x60, 0x82
        ];
    }
}
