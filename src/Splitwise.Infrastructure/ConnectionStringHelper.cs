using Npgsql;

namespace Splitwise.Infrastructure;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Neon (і більшість хостингів) віддає рядок у вигляді URL: postgresql://user:pass@host/db?sslmode=require.
    /// Npgsql такий формат не розуміє, тому перетворюємо його на звичайний рядок підключення.
    /// Звичайні рядки підключення повертаються без змін.
    /// </summary>
    public static string Normalize(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
