using System.Security.Cryptography;
using System.Text;

namespace Blog.Infrastructure.Security;

public static class SecretHasher
{
    public static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    public static string GenerateToken(int bytes = 32)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
    }

    public static string GenerateApiKey(out string prefix)
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var key = "nxa_" + secret;
        prefix = key.Length >= 12 ? key[..12] : key;
        return key;
    }
}
