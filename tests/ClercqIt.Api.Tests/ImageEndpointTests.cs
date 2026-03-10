using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class ImageEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ImageEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(DisplayName = "POST /api/images returns OK with valid image upload")]
    public async Task UploadImage_ValidImage_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();

        using var formContent = new MultipartFormDataContent();
        var imageBytes = CreateMinimalPngBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(imageContent, "image", "inline-image.png");

        var response = await client.PostAsync("/api/images", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        result.GetProperty("url").GetString().Should().Be("https://test-storage.example.com/test-image.png");
    }

    [Fact(DisplayName = "POST /api/images returns Unauthorized without authentication token")]
    public async Task UploadImage_NoAuthToken_ReturnsUnauthorized()
    {
        using var formContent = new MultipartFormDataContent();
        var imageBytes = CreateMinimalPngBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(imageContent, "image", "inline-image.png");

        var response = await _client.PostAsync("/api/images", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenHelper.GenerateToken("Admin.View", "Blogs.Contributor");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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
