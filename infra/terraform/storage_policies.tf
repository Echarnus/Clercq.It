# =============================================================================
# Bucket policies (least privilege for object storage, issue #415)
# =============================================================================
#
# IAM can only grant object storage per project, so the container keys could
# otherwise reach every bucket in it, including the Terraform state that holds
# every secret. A bucket policy limits a bucket to the principals it lists
# (Scaleway's 2023-04-17 policy version allows only what is listed). So:
#
# - each image bucket: its own container key (objects only), Terraform, and
#   public reads for the site's images;
# - the state bucket: Terraform only.
#
# The Organization owner can always replace or delete a bucket policy, so a
# mistake here can be undone in the console. See docs/secrets.md.

# Whoever Terraform runs as (the deploy pipeline uses the same key for the
# production content import), looked up from its access key.
data "scaleway_iam_api_key" "terraform" {
  access_key = var.terraform_access_key
}

locals {
  terraform_principal = (
    try(data.scaleway_iam_api_key.terraform.application_id, "") != ""
    ? "application_id:${data.scaleway_iam_api_key.terraform.application_id}"
    : "user_id:${data.scaleway_iam_api_key.terraform.user_id}"
  )

  # What the site does with images: upload with a public-read ACL, check,
  # delete. ObjectStorageService and the content import need nothing more.
  image_object_actions = [
    "s3:GetObject",
    "s3:GetObjectAcl",
    "s3:PutObject",
    "s3:PutObjectAcl",
    "s3:DeleteObject",
  ]
}

resource "scaleway_object_bucket_policy" "blog_images" {
  bucket = scaleway_object_bucket.blog_images.name
  region = var.scaleway_region

  policy = jsonencode({
    Version = "2023-04-17"
    Id      = "clercq-it-blog-images"
    Statement = [
      {
        Sid       = "Terraform and the deploy pipeline manage the bucket"
        Effect    = "Allow"
        Principal = { SCW = local.terraform_principal }
        Action    = "*"
        Resource  = [scaleway_object_bucket.blog_images.name, "${scaleway_object_bucket.blog_images.name}/*"]
      },
      {
        Sid       = "The site container reads and writes images"
        Effect    = "Allow"
        Principal = { SCW = "application_id:${scaleway_iam_application.app_storage.id}" }
        Action    = concat(["s3:ListBucket"], local.image_object_actions)
        Resource  = [scaleway_object_bucket.blog_images.name, "${scaleway_object_bucket.blog_images.name}/*"]
      },
      {
        Sid       = "Everyone can view the images"
        Effect    = "Allow"
        Principal = "*"
        Action    = ["s3:GetObject"]
        Resource  = ["${scaleway_object_bucket.blog_images.name}/*"]
      },
    ]
  })
}

resource "scaleway_object_bucket_policy" "verify_images" {
  bucket = scaleway_object_bucket.verify_images.name
  region = var.scaleway_region

  policy = jsonencode({
    Version = "2023-04-17"
    Id      = "clercq-it-verify-images"
    Statement = [
      {
        Sid       = "Terraform manages the bucket"
        Effect    = "Allow"
        Principal = { SCW = local.terraform_principal }
        Action    = "*"
        Resource  = [scaleway_object_bucket.verify_images.name, "${scaleway_object_bucket.verify_images.name}/*"]
      },
      {
        Sid       = "The verify container and content import read and write images"
        Effect    = "Allow"
        Principal = { SCW = "application_id:${scaleway_iam_application.verify_storage.id}" }
        Action    = concat(["s3:ListBucket"], local.image_object_actions)
        Resource  = [scaleway_object_bucket.verify_images.name, "${scaleway_object_bucket.verify_images.name}/*"]
      },
      {
        Sid       = "Everyone can view the images"
        Effect    = "Allow"
        Principal = "*"
        Action    = ["s3:GetObject"]
        Resource  = ["${scaleway_object_bucket.verify_images.name}/*"]
      },
    ]
  })
}

# The state bucket is created outside Terraform (infra/scripts/setup-backend.sh)
# in this project. Only Terraform may touch it.
resource "scaleway_object_bucket_policy" "terraform_state" {
  bucket = "clercq-it-terraform-state"
  region = var.scaleway_region

  policy = jsonencode({
    Version = "2023-04-17"
    Id      = "clercq-it-terraform-state"
    Statement = [
      {
        Sid       = "Only Terraform reads and writes the state"
        Effect    = "Allow"
        Principal = { SCW = local.terraform_principal }
        Action    = "*"
        Resource  = ["clercq-it-terraform-state", "clercq-it-terraform-state/*"]
      },
    ]
  })
}
