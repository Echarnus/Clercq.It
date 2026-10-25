namespace Clercq.It.Api.Features.Auth;

/// <summary>
/// How the site's identity provider is set up, for the admin Settings page (issue #462),
/// so the owner can find the Cloud IAM console again. Only non-secret values: URLs,
/// the realm, client ids and role names. Never add passwords or client secrets here.
/// </summary>
public sealed record IdentityProviderSettings(
    string Provider,
    string BaseUrl,
    string Realm,
    string Authority,
    string AdminConsoleUrl,
    string AccountConsoleUrl,
    string? ProviderConsoleUrl,
    string WebClientId,
    string McpClientId,
    IReadOnlyList<IdentityProviderRole> Roles)
{
    /// <summary>The Cloud IAM site, where the owner signs in to manage the deployment itself.</summary>
    public const string CloudIamConsoleUrl = "https://www.cloud-iam.com";

    /// <summary>The realm roles the site checks (docs/ciam.md#4-create-realm-roles).</summary>
    public static readonly IReadOnlyList<IdentityProviderRole> SiteRoles =
    [
        new("Admin.View", "Open the admin dashboard, statistics and settings"),
        new("Blogs.Contributor", "Create, edit and delete blog posts and upload images"),
        new("Projects.Contributor", "Create, edit and delete projects"),
        new("Certifications.Contributor", "Create, edit and delete certifications"),
        new("MyZen.Owner", "Use the owner's MyZen account"),
    ];

    public static IdentityProviderSettings Create(
        string baseUrl, string realm, bool isLocal, string webClientId, string mcpClientId)
    {
        var trimmed = baseUrl.TrimEnd('/');
        var authority = $"{trimmed}/realms/{realm}";
        return new(
            isLocal ? "Local Keycloak (Aspire)" : "Cloud IAM (hosted Keycloak)",
            trimmed,
            realm,
            authority,
            $"{trimmed}/admin/{realm}/console/",
            $"{authority}/account/",
            isLocal ? null : CloudIamConsoleUrl,
            webClientId,
            mcpClientId,
            SiteRoles);
    }
}

public sealed record IdentityProviderRole(string Name, string Description);
