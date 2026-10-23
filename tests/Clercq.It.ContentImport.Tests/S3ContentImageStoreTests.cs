using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Clercq.It.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MoqIt = Moq.It;

namespace Clercq.It.ContentImport.Tests;

public sealed class S3ContentImageStoreTests : IDisposable
{
    private const string Endpoint = "https://s3.example.com";
    private const string Bucket = "images";

    private readonly string _root = Directory.CreateTempSubdirectory("images-").FullName;
    private readonly Mock<IAmazonS3> _s3 = new();

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private S3ContentImageStore NewStore() => new(
        _s3.Object,
        Options.Create(new ObjectStorageSettings { Endpoint = Endpoint, BucketName = Bucket }),
        NullLogger<S3ContentImageStore>.Instance);

    private string WriteImage(string name)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, [1, 2, 3, 4]);
        return path;
    }

    private void ObjectIsMissing() =>
        _s3.Setup(s => s.GetObjectMetadataAsync(Bucket, MoqIt.IsAny<string>(), MoqIt.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

    [Fact]
    public async Task UploadAsync_ObjectAlreadyExists_ReturnsUrlWithoutUploading()
    {
        // Arrange
        _s3.Setup(s => s.GetObjectMetadataAsync(Bucket, MoqIt.IsAny<string>(), MoqIt.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse());
        var path = WriteImage("cover.png");

        // Act
        var url = await NewStore().UploadAsync(path, CancellationToken.None);

        // Assert
        url.Should().StartWith($"{Endpoint}/{Bucket}/content/").And.EndWith("/cover.png");
        _s3.Verify(s => s.PutObjectAsync(MoqIt.IsAny<PutObjectRequest>(), MoqIt.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("a.png", "image/png")]
    [InlineData("a.JPG", "image/jpeg")]
    [InlineData("a.jpeg", "image/jpeg")]
    [InlineData("a.gif", "image/gif")]
    [InlineData("a.webp", "image/webp")]
    [InlineData("a.svg", "image/svg+xml")]
    [InlineData("a.bin", "application/octet-stream")]
    public async Task UploadAsync_ObjectMissing_UploadsPublicObjectWithContentType(string fileName, string contentType)
    {
        // Arrange
        ObjectIsMissing();
        PutObjectRequest? sent = null;
        _s3.Setup(s => s.PutObjectAsync(MoqIt.IsAny<PutObjectRequest>(), MoqIt.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new PutObjectResponse());
        var path = WriteImage(fileName);

        // Act
        var url = await NewStore().UploadAsync(path, CancellationToken.None);

        // Assert
        sent.Should().NotBeNull();
        sent!.BucketName.Should().Be(Bucket);
        sent.Key.Should().MatchRegex($"^content/[0-9a-f]{{16}}/{System.Text.RegularExpressions.Regex.Escape(fileName)}$");
        sent.ContentType.Should().Be(contentType);
        sent.CannedACL.Should().Be(S3CannedACL.PublicRead);
        url.Should().Be($"{Endpoint}/{Bucket}/{sent.Key}");
    }

    [Fact]
    public async Task UploadAsync_SameBytes_UsesTheSameKey()
    {
        // Arrange
        ObjectIsMissing();
        var keys = new List<string>();
        _s3.Setup(s => s.PutObjectAsync(MoqIt.IsAny<PutObjectRequest>(), MoqIt.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((request, _) => keys.Add(request.Key))
            .ReturnsAsync(new PutObjectResponse());
        var path = WriteImage("same.png");

        // Act
        await NewStore().UploadAsync(path, CancellationToken.None);
        await NewStore().UploadAsync(path, CancellationToken.None);

        // Assert
        keys.Should().HaveCount(2).And.OnlyContain(k => k == keys[0]);
    }

    [Fact]
    public async Task UploadAsync_MetadataFailsOtherwise_Throws()
    {
        // Arrange
        _s3.Setup(s => s.GetObjectMetadataAsync(Bucket, MoqIt.IsAny<string>(), MoqIt.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Forbidden") { StatusCode = HttpStatusCode.Forbidden });
        var path = WriteImage("x.png");

        // Act
        var act = () => NewStore().UploadAsync(path, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AmazonS3Exception>();
        _s3.Verify(s => s.PutObjectAsync(MoqIt.IsAny<PutObjectRequest>(), MoqIt.IsAny<CancellationToken>()), Times.Never);
    }
}
