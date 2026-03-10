using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clercq.It.AppHost;

/// <summary>
/// Seeds Keycloak test users after the resource becomes ready.
/// Keycloak's --import-realm uses SKIP strategy by default, so users defined in the
/// realm JSON are only imported on first run. This seeder ensures users always exist
/// regardless of volume state.
/// </summary>
internal static class KeycloakUserSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void SeedUsersWhenReady(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<KeycloakResource> keycloak)
    {
        builder.Eventing.Subscribe<ResourceReadyEvent>(keycloak.Resource, async (evt, ct) =>
        {
            var logger = evt.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("KeycloakUserSeeder");

            try
            {
                await SeedUsersAsync(keycloak.Resource, logger, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed Keycloak users");
            }
        });
    }

    private static async Task SeedUsersAsync(KeycloakResource keycloak, ILogger logger, CancellationToken ct)
    {
        const string realm = "clercqit";

        var adminPassword = keycloak.AdminPasswordParameter is { } param
            ? await param.GetValueAsync(ct)
            : throw new InvalidOperationException("Keycloak admin password not available");

        using var http = new HttpClient { BaseAddress = new Uri($"http://localhost:8080") };

        // Get admin token
        var tokenResponse = await http.PostAsync(
            "/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = "admin-cli",
                ["username"] = "admin",
                ["password"] = adminPassword!,
                ["grant_type"] = "password"
            }), ct);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenDoc = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()!;

        http.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        // Get existing realm roles
        var rolesResponse = await http.GetFromJsonAsync<List<KeycloakRole>>($"/admin/realms/{realm}/roles", JsonOptions, ct)
            ?? [];
        var roleMap = rolesResponse.ToDictionary(r => r.Name, r => r);

        // Define test users
        var users = new[]
        {
            new TestUser("admin", "admin", "admin@clercq.it", "Admin", "User",
                ["Admin.View", "Blogs.Contributor", "Projects.Contributor", "Certifications.Contributor"]),
            new TestUser("blogger", "blogger", "blogger@clercq.it", "Blog", "Contributor",
                ["Admin.View", "Blogs.Contributor"]),
            new TestUser("projects", "projects", "projects@clercq.it", "Project", "Contributor",
                ["Admin.View", "Projects.Contributor"]),
            new TestUser("certs", "certs", "certs@clercq.it", "Certification", "Contributor",
                ["Admin.View", "Certifications.Contributor"]),
            new TestUser("test", "test", "test@clercq.it", "Test", "User", []),
        };

        foreach (var user in users)
        {
            await EnsureUserAsync(http, realm, user, roleMap, logger, ct);
        }

        logger.LogInformation("Keycloak user seeding completed - {Count} users ensured", users.Length);
    }

    private static async Task EnsureUserAsync(
        HttpClient http, string realm, TestUser user,
        Dictionary<string, KeycloakRole> roleMap, ILogger logger, CancellationToken ct)
    {
        var existingResponse = await http.GetFromJsonAsync<List<KeycloakUser>>(
            $"/admin/realms/{realm}/users?username={user.Username}&exact=true", JsonOptions, ct) ?? [];

        string userId;

        if (existingResponse.Count > 0)
        {
            userId = existingResponse[0].Id;

            // Reset password to ensure it matches
            await http.PutAsJsonAsync(
                $"/admin/realms/{realm}/users/{userId}/reset-password",
                new { type = "password", value = user.Password, temporary = false }, ct);

            logger.LogInformation("Reset password for existing user '{Username}'", user.Username);
        }
        else
        {
            // Create user
            var createResponse = await http.PostAsJsonAsync($"/admin/realms/{realm}/users", new
            {
                username = user.Username,
                email = user.Email,
                emailVerified = true,
                enabled = true,
                firstName = user.FirstName,
                lastName = user.LastName,
                credentials = new[] { new { type = "password", value = user.Password, temporary = false } }
            }, ct);

            createResponse.EnsureSuccessStatusCode();

            // Fetch the created user to get their ID
            var created = await http.GetFromJsonAsync<List<KeycloakUser>>(
                $"/admin/realms/{realm}/users?username={user.Username}&exact=true", JsonOptions, ct) ?? [];
            userId = created[0].Id;

            logger.LogInformation("Created user '{Username}'", user.Username);
        }

        // Assign roles (idempotent - Keycloak ignores already-assigned roles)
        if (user.Roles.Length > 0)
        {
            var rolesToAssign = user.Roles
                .Where(r => roleMap.ContainsKey(r))
                .Select(r => new { id = roleMap[r].Id, name = r })
                .ToArray();

            if (rolesToAssign.Length > 0)
            {
                await http.PostAsJsonAsync(
                    $"/admin/realms/{realm}/users/{userId}/role-mappings/realm",
                    rolesToAssign, ct);
            }
        }
    }

    private record TestUser(string Username, string Password, string Email, string FirstName, string LastName, string[] Roles);
    private record KeycloakRole(string Id, string Name);
    private record KeycloakUser(string Id, string Username);
}
