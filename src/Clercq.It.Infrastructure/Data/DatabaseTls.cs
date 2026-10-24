using System.Security.Cryptography.X509Certificates;
using Npgsql;

namespace Clercq.It.Infrastructure.Data;

/// <summary>
/// Pins the database's TLS certificate (issue #416). When <c>Database:CaCertificate</c>
/// holds the instance's PEM certificate, connections use <see cref="SslMode.VerifyCA"/>
/// against that certificate only, so a server that cannot show it is refused.
/// Without it the connection string's own SSL Mode applies. See docs/secrets.md#database-logins.
/// </summary>
public static class DatabaseTls
{
    public const string CaCertificateKey = "Database:CaCertificate";

    public static void Configure(NpgsqlDataSourceBuilder builder, string? caCertificatePem)
    {
        if (string.IsNullOrWhiteSpace(caCertificatePem))
        {
            return;
        }

        var certificate = X509Certificate2.CreateFromPem(caCertificatePem);
        builder.ConnectionStringBuilder.SslMode = SslMode.VerifyCA;
        builder.UseRootCertificate(certificate);
    }
}
