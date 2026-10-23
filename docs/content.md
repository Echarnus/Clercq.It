# Content in the repository

Projects, blog posts and certifications can be added with a pull request instead of the admin UI. Each item is a Markdown file under `content/`. After the PR merges, the deploy pipeline imports it into the database and it shows on the site.

```
content/
├── projects/        one file per project
├── blogs/           one file per blog post
├── certifications/  one file per certification
└── images/          images referenced by the files above
```

## File format

A file is YAML front matter between `---` lines, then a Markdown body. The file name without `.md` is the **slug**: it identifies the item, so renaming a file creates a new item.

### Projects (`content/projects/<slug>.md`)

| Field | Required | Notes |
|-------|----------|-------|
| `title` | yes | Max 200 characters |
| `shortDescription` | yes | Max 500 characters, shown on cards |
| `startDate` | yes | `yyyy-MM-dd` |
| `endDate` | no | `yyyy-MM-dd`, defaults to `startDate` |
| `featured` | no | `true` shows it on the homepage, default `false` |
| `image` | no | See [Images](#images), default `/placeholder.svg` |
| `skills` | no | List of strings |
| body | no | Markdown long description, defaults to `shortDescription` |

```markdown
---
title: "ClercqIt"
shortDescription: "My personal website."
startDate: 2025-06-28
featured: true
image: images/clercqit.png
skills:
  - .NET
  - Next.js
---
The long description, in **Markdown**.
```

### Blog posts (`content/blogs/<slug>.md`)

| Field | Required | Notes |
|-------|----------|-------|
| `title` | yes | Max 200 characters, shown as the heading |
| `shortDescription` | yes | Summary |
| `createdDate` | yes | `yyyy-MM-dd` |
| `publishDate` | no | `yyyy-MM-dd`, defaults to `createdDate` |
| `image` | no | See [Images](#images) |
| `tags` | no | List of strings |
| body | no | Markdown post, defaults to `shortDescription` |

### Certifications (`content/certifications/<slug>.md`)

| Field | Required | Notes |
|-------|----------|-------|
| `title` | yes | |
| `issuer` | yes | |
| `issueDate` | yes | `yyyy-MM-dd` |
| `expiryDate` | no | `yyyy-MM-dd` |
| `credentialId` | no | |
| `credentialUrl` | no | |
| `image` | no | See [Images](#images) |
| body | no | Markdown description |

## How bodies render

The site renders bodies as GitHub Flavored Markdown: headings, emphasis, lists, links, code blocks and tables. Raw HTML is shown as text, not rendered. Links to other sites open in a new tab. See [UX and page layout](./ux.md#markdown).

## Images

`image` can be:

- a path relative to `content/`, e.g. `images/clercqit.png`. The importer uploads it to the `clercq-it-blog-images` bucket under `content/<hash>/<file name>` and stores the public URL. The hash comes from the file bytes, so an unchanged image is not uploaded again, and a changed image gets a new URL.
- a site path such as `/placeholder.svg`, or a full `https://` URL. These are stored as they are.

Paths outside `content/` are rejected.

## How the import runs

- Code: `src/Clercq.It.ContentImport` (a console app), tests in `tests/Clercq.It.ContentImport.Tests`.
- `deploy.yml` runs it after the database migrations on every automatic deploy: `dotnet run --project src/Clercq.It.ContentImport -- content`.
- Each file maps to a fixed id derived from its type and slug, so the import is an upsert. Running it again changes nothing; it logs `created / updated / unchanged` counts.
- Items created in the admin UI have random ids and are never touched by the import.
- Deleting a file does **not** delete the item. Delete it in the admin UI.
- If you edit an imported item in the admin UI, the next import overwrites the fields that differ from the file. The file is the source of truth for items that have one.
- All files are parsed before anything is written. One invalid file stops the import and fails the deploy, with the file name and the problem in the log.
- The `RepoContentTests` test imports the real `content/` folder in CI, so a broken file already fails the pull request.
- The project treats assembly version conflicts (MSB3277) as build errors. The test host loads mismatched assemblies that the real app cannot, so a conflict would otherwise pass CI and crash the deploy. `Clercq.It.Infrastructure` pins `Microsoft.EntityFrameworkCore.Relational` for the same reason; keep it on the same version as `Microsoft.EntityFrameworkCore`.

## Running it locally

Against the Aspire database (see [development.md](./development.md)):

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=clercqit_portfolio;Username=...;Password=..." \
dotnet run --project src/Clercq.It.ContentImport -- content
```

Local images also need `ObjectStorage__Endpoint`, `ObjectStorage__BucketName`, `ObjectStorage__AccessKey` and `ObjectStorage__SecretKey` (MinIO locally). Without them, content that only uses site paths or URLs still imports.
