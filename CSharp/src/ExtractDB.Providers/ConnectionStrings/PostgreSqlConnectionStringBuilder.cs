using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using Npgsql;

namespace ExtractDB.Providers.ConnectionStrings;

public sealed class PostgreSqlConnectionStringBuilder : IConnectionStringBuilder
{
    public DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    public string Build(DatabaseConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Server,
            Port = options.Port,
            Database = options.Database,
            Username = options.UserName,
            Password = options.Password
        };

        return builder.ConnectionString;
    }
}
