using System.Security.Cryptography;
using System.Text;

namespace Clercq.It.ContentImport;

/// <summary>
/// Derives a fixed Guid from a content type and slug, so re-importing a file
/// updates the same row. Admin UI rows use random Guids and never collide.
/// </summary>
public static class StableId
{
    public static Guid For(string type, string slug)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"clercq-it-content/{type}/{slug.ToLowerInvariant()}"));
        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // version 5 style
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant
        return new Guid(bytes);
    }
}
