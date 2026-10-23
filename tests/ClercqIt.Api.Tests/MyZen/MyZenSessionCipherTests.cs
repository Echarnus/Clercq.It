using Clercq.It.Infrastructure.MyZen;
using FluentAssertions;

namespace ClercqIt.Api.Tests.MyZen;

public class MyZenSessionCipherTests
{
    private static readonly MyZenSessionData Data = new(
        [new MyZenCookie(".AUTH", "secret", "myzen.example", "/", new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), true, true)],
        "form", "header", "check");

    [Fact(DisplayName = "Encrypted session data decrypts with the same key")]
    public void RoundTrip_SameKey_ReturnsData()
    {
        // Arrange
        var cipher = new MyZenSessionCipher("key-one");

        // Act
        var payload = cipher.Encrypt(Data);

        // Assert
        System.Text.Encoding.UTF8.GetString(payload).Should().NotContain("secret");
        cipher.Decrypt(payload).Should().BeEquivalentTo(Data);
    }

    [Fact(DisplayName = "Another key, a tampered or a short payload gives nothing")]
    public void Decrypt_WrongKeyOrTampered_ReturnsNull()
    {
        // Arrange
        var payload = new MyZenSessionCipher("key-one").Encrypt(Data);
        var tampered = (byte[])payload.Clone();
        tampered[^1] ^= 0xFF;

        // Act & Assert
        new MyZenSessionCipher("key-two").Decrypt(payload).Should().BeNull();
        new MyZenSessionCipher("key-one").Decrypt(tampered).Should().BeNull();
        new MyZenSessionCipher("key-one").Decrypt([1, 2, 3]).Should().BeNull();
    }
}
