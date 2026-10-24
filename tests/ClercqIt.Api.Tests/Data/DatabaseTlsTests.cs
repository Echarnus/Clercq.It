using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Clercq.It.Infrastructure.Data;
using FluentAssertions;
using Npgsql;

namespace ClercqIt.Api.Tests.Data;

public class DatabaseTlsTests
{
    private const string ConnectionString = "Host=db.example;Database=app;Username=app;Password=pw;SSL Mode=Require";

    [Fact(DisplayName = "A pinned certificate upgrades the connection to VerifyCA")]
    public void Configure_WithCertificate_UsesVerifyCa()
    {
        // Arrange
        var builder = new NpgsqlDataSourceBuilder(ConnectionString);

        // Act
        DatabaseTls.Configure(builder, SelfSignedPem());

        // Assert
        builder.ConnectionStringBuilder.SslMode.Should().Be(SslMode.VerifyCA);
        using var dataSource = builder.Build();
        dataSource.Should().NotBeNull();
    }

    [Theory(DisplayName = "Without a certificate the connection string's SSL Mode stays")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Configure_WithoutCertificate_KeepsSslMode(string? pem)
    {
        // Arrange
        var builder = new NpgsqlDataSourceBuilder(ConnectionString);

        // Act
        DatabaseTls.Configure(builder, pem);

        // Assert
        builder.ConnectionStringBuilder.SslMode.Should().Be(SslMode.Require);
    }

    [Fact(DisplayName = "A value that is not a PEM certificate fails at start-up")]
    public void Configure_InvalidPem_Throws()
    {
        // Arrange
        var builder = new NpgsqlDataSourceBuilder(ConnectionString);

        // Act
        var act = () => DatabaseTls.Configure(builder, "not a certificate");

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    private static string SelfSignedPem()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=rdb.example", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return certificate.ExportCertificatePem();
    }
}
