using System.Data.Common;
using ExtractDB.Core.Metadata;
using ExtractDB.Providers.ConnectionStrings;
using Microsoft.Data.SqlClient;

namespace ExtractDB.Providers.DataExport;

public sealed class SqlServerDataExportProvider : DatabaseDataExportProviderBase
{
    public SqlServerDataExportProvider()
        : base(new SqlServerConnectionStringBuilder())
    {
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    protected override string QuoteIdentifier(string identifier)
        => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

    protected override string BuildSelectSql(TableMetadata table, IReadOnlyList<ColumnMetadata> columns, int maxRows)
        => $"SELECT TOP ({maxRows + 1}) {string.Join(", ", columns.Select(column => QuoteIdentifier(column.Name)))} FROM {QualifyTable(table)}";

    protected override string FormatBoolean(bool value)
        => value ? "1" : "0";
}
