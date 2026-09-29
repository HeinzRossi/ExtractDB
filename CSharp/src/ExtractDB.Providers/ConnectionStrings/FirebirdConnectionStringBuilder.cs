using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using FirebirdSql.Data.FirebirdClient;

namespace ExtractDB.Providers.ConnectionStrings;

public sealed class FirebirdConnectionStringBuilder : IConnectionStringBuilder
{
    public DatabaseProvider Provider => DatabaseProvider.Firebird;

    public string Build(DatabaseConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new FbConnectionStringBuilder
        {
            DataSource = options.Server,
            Port = options.Port,
            Database = options.Database,
            UserID = options.UserName,
            Password = options.Password,
            Charset = "UTF8"
        };

        return builder.ConnectionString;
    }
}
