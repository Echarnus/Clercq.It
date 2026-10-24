# Content MCP server

The API has a remote [MCP](https://modelcontextprotocol.io) server at `https://www.clercq.it/api/mcp` (issue #386). Claude uses it to manage the site's projects, certifications and blog posts. It does the same as the admin UI, and writes to the same database.

## Endpoint

| | |
| --- | --- |
| URL | `/api/mcp` (Streamable HTTP, stateless) |
| Code | `src/ClercqIt.Api/Features/Mcp/` |
| SDK | `ModelContextProtocol.AspNetCore` (official C# SDK) |
| OAuth metadata | `/.well-known/oauth-protected-resource/api/mcp` (RFC 9728) |
| Rate limit | The `api` policy (30 requests per minute per IP), plus nginx `api_limit` |

The server is stateless, so it works with the serverless container scaling to zero and running more than one instance.

## Tools

| Content | Tools | Role |
| --- | --- | --- |
| Projects | `list_projects`, `get_project`, `create_project`, `update_project`, `delete_project` | `Projects.Contributor` |
| Certifications | `list_certifications`, `get_certification`, `create_certification`, `update_certification`, `delete_certification` | `Certifications.Contributor` |
| Blog posts | `list_blog_posts`, `get_blog_post`, `create_blog_post`, `update_blog_post`, `delete_blog_post` | `Blogs.Contributor` |

- The tools send the same MediatR commands as the admin endpoints, so the same FluentValidation rules apply. A validation failure comes back as a tool error with the messages.
- `update_*` tools change only the fields you pass. `update_certification` takes `clearExpiryDate: true` to remove an expiry date.
- Dates are `yyyy-MM-dd`. Descriptions and blog bodies are Markdown.
- Images go in as `imageBase64` plus `imageFileName` (jpg, jpeg, png, gif or webp, max 5 MB) and are uploaded to object storage like admin uploads. Certifications and blog posts need an image when created; projects fall back to a placeholder.
- Every create, update and delete is logged as `MCP <action> <type> <id> by <caller>`, where the caller is the Keycloak username, or the client id for a service account.

## Authentication

The server accepts the same Keycloak (Cloud IAM) access tokens as the rest of the API:

1. The endpoint requires a token with at least one of `Projects.Contributor`, `Certifications.Contributor` or `Blogs.Contributor`. No token or an invalid token gives 401; a token without those roles gives 403.
2. Each tool then checks its own role. A caller only sees, and can only call, the tools for their roles.

A 401 carries `WWW-Authenticate: Bearer resource_metadata="…/.well-known/oauth-protected-resource/api/mcp"`. That metadata names the Keycloak realm as the authorization server, so an MCP client such as a Claude connector can find the login page by itself. Production sets `Mcp__PublicBaseUrl` (`infra/terraform/main.tf`) because behind nginx the API only sees `http`. nginx routes `/.well-known/oauth-protected-resource` to the API (`src/nginx.conf`).

The Keycloak clients for the connector and for agents are described in [Cloud IAM](./ciam.md#mcp-clients).

## Adding the connector in Claude

Once the `clercqit-mcp` client exists in Cloud IAM:

1. In Claude, open **Settings → Connectors → Add custom connector**.
2. Name: `Clercq.It`. URL: `https://www.clercq.it/api/mcp`.
3. Under **Advanced settings**, set the OAuth Client ID to `clercqit-mcp` (no secret: it is a public PKCE client).
4. Click **Connect** and sign in with your Cloud IAM account. The account needs the contributor roles.

## Testing

`tests/ClercqIt.Api.Tests/McpEndpointTests.cs` runs the real MCP client against the API with real Keycloak tokens (see [Testing](./testing.md#api-integration-tests)): 401 with the metadata link, 403, per-role tool lists, and each tool.

To try it locally, run the Aspire stack ([Development](./development.md)), get a token for a local user, and point the MCP Inspector at `http://localhost:5000/api/mcp` with an `Authorization: Bearer <token>` header:

```bash
npx @modelcontextprotocol/inspector
```
