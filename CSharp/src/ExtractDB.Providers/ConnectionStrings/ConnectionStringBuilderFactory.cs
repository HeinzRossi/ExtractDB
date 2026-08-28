using ExtractDB.Core.Types;

namespace ExtractDB.Providers.ConnectionStrings;

public sealed class ConnectionStringBuilderFactory
{
    public IConnectionStringBuilder Create(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.PostgreSql => new PostgreSqlConnectionStringBuilder(),
            DatabaseProvider.SqlServer => new SqlServerConnectionStringBuilder(),
            DatabaseProvider.Firebird => new FirebirdConnectionStringBuilder(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
}
