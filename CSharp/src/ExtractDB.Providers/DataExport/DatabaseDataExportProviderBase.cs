using System.Data.Common;
using System.Globalization;
using System.Text;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;

namespace ExtractDB.Providers.DataExport;

public abstract class DatabaseDataExportProviderBase : IDataExportProvider
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private readonly IConnectionStringBuilder connectionStringBuilder;

    protected DatabaseDataExportProviderBase(IConnectionStringBuilder connectionStringBuilder)
        => this.connectionStringBuilder = connectionStringBuilder;

    public async Task<GenerationResult> ExportAsync(
        DataExportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = new List<GenerationMessage>();
        var successCount = 0;
        var tables = ResolveTables(request, messages);
        var orderedTables = OrderTables(tables, request.Metadata, messages);

        await using var connection = CreateConnection(connectionStringBuilder.Build(request.ConnectionOptions));
        await connection.OpenAsync(cancellationToken);

        foreach (var table in orderedTables)
        {
            try
            {
                var exportableColumns = table.Columns
                    .Where(IsExportableColumn)
                    .OrderBy(column => column.OrdinalPosition)
                    .ToArray();

                if (exportableColumns.Length == 0)
                {
                    messages.Add(CreateMessage(
                        GenerationMessageSeverity.Warning,
                        table.Schema,
                        table.Name,
                        "DataExport",
                        "Table has no exportable columns."));
                    continue;
                }

                var rows = await ReadRowsAsync(
                    connection,
                    table,
                    exportableColumns,
                    request.Configuration.MaxRowsPerTable,
                    cancellationToken);

                if (rows.Count > request.Configuration.MaxRowsPerTable)
                {
                    messages.Add(CreateMessage(
                        GenerationMessageSeverity.Error,
                        table.Schema,
                        table.Name,
                        "DataExport",
                        $"Table has more than {request.Configuration.MaxRowsPerTable} rows and was not exported."));
                    continue;
                }

                var sql = BuildInsertScript(table, exportableColumns, rows);
                WriteFile(GetDataScriptPath(request.OutputDirectory, table.Schema, table.Name), sql);
                successCount++;
            }
            catch (Exception ex)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Error,
                    table.Schema,
                    table.Name,
                    "DataExport",
                    ex.Message));
            }
        }

        return new GenerationResult
        {
            SuccessCount = successCount,
            WarningCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Warning),
            ErrorCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Error),
            Messages = messages
        };
    }

    protected abstract DbConnection CreateConnection(string connectionString);

    protected abstract string QuoteIdentifier(string identifier);

    protected abstract string BuildSelectSql(TableMetadata table, IReadOnlyList<ColumnMetadata> columns, int maxRows);

    protected virtual string FormatBoolean(bool value)
        => value ? "1" : "0";

    protected virtual string FormatBytes(byte[] value)
        => "0x" + Convert.ToHexString(value);

    protected virtual string QualifyTable(TableMetadata table)
        => string.IsNullOrWhiteSpace(table.Schema)
            ? QuoteIdentifier(table.Name)
            : $"{QuoteIdentifier(table.Schema)}.{QuoteIdentifier(table.Name)}";

    private static IReadOnlyList<TableMetadata> ResolveTables(
        DataExportRequest request,
        ICollection<GenerationMessage> messages)
    {
        var tables = new List<TableMetadata>();

        foreach (var configuredTable in Flatten(request.Configuration))
        {
            var table = request.Metadata.Tables.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, configuredTable.Name, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(configuredTable.Schema)
                    || string.Equals(candidate.Schema, configuredTable.Schema, StringComparison.OrdinalIgnoreCase)));

            if (table is null)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Warning,
                    configuredTable.Schema,
                    configuredTable.Name,
                    "DataExportConfig",
                    "Configured table was not found in metadata."));
                continue;
            }

            if (!tables.Any(existing =>
                    string.Equals(existing.Schema, table.Schema, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.Name, table.Name, StringComparison.OrdinalIgnoreCase)))
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    private static IReadOnlyList<DataExportTable> Flatten(DataExportConfiguration configuration)
        => configuration.Tables
            .SelectMany(group => group.Name
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new DataExportTable(group.Schema, name)))
            .ToArray();

    private static IReadOnlyList<TableMetadata> OrderTables(
        IReadOnlyList<TableMetadata> tables,
        DatabaseMetadata metadata,
        ICollection<GenerationMessage> messages)
    {
        var remaining = tables.ToList();
        var ordered = new List<TableMetadata>();

        while (remaining.Count > 0)
        {
            var ready = remaining.FirstOrDefault(table => table.ForeignKeys.All(foreignKey =>
                !IsSelectedTable(foreignKey.TargetSchema, foreignKey.TargetTable, tables)
                || IsSelectedTable(foreignKey.TargetSchema, foreignKey.TargetTable, ordered)));

            if (ready is null)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Warning,
                    null,
                    metadata.DatabaseName,
                    "DataExportOrder",
                    "Could not fully order configured tables by foreign keys; JSON order was preserved for the remaining tables."));
                ordered.AddRange(remaining);
                break;
            }

            ordered.Add(ready);
            remaining.Remove(ready);
        }

        return ordered;
    }

    private static bool IsSelectedTable(
        string? schema,
        string table,
        IEnumerable<TableMetadata> tables)
        => tables.Any(candidate =>
            string.Equals(candidate.Name, table, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(schema)
                || string.Equals(candidate.Schema, schema, StringComparison.OrdinalIgnoreCase)));

    private static bool IsExportableColumn(ColumnMetadata column)
        => !column.IsComputed
            && column.ValueGeneration?.Strategy is not (ValueGenerationStrategy.Identity
                or ValueGenerationStrategy.Sequence
                or ValueGenerationStrategy.TriggerSequence);

    private async Task<IReadOnlyList<object?[]>> ReadRowsAsync(
        DbConnection connection,
        TableMetadata table,
        IReadOnlyList<ColumnMetadata> columns,
        int maxRows,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = BuildSelectSql(table, columns, maxRows);

        var rows = new List<object?[]>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new object?[columns.Count];

            for (var i = 0; i < columns.Count; i++)
            {
                values[i] = await reader.IsDBNullAsync(i, cancellationToken)
                    ? null
                    : reader.GetValue(i);
            }

            rows.Add(values);
        }

        return rows;
    }

    private string BuildInsertScript(
        TableMetadata table,
        IReadOnlyList<ColumnMetadata> columns,
        IReadOnlyList<object?[]> rows)
    {
        if (rows.Count == 0)
        {
            return string.Empty;
        }

        var source = new StringBuilder();
        var columnList = string.Join(", ", columns.Select(column => QuoteIdentifier(column.Name)));
        var tableName = QualifyTable(table);

        foreach (var row in rows)
        {
            var values = row.Select(FormatValue);
            source.Append("INSERT INTO ");
            source.Append(tableName);
            source.Append(" (");
            source.Append(columnList);
            source.Append(") VALUES (");
            source.Append(string.Join(", ", values));
            source.AppendLine(");");
        }

        return source.ToString();
    }

    private string FormatValue(object? value)
        => value switch
        {
            null => "NULL",
            string text => $"'{EscapeString(text)}'",
            char character => $"'{EscapeString(character.ToString())}'",
            bool boolean => FormatBoolean(boolean),
            byte[] bytes => FormatBytes(bytes),
            Guid guid => $"'{guid}'",
            DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fffffff}'",
            DateOnly date => $"'{date:yyyy-MM-dd}'",
            TimeOnly time => $"'{time:HH:mm:ss.fffffff}'",
            DateTimeOffset dateTimeOffset => $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.fffffff zzz}'",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "NULL",
            _ => $"'{EscapeString(value.ToString() ?? string.Empty)}'"
        };

    private static string EscapeString(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static string GetDataScriptPath(
        string outputDirectory,
        string? schema,
        string tableName)
    {
        var parts = string.IsNullOrWhiteSpace(schema)
            ? [outputDirectory, "Scripts", "Data", $"{tableName}.sql"]
            : new[] { outputDirectory, "Scripts", "Data", schema, $"{tableName}.sql" };

        return Path.Combine(parts);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.ReplaceLineEndings("\r\n"), Utf8NoBom);
    }

    private static GenerationMessage CreateMessage(
        GenerationMessageSeverity severity,
        string? schema,
        string objectName,
        string stage,
        string message)
        => new()
        {
            Severity = severity,
            ObjectType = DatabaseObjectType.Table,
            Schema = schema,
            ObjectName = objectName,
            Stage = stage,
            Message = message
        };
}
