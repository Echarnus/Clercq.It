namespace Clercq.It.Infrastructure.Configuration;

public class AuthenticationSettings
{
    public string JwtSecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Clercq.It";
    public string Audience { get; set; } = "Clercq.It.Api";
    public int ExpirationMinutes { get; set; } = 60;
}
