using System.Data.Common;
using ExtractDB.Core.Metadata;
using ExtractDB.Providers.ConnectionStrings;
using FirebirdSql.Data.FirebirdClient;

namespace ExtractDB.Providers.DataExport;

public sealed class FirebirdDataExportProvider : DatabaseDataExportProviderBase
{
    public FirebirdDataExportProvider()
        : base(new FirebirdConnectionStringBuilder())
    {
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new FbConnection(connectionString);

    protected override string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    protected override string BuildSelectSql(TableMetadata table, IReadOnlyList<ColumnMetadata> columns, int maxRows)
        => $"SELECT {string.Join(", ", columns.Select(column => QuoteIdentifier(column.Name)))} FROM {QualifyTable(table)} ROWS {maxRows + 1}";

    protected override string FormatBoolean(bool value)
        => value ? "TRUE" : "FALSE";

    protected override string FormatBytes(byte[] value)
        => "x'" + Convert.ToHexString(value) + "'";
}
