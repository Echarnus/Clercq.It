using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Npgsql;
using Respawn;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Runs the real API against PostgreSQL and Keycloak containers (issue #244).
/// The database gets the real EF migrations; Keycloak imports the local realm
/// (src/Clercq.It.AppHost/KeycloakRealms) so tests use real tokens for real users.
/// Only object storage is faked. Shared by every test class in the "Api" collection;
/// call <see cref="ResetDatabaseAsync"/> to start a test from an empty database.
/// See docs/testing.md#api-integration-tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Realm = "clercqit";
    public const string WebClientId = "clercqit-web";
    public const string FakeImageUrl = "https://test-storage.example.com/test-image.png";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private readonly KeycloakContainer _keycloak = new KeycloakBuilder()
        .WithImage("keycloak/keycloak:26.4")
        .WithResourceMapping(
            new FileInfo(Path.Combine(AppContext.BaseDirectory, "Keycloak", "clercqit-realm.json")),
            "/opt/keycloak/data/import/")
        .WithCommand("--import-realm")
        .Build();

    private readonly ConcurrentDictionary<string, string> _tokens = new();
    private Respawner? _respawner;

    public string KeycloakUrl => _keycloak.GetBaseAddress().TrimEnd('/');

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _keycloak.StartAsync());

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _keycloak.DisposeAsync();
    }

    /// <summary>Deletes all rows except the migrations history.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    /// <summary>
    /// A client that sends a real access token for a realm user: admin (every role),
    /// blogger, projects, certs (one contributor role each) or test (no roles).
    /// </summary>
    public async Task<HttpClient> CreateClientForUserAsync(string username)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetTokenAsync(username));
        return client;
    }

    public async Task<string> GetTokenAsync(string username)
    {
        if (_tokens.TryGetValue(username, out var cached))
        {
            return cached;
        }

        // Local realm users have their username as password (dev-only realm).
        using var http = new HttpClient();
        var response = await http.PostAsync(
            $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = WebClientId,
                ["username"] = username,
                ["password"] = username,
            }));
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        return _tokens[username] = token;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production code paths; only the external services point at the containers.
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        // The Aspire-style service reference marks Keycloak as local, so plain HTTP metadata is allowed.
        builder.UseSetting("services:keycloak:http:0", KeycloakUrl);
        builder.UseSetting("Keycloak:Realm", Realm);
        builder.UseSetting("Keycloak:ClientId", WebClientId);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IObjectStorageService>();

            var storage = new Mock<IObjectStorageService>();
            storage
                .Setup(s => s.UploadFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(FakeImageUrl);
            storage
                .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(storage.Object);

            // Every test class shares one app, so give each request its own client IP:
            // otherwise the per-IP rate limits (100/min global, 30/min API) trip mid-run.
            services.AddSingleton<IStartupFilter, UniqueClientIpStartupFilter>();
        });
    }

    private sealed class UniqueClientIpStartupFilter : IStartupFilter
    {
        private static int _counter;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                var n = Interlocked.Increment(ref _counter);
                context.Connection.RemoteIpAddress = new IPAddress([10, (byte)(n >> 16), (byte)(n >> 8), (byte)n]);
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}

[CollectionDefinition(Name)]
public class ApiTestGroup : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
