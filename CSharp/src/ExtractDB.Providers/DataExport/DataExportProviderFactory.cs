using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;

namespace ExtractDB.Providers.DataExport;

public sealed class DataExportProviderFactory : IDataExportProviderFactory
{
    public IDataExportProvider Create(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.PostgreSql => new PostgreSqlDataExportProvider(),
            DatabaseProvider.SqlServer => new SqlServerDataExportProvider(),
            DatabaseProvider.Firebird => new FirebirdDataExportProvider(),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
}
