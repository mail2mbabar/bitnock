namespace Blog.Infrastructure.Persistence;

internal static class PostgresConnectionString
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Connection string 'Database' is not configured.");
        }

        var raw = value.Trim().Trim('"');
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            raw = FromUri(raw);
        }

        if (!raw.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
            && (raw.Contains("render.com", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("dpg-", StringComparison.OrdinalIgnoreCase)))
        {
            raw = raw.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return raw;
    }

    private static string FromUri(string uri)
    {
        var parsed = new Uri(uri);
        var userInfo = parsed.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = Uri.UnescapeDataString(parsed.AbsolutePath.Trim('/'));
        var port = parsed.Port > 0 ? parsed.Port : 5432;
        var ssl = parsed.Query.Contains("sslmode=disable", StringComparison.OrdinalIgnoreCase)
            ? "SSL Mode=Disable"
            : "SSL Mode=Require;Trust Server Certificate=true";

        return string.Join(';',
            $"Host={parsed.Host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={user}",
            $"Password={password}",
            ssl);
    }
}
