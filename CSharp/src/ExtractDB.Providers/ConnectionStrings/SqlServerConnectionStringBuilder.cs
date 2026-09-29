using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using Microsoft.Data.SqlClient;

namespace ExtractDB.Providers.ConnectionStrings;

public sealed class SqlServerConnectionStringBuilder : IConnectionStringBuilder
{
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public string Build(DatabaseConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{options.Server},{options.Port}",
            InitialCatalog = options.Database,
            UserID = options.UserName,
            Password = options.Password,
            Encrypt = false,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}
