using ExtractDB.Core.Types;

namespace ExtractDB.Core.Contracts;

public sealed record DatabaseConnectionOptions
{
    public required DatabaseProvider Provider { get; init; }
    public required string Server { get; init; }
    public required int Port { get; init; }
    public required string Database { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }

    public static int GetDefaultPort(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.PostgreSql => 5432,
            DatabaseProvider.SqlServer => 1433,
            DatabaseProvider.Firebird => 3050,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
}
