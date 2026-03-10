using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class CertificationEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CertificationEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact(DisplayName = "GET /api/certifications returns OK")]
    public async Task GetAllCertifications_EndpointCalled_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/certifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "POST /api/certifications returns Created with valid data")]
    public async Task CreateCertification_ValidData_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateCertificationFormContent(
            "Azure Solutions Architect",
            "Microsoft",
            "2024-01-15",
            "2026-01-15",
            "CRED-12345",
            "https://learn.microsoft.com/credentials/12345",
            "Expert level Azure certification");

        var response = await client.PostAsync("/api/certifications", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var certification = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        certification.GetProperty("title").GetString().Should().Be("Azure Solutions Architect");
        certification.GetProperty("issuer").GetString().Should().Be("Microsoft");
        certification.GetProperty("image").GetString().Should().Be("https://test-storage.example.com/test-image.png");
    }

    [Fact(DisplayName = "POST /api/certifications returns Created without expiry date")]
    public async Task CreateCertification_WithoutExpiryDate_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateCertificationFormContent(
            "AWS Cloud Practitioner",
            "Amazon Web Services",
            "2024-03-01",
            null,
            "AWS-CP-001",
            "https://aws.amazon.com/verify/001",
            "Foundational AWS certification");

        var response = await client.PostAsync("/api/certifications", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var certification = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        certification.GetProperty("title").GetString().Should().Be("AWS Cloud Practitioner");
        certification.GetProperty("expiryDate").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "POST /api/certifications returns BadRequest without image")]
    public async Task CreateCertification_NoImage_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent("No Image Cert"), "title");
        formContent.Add(new StringContent("Issuer"), "issuer");
        formContent.Add(new StringContent("2024-01-01"), "issueDate");
        formContent.Add(new StringContent("CRED-000"), "credentialId");
        formContent.Add(new StringContent("https://example.com"), "credentialUrl");
        formContent.Add(new StringContent("Description"), "description");

        var response = await client.PostAsync("/api/certifications", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/certifications returns Unauthorized without authentication token")]
    public async Task CreateCertification_NoAuthToken_ReturnsUnauthorized()
    {
        using var formContent = CreateCertificationFormContent(
            "Cert", "Issuer", "2024-01-01", null, "ID", "https://url", "Desc");

        var response = await _client.PostAsync("/api/certifications", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /api/certifications/{id} returns OK for existing certification")]
    public async Task GetCertificationById_CertificationExists_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();
        using var formContent = CreateCertificationFormContent(
            "Get By Id Cert", "Issuer", "2024-06-01", "2026-06-01",
            "CRED-GET", "https://example.com/get", "Get by id description");

        var createResponse = await client.PostAsync("/api/certifications", formContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCert = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var certId = createdCert.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/certifications/{certId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cert = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
        cert.GetProperty("title").GetString().Should().Be("Get By Id Cert");
    }

    [Fact(DisplayName = "GET /api/certifications/{id} returns NotFound for non-existent certification")]
    public async Task GetCertificationById_CertificationDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/certifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /api/certifications/{id} returns OK with updated data")]
    public async Task UpdateCertification_ValidData_ReturnsOk()
    {
        var client = CreateAuthenticatedClient();

        // Create first
        using var createContent = CreateCertificationFormContent(
            "Original Cert", "Original Issuer", "2024-01-01", "2025-01-01",
            "CRED-UPD", "https://example.com/upd", "Original description");
        var createResponse = await client.PostAsync("/api/certifications", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCert = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var certId = createdCert.GetProperty("id").GetString();

        // Update without image (keeps existing)
        using var updateContent = new MultipartFormDataContent();
        updateContent.Add(new StringContent("Updated Cert"), "title");
        updateContent.Add(new StringContent("Updated Issuer"), "issuer");
        updateContent.Add(new StringContent("2024-02-01"), "issueDate");
        updateContent.Add(new StringContent("2026-02-01"), "expiryDate");
        updateContent.Add(new StringContent("CRED-UPD-2"), "credentialId");
        updateContent.Add(new StringContent("https://example.com/upd2"), "credentialUrl");
        updateContent.Add(new StringContent("Updated description"), "description");

        var updateResponse = await client.PutAsync($"/api/certifications/{certId}", updateContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCert = JsonSerializer.Deserialize<JsonElement>(
            await updateResponse.Content.ReadAsStringAsync(), JsonOptions);
        updatedCert.GetProperty("title").GetString().Should().Be("Updated Cert");
        updatedCert.GetProperty("issuer").GetString().Should().Be("Updated Issuer");
    }

    [Fact(DisplayName = "DELETE /api/certifications/{id} returns NoContent after successful deletion")]
    public async Task DeleteCertification_CertificationExists_ReturnsNoContent()
    {
        var client = CreateAuthenticatedClient();

        using var createContent = CreateCertificationFormContent(
            "Delete Cert", "Issuer", "2024-01-01", null,
            "CRED-DEL", "https://example.com/del", "To be deleted");
        var createResponse = await client.PostAsync("/api/certifications", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCert = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var certId = createdCert.GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/certifications/{certId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "DELETE /api/certifications/{id} returns Unauthorized without authentication token")]
    public async Task DeleteCertification_NoAuthToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync($"/api/certifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenHelper.GenerateToken("Admin.View", "Certifications.Contributor");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static MultipartFormDataContent CreateCertificationFormContent(
        string title, string issuer, string issueDate, string? expiryDate,
        string credentialId, string credentialUrl, string description)
    {
        var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(title), "title");
        formContent.Add(new StringContent(issuer), "issuer");
        formContent.Add(new StringContent(issueDate), "issueDate");
        if (expiryDate != null)
        {
            formContent.Add(new StringContent(expiryDate), "expiryDate");
        }
        formContent.Add(new StringContent(credentialId), "credentialId");
        formContent.Add(new StringContent(credentialUrl), "credentialUrl");
        formContent.Add(new StringContent(description), "description");

        var imageBytes = CreateMinimalPngBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(imageContent, "image", "test-cert-image.png");

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
