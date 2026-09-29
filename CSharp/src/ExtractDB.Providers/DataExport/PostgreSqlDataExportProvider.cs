using System.Data.Common;
using ExtractDB.Core.Metadata;
using ExtractDB.Providers.ConnectionStrings;
using Npgsql;

namespace ExtractDB.Providers.DataExport;

public sealed class PostgreSqlDataExportProvider : DatabaseDataExportProviderBase
{
    public PostgreSqlDataExportProvider()
        : base(new PostgreSqlConnectionStringBuilder())
    {
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    protected override string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    protected override string BuildSelectSql(TableMetadata table, IReadOnlyList<ColumnMetadata> columns, int maxRows)
        => $"SELECT {string.Join(", ", columns.Select(column => QuoteIdentifier(column.Name)))} FROM {QualifyTable(table)} LIMIT {maxRows + 1}";

    protected override string FormatBoolean(bool value)
        => value ? "TRUE" : "FALSE";

    protected override string FormatBytes(byte[] value)
        => "'\\x" + Convert.ToHexString(value).ToLowerInvariant() + "'";
}
