# Documentation

Every PR keeps these pages current (see [Contributing](./contributing.md#docs-rule)).

| Page | What it covers |
| --- | --- |
| [Contributing](./contributing.md) | Issue → branch → PR → required checks → auto-merge → deploy loop, labels, docs rule, enforced rules (rulesets, PR conventions, weekly audit) |
| [Architecture](./architecture.md) | Layers, API endpoints and roles, database tables, authentication, the production container |
| [Development](./development.md) | Local setup with Aspire, Keycloak test users, code style and analyzers, frontend lint/format/types and hooks, troubleshooting |
| [Testing](./testing.md) | Unit test stack and conventions, how to run tests, coverage gate and thresholds |
| [DevOps](./devops.md) | GitHub Actions pipelines, deployment, required secrets |
| [Secrets](./secrets.md) | Scaleway Secret Manager: which secrets exist, who can read them, rotation |
| [Versioning](./versioning.md) | GitVersion and branching strategy |
| [Migrations](./migrations.md) | EF Core migrations and how they reach production |
| [Cloud IAM](./ciam.md) | Production Keycloak (Cloud IAM) realm, clients and roles |
| [Features](./features.md) | Site features overview |
| [SEO](./seo.md) | Readable detail URLs, redirects from old GUID URLs, page metadata, Open Graph, JSON-LD, sitemap and robots.txt |
| [UX and page layout](./ux.md) | Layout rules for the public site (one content width), the UX review and its open findings |
| [MCP](./mcp.md) | Content MCP server at `/api/mcp`: tools, Keycloak auth, adding it as a Claude connector |
| [Visitor statistics](./statistics.md) | Cookieless page view and visitor counting, rollup and retention, the admin Statistics tab |
| [MyZen](./myzen.md) | Connecting the owner's MyZen account: login flow, session, keep-alive, managing time entries, API |
| [Content](./content.md) | Adding projects, blog posts and certifications as Markdown files through PRs |
| [Portfolio feature](./portfolio-feature.md) | Projects / portfolio pages |
| [Portfolio implementation summary](./portfolio-implementation-summary.md) | How the portfolio feature was built |
| [Images and object storage](./deployment-blog-creation.md) | Where images are stored, buckets and keys per environment, local MinIO, troubleshooting |
| [Application security](./application-security.md) | Application-level security controls |
| [Security compliance](./security-compliance.md) | Framework alignment (NIST, ISO 27001, PCI DSS) |
