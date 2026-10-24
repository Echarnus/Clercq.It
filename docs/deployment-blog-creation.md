# Images and object storage

Project, blog and certification images are stored in Scaleway Object Storage (S3-compatible). This page replaces the original blog-creation deployment guide; the setup it described (account-wide keys passed as Terraform variables) is gone.

## How it works

- The admin UI, the MCP server (`imageBase64`, see [MCP](./mcp.md)) and the content import (see [Content](./content.md)) all upload through `IObjectStorageService` (`src/Clercq.It.Domain/Abstractions/IObjectStorageService.cs`), implemented by `src/Clercq.It.Infrastructure/Services/ObjectStorageService.cs` with the AWS S3 SDK.
- Each upload gets the key `<prefix>/<new GUID>/<file name>`, so names never collide. The API returns the public URL `<Endpoint>/<BucketName>/<key>`, and that URL is stored on the item.
- Accepted types: jpg, jpeg, png, gif and webp (FluentValidation in Application). nginx accepts request bodies up to 10 MB (`client_max_body_size` in `src/nginx.conf`); the MCP tools accept images up to 5 MB.
- Replacing or removing an image, or deleting an item, leaves the old object in the bucket. `DeleteFileAsync` exists but nothing calls it yet.
- `POST /api/images` (`Blogs.Contributor`) uploads an image for use inside Markdown and returns its URL.

## Infrastructure

| Environment | Bucket | Credentials |
| --- | --- | --- |
| Production | `clercq-it-blog-images` (public-read ACL), `infra/terraform/main.tf` | IAM application `clercq-it-app-storage` and its API key, `infra/terraform/iam.tf` |
| Verify | `clercq-it-verify-images` (public-read ACL), `infra/terraform/verify.tf` | IAM application `clercq-it-verify-storage` and its API key, `infra/terraform/verify.tf` |

Terraform creates the keys and passes them to the container as secret environment variables. Nobody creates or copies a key by hand. Each key can only reach its own bucket (bucket policies in `infra/terraform/storage_policies.tf`). See [Secrets](./secrets.md) for rotation.

## Configuration

| Setting | Production value |
| --- | --- |
| `ObjectStorage__Endpoint` | `https://s3.fr-par.scw.cloud` |
| `ObjectStorage__BucketName` | `clercq-it-blog-images` |
| `ObjectStorage__Region` | `fr-par` |
| `ObjectStorage__AccessKey`, `ObjectStorage__SecretKey` | From `scaleway_iam_api_key.app_storage` (secret env vars) |

Without these settings an upload fails with `Object storage is not configured`. Reading content still works.

Locally, the Aspire AppHost does not start object storage. Run MinIO with the command in the comment in `src/Clercq.It.AppHost/Program.cs`, and set the `ObjectStorage` settings for the API with `dotnet user-secrets` (endpoint `http://localhost:9100`, bucket `clercq-it-dev`).

## Check it

```bash
# Public read works (no auth)
curl -s https://www.clercq.it/api/blogs | head -c 300

# Writes need a token with the right role: expect 401
curl -s -o /dev/null -w '%{http_code}\n' -X POST https://www.clercq.it/api/images -F "image=@test.jpg"
```

Container logs are in Scaleway Cockpit (Observability → Cockpit → Logs, container `clercq-it-app`).

## Troubleshooting

- **`Object storage is not configured`**: an `ObjectStorage__*` setting is empty. In production, run **Deploy Infra** so Terraform sets them again.
- **401 or 403 on upload**: the token is missing or lacks the role (`Blogs.Contributor` for `/api/images`, the matching contributor role for project, blog and certification writes). See [Cloud IAM](./ciam.md).
- **413 Request Entity Too Large**: the body is over nginx's 10 MB limit.
- **S3 error during upload (403)**: check the bucket exists, the key's IAM policy has the objects-only permission sets, and the bucket policy in `infra/terraform/storage_policies.tf` lists that key ([Secrets](./secrets.md#who-can-reach-which-bucket)).
