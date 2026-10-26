using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

namespace Clercq.It.Api.Features.Mcp;

/// <summary>
/// The content MCP server (issue #386): Claude manages projects, certifications and
/// blog posts through it, with a Keycloak token that carries the contributor roles.
/// See docs/mcp.md.
/// </summary>
public static class McpEndpoints
{
    public const string Path = "/api/mcp";
    public const string MetadataPath = "/.well-known/oauth-protected-resource" + Path;
    public const string Policy = "McpContributor";

    // Same token validation as the API, but a 401 points the client at the OAuth
    // metadata (RFC 9728), so a Claude connector can find Keycloak and sign in.
    public const string BearerScheme = "McpBearer";

    public static AuthenticationBuilder AddMcpAuthentication(
        this AuthenticationBuilder auth,
        Action<JwtBearerOptions> configureBearer,
        string keycloakAuthority)
    {
        return auth
            .AddJwtBearer(BearerScheme, options =>
            {
                configureBearer(options);
                options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddMcp(options =>
            {
                // Relative on purpose: the handler serves the metadata only when the request URL
                // matches, and an absolute URI never matched behind nginx (http inside, https outside).
                // The 401 header and the resource use the request's origin, which is https once
                // UseForwardedHeaders has applied nginx's X-Forwarded-Proto (Program.cs).
                options.ResourceMetadataUri = new Uri(MetadataPath, UriKind.Relative);
                options.Events.OnResourceMetadataRequest = context =>
                {
                    var request = context.HttpContext.Request;
                    var origin = $"{request.Scheme}://{request.Host}";
                    context.ResourceMetadata = new()
                    {
                        Resource = origin + Path,
                        AuthorizationServers = { keycloakAuthority },
                        BearerMethodsSupported = { "header" },
                        ResourceName = "Clercq.It content",
                    };
                    return Task.CompletedTask;
                };
            });
    }

    public static IServiceCollection AddContentMcpServer(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<McpContent>();
        services.AddMcpServer()
            // Stateless: the serverless container scales to zero and can run several instances.
            .WithHttpTransport(options => options.Stateless = true)
            // Hides and refuses tools whose [Authorize] policy the caller does not meet.
            .AddAuthorizationFilters()
            .WithTools<ProjectTools>()
            .WithTools<CertificationTools>()
            .WithTools<BlogTools>()
            .WithTools<MyZenTools>();
        return services;
    }

    public static IEndpointRouteBuilder MapContentMcp(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMcp(Path)
            .RequireAuthorization(Policy)
            .RequireRateLimiting("api");
        return endpoints;
    }
}
