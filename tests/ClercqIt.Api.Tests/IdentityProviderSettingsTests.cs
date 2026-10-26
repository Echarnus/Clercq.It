using Clercq.It.Api.Features.Auth;
using FluentAssertions;

namespace ClercqIt.Api.Tests;

public class IdentityProviderSettingsTests
{
    [Fact(DisplayName = "Cloud IAM settings link the realm console and the Cloud IAM site")]
    public void Create_CloudIam_BuildsConsoleUrls()
    {
        var settings = IdentityProviderSettings.Create(
            "https://lemur-16.cloud-iam.com/auth/", "clercqit", isLocal: false, "clercqit-web", "clercqit-mcp");

        settings.Provider.Should().Be("Cloud IAM (hosted Keycloak)");
        settings.BaseUrl.Should().Be("https://lemur-16.cloud-iam.com/auth");
        settings.Authority.Should().Be("https://lemur-16.cloud-iam.com/auth/realms/clercqit");
        settings.AdminConsoleUrl.Should().Be("https://lemur-16.cloud-iam.com/auth/admin/clercqit/console/");
        settings.AccountConsoleUrl.Should().Be("https://lemur-16.cloud-iam.com/auth/realms/clercqit/account/");
        settings.ProviderConsoleUrl.Should().Be(IdentityProviderSettings.CloudIamConsoleUrl);
    }

    [Fact(DisplayName = "Local Keycloak settings have no Cloud IAM link")]
    public void Create_Local_HasNoProviderConsole()
    {
        var settings = IdentityProviderSettings.Create(
            "http://localhost:8080", "clercqit", isLocal: true, "clercqit-web", "clercqit-mcp");

        settings.Provider.Should().Be("Local Keycloak (Aspire)");
        settings.ProviderConsoleUrl.Should().BeNull();
        settings.Roles.Should().HaveCount(5);
    }
}
