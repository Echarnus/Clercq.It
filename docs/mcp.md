# Content MCP server

The API has a remote [MCP](https://modelcontextprotocol.io) server at `https://www.clercq.it/api/mcp` (issue #386). Claude uses it to manage the site's projects, certifications and blog posts, and the owner's MyZen time entries (issue #394). It does the same as the admin UI, and writes to the same database or MyZen account.

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
| MyZen | `myzen_status`, `list_myzen_projects`, `search_myzen_projects`, `list_myzen_time_entries`, `get_myzen_time_entry`, `create_myzen_time_entry`, `update_myzen_time_entry`, `delete_myzen_time_entry` | `MyZen.Owner`, and only while the `MyZen` flag is on |

- The tools send the same MediatR commands as the admin endpoints, so the same FluentValidation rules apply. A validation failure comes back as a tool error with the messages.
- `update_*` tools change only the fields you pass. `update_certification` takes `clearExpiryDate: true` to remove an expiry date.
- Dates are `yyyy-MM-dd`. Descriptions and blog bodies are Markdown.
- Images go in as `imageBase64` plus `imageFileName` (jpg, jpeg, png, gif or webp, max 5 MB) and are uploaded to object storage like admin uploads. Certifications and blog posts need an image when created. Projects can have no image: `update_project` with `removeImage: true` clears it, and the site then shows no picture.
- The MyZen tools use the session connected on the admin tab ([MyZen](./myzen.md)); they cannot connect it. An expired session gives a tool error that says to reconnect. Each call writes one entry, and `search_myzen_projects` gives the `projectId` and `customerId` an entry needs. `update_myzen_time_entry` keeps the entry's customer unless `projectId` changes; then it takes the `customerId` passed with it.
- Every create, update and delete is logged as `MCP <action> <type> <id> by <caller>`, where the caller is the Keycloak username, or the client id for a service account.

## Authentication

The server accepts the same Keycloak (Cloud IAM) access tokens as the rest of the API:

1. The endpoint requires a token with at least one of `Projects.Contributor`, `Certifications.Contributor`, `Blogs.Contributor` or `MyZen.Owner`. No token or an invalid token gives 401; a token without those roles gives 403.
2. Each tool then checks its own role. A caller only sees, and can only call, the tools for their roles. The MyZen tools' policy (`MyZenMcp`) also needs the `MyZen` feature flag (`FeatureFlagRequirement`), because MCP tools have no endpoint filter.

A 401 carries `WWW-Authenticate: Bearer resource_metadata="…/.well-known/oauth-protected-resource/api/mcp"`. That metadata names the Keycloak realm as the authorization server, so an MCP client such as a Claude connector can find the login page by itself. Both URLs use the origin of the request, so the metadata works for `www.clercq.it` and `clercq.it` alike. Scaleway's edge terminates TLS, so nginx passes the edge's `X-Forwarded-Proto` on to the API, and `UseForwardedHeaders` in `src/ClercqIt.Api/Program.cs` makes the request `https`. nginx routes `/.well-known/oauth-protected-resource` to the API (`src/nginx.conf`).

The metadata URI is relative on purpose: the SDK's handler only serves the metadata when the request URL matches it, and an absolute `https://` URI never matched the `http` request behind nginx, so the link in the 401 was a 404 and Claude fell back to `/authorize` on the site.

The Keycloak clients for the connector and for agents are described in [Cloud IAM](./ciam.md#mcp-clients).

## Adding the connector in Claude

Once the `clercqit-mcp` client exists in Cloud IAM:

1. In Claude, open **Settings → Connectors → Add custom connector**.
2. Name: `Clercq.It`. URL: `https://www.clercq.it/api/mcp`.
3. Under **Advanced settings**, set the OAuth Client ID to `clercqit-mcp` (no secret: it is a public PKCE client).
4. Click **Connect** and sign in with your Cloud IAM account. The account needs the contributor roles, and `MyZen.Owner` for the MyZen tools.

## Testing

`tests/ClercqIt.Api.Tests/McpEndpointTests.cs` and `tests/ClercqIt.Api.Tests/MyZen/MyZenMcpToolsTests.cs` (with a mocked MyZen gateway) run the real MCP client against the API with real Keycloak tokens (see [Testing](./testing.md#api-integration-tests)): 401 with the metadata link, 403, per-role tool lists, and each tool.

To try it locally, run the Aspire stack ([Development](./development.md)), get a token for a local user, and point the MCP Inspector at `http://localhost:5001/api/mcp` (the Aspire API port; `5000` when you run the API on its own) with an `Authorization: Bearer <token>` header:

```bash
npx @modelcontextprotocol/inspector
```
