using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Clercq.It.Infrastructure.MyZen;

/// <summary>
/// Encrypts the session payload with AES-GCM. The key is SHA-256 of the
/// configured session key, so any long random string works and nothing depends
/// on a key ring that a scaled-to-zero container would lose.
/// </summary>
public sealed class MyZenSessionCipher(string sessionKey)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(sessionKey));

    public byte[] Encrypt(MyZenSessionData data)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(data);
        var output = new byte[NonceSize + TagSize + plain.Length];
        var nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize));
        return output;
    }

    /// <summary>Decrypts a payload made by <see cref="Encrypt"/>.</summary>
    /// <returns>The data, or null when it was encrypted with another key or was tampered with.</returns>
    public MyZenSessionData? Decrypt(byte[] payload)
    {
        if (payload.Length < NonceSize + TagSize)
        {
            return null;
        }

        var plain = new byte[payload.Length - NonceSize - TagSize];
        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(payload.AsSpan(0, NonceSize), payload.AsSpan(NonceSize + TagSize),
                payload.AsSpan(NonceSize, TagSize), plain);
        }
        catch (AuthenticationTagMismatchException)
        {
            return null;
        }

        return JsonSerializer.Deserialize<MyZenSessionData>(plain);
    }
}
