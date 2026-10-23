using Amazon.S3;
using Amazon.S3.Model;
using Clercq.It.Infrastructure.Configuration;
using Clercq.It.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MoqIt = Moq.It;

namespace ClercqIt.Api.Tests;

public class ObjectStorageServiceTests
{
    private const string Endpoint = "https://s3.example.com";
    private const string Bucket = "blog-images-bucket";

    private readonly Mock<IAmazonS3> _s3 = new();

    private static IOptions<ObjectStorageSettings> Settings() =>
        Options.Create(new ObjectStorageSettings { Endpoint = Endpoint, BucketName = Bucket });

    [Fact]
    public async Task UploadFileAsync_NoS3Client_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new ObjectStorageService(Settings());

        // Act
        var act = () => service.UploadFileAsync("a.png", new MemoryStream(), "image/png");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false, "blog-images/")]
    [InlineData(true, "blog-images/inline/")]
    public async Task UploadFileAsync_ConfiguredClient_PutsPublicObjectAndReturnsItsUrl(bool isInlineImage, string prefix)
    {
        // Arrange
        PutObjectRequest? sent = null;
        _s3.Setup(s => s.PutObjectAsync(MoqIt.IsAny<PutObjectRequest>(), MoqIt.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new PutObjectResponse());
        var service = new ObjectStorageService(Settings(), _s3.Object);
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        var url = await service.UploadFileAsync("photo.png", content, "image/png", isInlineImage);

        // Assert
        sent.Should().NotBeNull();
        sent!.BucketName.Should().Be(Bucket);
        sent.Key.Should().StartWith(prefix).And.EndWith("/photo.png");
        sent.ContentType.Should().Be("image/png");
        sent.CannedACL.Should().Be(S3CannedACL.PublicRead);
        sent.InputStream.Should().BeSameAs(content);
        url.Should().Be($"{Endpoint}/{Bucket}/{sent.Key}");
    }

    [Fact]
    public async Task DeleteFileAsync_NoS3Client_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new ObjectStorageService(Settings());

        // Act
        var act = () => service.DeleteFileAsync($"{Endpoint}/{Bucket}/blog-images/x/a.png");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(Endpoint + "/" + Bucket + "/blog-images/1/a.png")]
    [InlineData("https://" + Bucket + ".s3.example.com/blog-images/1/a.png")]
    public async Task DeleteFileAsync_PathOrHostStyleUrl_DeletesTheObjectKey(string fileUrl)
    {
        // Arrange
        DeleteObjectRequest? sent = null;
        _s3.Setup(s => s.DeleteObjectAsync(MoqIt.IsAny<DeleteObjectRequest>(), MoqIt.IsAny<CancellationToken>()))
            .Callback<DeleteObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new DeleteObjectResponse());
        var service = new ObjectStorageService(Settings(), _s3.Object);

        // Act
        await service.DeleteFileAsync(fileUrl);

        // Assert
        sent.Should().NotBeNull();
        sent!.BucketName.Should().Be(Bucket);
        sent.Key.Should().Be("blog-images/1/a.png");
    }
}
