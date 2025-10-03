# Test with a simple resource that requires minimal permissions
resource "scaleway_vpc" "test_vpc" {
  name   = "test-vpc"
  region = "fr-par"
  tags   = ["test"]
}

output "vpc_id" {
  value = scaleway_vpc.test_vpc.id
}
