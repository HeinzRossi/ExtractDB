using System.Data;
using System.Data.Common;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using Microsoft.Data.SqlClient;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class SqlServerMetadataProvider : DatabaseMetadataProviderBase
{
    public SqlServerMetadataProvider()
        : base(new SqlServerConnectionStringBuilder())
    {
    }

    public override string ProviderName => "SQL Server";

    public override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public override string DefaultSchema => "dbo";

    public override bool IsSystemSchema(string? schemaName)
        => string.Equals(schemaName, "sys", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaName, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return IsSystemSchema(schemaName)
            || objectName.StartsWith("sys", StringComparison.OrdinalIgnoreCase);
    }

    public override async Task<DatabaseMetadata> ReadAsync(
        DatabaseConnectionOptions options,
        IProgress<MetadataProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.Connecting,
            Message = "Connecting to SQL Server."
        });

        await using var connection = new SqlConnection(BuildConnectionString(options));
        await connection.OpenAsync(cancellationToken);

        var schema = await ResolveDefaultSchemaAsync(connection, cancellationToken) ?? DefaultSchema;

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTables,
            Message = "Reading SQL Server tables."
        });
        var tables = await ReadTablesAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingColumns,
            Message = "Reading SQL Server columns."
        });
        var columns = await ReadColumnsAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingPrimaryKeys,
            Message = "Reading SQL Server primary keys."
        });
        var primaryKeys = await ReadPrimaryKeysAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingForeignKeys,
            Message = "Reading SQL Server foreign keys."
        });
        var foreignKeys = await ReadForeignKeysAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingViews,
            Message = "Reading SQL Server views."
        });
        var views = await ReadViewsAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingProcedures,
            Message = "Reading SQL Server procedures."
        });
        var procedures = await ReadProceduresAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTriggers,
            Message = "Reading SQL Server triggers."
        });
        var triggers = await ReadTriggersAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.Completed,
            Message = "SQL Server metadata read completed."
        });

        return new DatabaseMetadata
        {
            Provider = Provider,
            DatabaseName = options.Database,
            DefaultSchema = schema,
            Tables = tables
                .Select(table => table with
                {
                    Columns = MarkPrimaryKeyColumns(
                        columns.GetValueOrDefault(table.Name, []),
                        primaryKeys.GetValueOrDefault(table.Name)),
                    PrimaryKey = primaryKeys.GetValueOrDefault(table.Name),
                    ForeignKeys = foreignKeys.GetValueOrDefault(table.Name, [])
                })
                .ToArray(),
            Views = views,
            Procedures = procedures,
            Triggers = triggers
        };
    }

    public static CommonDbType MapNativeType(string nativeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeType);

        return nativeType.ToLowerInvariant() switch
        {
            "tinyint" or "smallint" => CommonDbType.SmallInt,
            "int" => CommonDbType.Integer,
            "bigint" => CommonDbType.BigInt,
            "decimal" or "numeric" or "money" or "smallmoney" => CommonDbType.Decimal,
            "real" => CommonDbType.Float,
            "float" => CommonDbType.Double,
            "bit" => CommonDbType.Boolean,
            "char" or "nchar" => CommonDbType.Char,
            "varchar" or "nvarchar" => CommonDbType.VarChar,
            "text" or "ntext" => CommonDbType.Text,
            "date" => CommonDbType.Date,
            "time" => CommonDbType.Time,
            "datetime" or "datetime2" or "smalldatetime" => CommonDbType.DateTime,
            "binary" or "varbinary" => CommonDbType.Binary,
            "image" => CommonDbType.Blob,
            "uniqueidentifier" => CommonDbType.Guid,
            _ => CommonDbType.Unknown
        };
    }

    public static bool IsUnicodeType(string nativeType)
        => nativeType.ToLowerInvariant() is "nchar" or "nvarchar" or "ntext";

    public static DefaultValueMetadata? ParseDefaultValue(string? rawExpression)
    {
        if (string.IsNullOrWhiteSpace(rawExpression))
        {
            return null;
        }

        var expression = rawExpression.Trim();

        return new DefaultValueMetadata
        {
            RawExpression = expression,
            Kind = GetDefaultValueKind(expression)
        };
    }

    public static ValueGenerationMetadata BuildValueGeneration(bool isIdentity)
        => new()
        {
            Strategy = isIdentity ? ValueGenerationStrategy.Identity : ValueGenerationStrategy.None
        };

    protected override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    protected override async Task<string?> ResolveDefaultSchemaAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select schema_name()";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as string ?? DefaultSchema;
    }

    private static async Task<IReadOnlyList<TableMetadata>> ReadTablesAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select table_info.name
            from sys.tables table_info
            join sys.schemas schema_info on schema_info.schema_id = table_info.schema_id
            where schema_info.name = @schema
              and table_info.is_ms_shipped = 0
            order by table_info.name
            """;

        return await QueryAsync(connection, sql, schema, reader => new TableMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Columns = []
        }, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<ColumnMetadata>>> ReadColumnsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                table_info.name as table_name,
                column_info.name as column_name,
                column_info.column_id,
                type_info.name as type_name,
                column_info.max_length,
                column_info.precision,
                column_info.scale,
                column_info.is_nullable,
                column_info.is_identity,
                default_info.definition as default_definition,
                computed_info.definition as computed_definition
            from sys.columns column_info
            join sys.tables table_info on table_info.object_id = column_info.object_id
            join sys.schemas schema_info on schema_info.schema_id = table_info.schema_id
            join sys.types type_info
              on type_info.user_type_id = column_info.user_type_id
            left join sys.default_constraints default_info
              on default_info.parent_object_id = column_info.object_id
             and default_info.parent_column_id = column_info.column_id
            left join sys.computed_columns computed_info
              on computed_info.object_id = column_info.object_id
             and computed_info.column_id = column_info.column_id
            where schema_info.name = @schema
              and table_info.is_ms_shipped = 0
            order by table_info.name, column_info.column_id
            """;

        var rows = await QueryAsync(connection, sql, schema, reader =>
        {
            var nativeType = reader.GetString(3);
            var computedExpression = GetNullableString(reader, 10);

            return new
            {
                TableName = reader.GetString(0),
                Column = new ColumnMetadata
                {
                    Name = reader.GetString(1),
                    OrdinalPosition = reader.GetInt32(2),
                    NativeType = nativeType,
                    DbType = MapNativeType(nativeType),
                    Length = GetLength(nativeType, reader.GetInt16(4)),
                    Precision = reader.GetByte(5),
                    Scale = reader.GetByte(6),
                    IsUnicode = IsUnicodeType(nativeType),
                    IsNullable = reader.GetBoolean(7),
                    IsComputed = computedExpression is not null,
                    ComputedExpression = computedExpression,
                    DefaultValue = ParseDefaultValue(GetNullableString(reader, 9)),
                    ValueGeneration = BuildValueGeneration(reader.GetBoolean(8))
                }
            };
        }, cancellationToken);

        return rows
            .GroupBy(row => row.TableName)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ColumnMetadata>)group.Select(row => row.Column).ToArray());
    }

    private static async Task<IReadOnlyDictionary<string, PrimaryKeyMetadata>> ReadPrimaryKeysAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                table_info.name as table_name,
                key_info.name as key_name,
                column_info.name as column_name
            from sys.key_constraints key_info
            join sys.tables table_info on table_info.object_id = key_info.parent_object_id
            join sys.schemas schema_info on schema_info.schema_id = table_info.schema_id
            join sys.index_columns index_column
              on index_column.object_id = key_info.parent_object_id
             and index_column.index_id = key_info.unique_index_id
            join sys.columns column_info
              on column_info.object_id = index_column.object_id
             and column_info.column_id = index_column.column_id
            where key_info.type = 'PK'
              and schema_info.name = @schema
              and table_info.is_ms_shipped = 0
            order by table_info.name, key_info.name, index_column.key_ordinal
            """;

        var rows = await QueryAsync(connection, sql, schema, reader => new
        {
            TableName = reader.GetString(0),
            KeyName = reader.GetString(1),
            ColumnName = reader.GetString(2)
        }, cancellationToken);

        return rows
            .GroupBy(row => new { row.TableName, row.KeyName })
            .ToDictionary(
                group => group.Key.TableName,
                group => new PrimaryKeyMetadata
                {
                    Name = group.Key.KeyName,
                    Columns = group.Select(row => row.ColumnName).ToArray()
                });
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<ForeignKeyMetadata>>> ReadForeignKeysAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                source_table.name as source_table,
                foreign_key.name as foreign_key_name,
                source_column.name as source_column,
                target_schema.name as target_schema,
                target_table.name as target_table,
                target_column.name as target_column,
                source_column.is_nullable
            from sys.foreign_keys foreign_key
            join sys.tables source_table on source_table.object_id = foreign_key.parent_object_id
            join sys.schemas source_schema on source_schema.schema_id = source_table.schema_id
            join sys.tables target_table on target_table.object_id = foreign_key.referenced_object_id
            join sys.schemas target_schema on target_schema.schema_id = target_table.schema_id
            join sys.foreign_key_columns foreign_key_column
              on foreign_key_column.constraint_object_id = foreign_key.object_id
            join sys.columns source_column
              on source_column.object_id = foreign_key_column.parent_object_id
             and source_column.column_id = foreign_key_column.parent_column_id
            join sys.columns target_column
              on target_column.object_id = foreign_key_column.referenced_object_id
             and target_column.column_id = foreign_key_column.referenced_column_id
            where source_schema.name = @schema
              and foreign_key.is_ms_shipped = 0
              and source_table.is_ms_shipped = 0
            order by source_table.name, foreign_key.name, foreign_key_column.constraint_column_id
            """;

        var rows = await QueryAsync(connection, sql, schema, reader => new
        {
            SourceTable = reader.GetString(0),
            Name = reader.GetString(1),
            SourceColumn = reader.GetString(2),
            TargetSchema = reader.GetString(3),
            TargetTable = reader.GetString(4),
            TargetColumn = reader.GetString(5),
            SourceColumnIsNullable = reader.GetBoolean(6)
        }, cancellationToken);

        return rows
            .GroupBy(row => new { row.SourceTable, row.Name, row.TargetSchema, row.TargetTable })
            .GroupBy(
                group => group.Key.SourceTable,
                group => new ForeignKeyMetadata
                {
                    Name = group.Key.Name,
                    SourceSchema = schema,
                    SourceTable = group.Key.SourceTable,
                    SourceColumns = group.Select(row => row.SourceColumn).ToArray(),
                    TargetSchema = group.Key.TargetSchema,
                    TargetTable = group.Key.TargetTable,
                    TargetColumns = group.Select(row => row.TargetColumn).ToArray(),
                    IsNullable = group.Any(row => row.SourceColumnIsNullable)
                })
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ForeignKeyMetadata>)group.ToArray());
    }

    private static async Task<IReadOnlyList<ViewMetadata>> ReadViewsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select view_info.name, module_info.definition
            from sys.views view_info
            join sys.schemas schema_info on schema_info.schema_id = view_info.schema_id
            left join sys.sql_modules module_info on module_info.object_id = view_info.object_id
            where schema_info.name = @schema
              and view_info.is_ms_shipped = 0
            order by view_info.name
            """;

        return await QueryAsync(connection, sql, schema, reader => new ViewMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = GetNullableString(reader, 1) ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<ProcedureMetadata>> ReadProceduresAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select procedure_info.name, module_info.definition
            from sys.procedures procedure_info
            join sys.schemas schema_info on schema_info.schema_id = procedure_info.schema_id
            left join sys.sql_modules module_info on module_info.object_id = procedure_info.object_id
            where schema_info.name = @schema
              and procedure_info.is_ms_shipped = 0
            order by procedure_info.name
            """;

        return await QueryAsync(connection, sql, schema, reader => new ProcedureMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = GetNullableString(reader, 1) ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<TriggerMetadata>> ReadTriggersAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select trigger_info.name, module_info.definition
            from sys.triggers trigger_info
            join sys.tables table_info on table_info.object_id = trigger_info.parent_id
            join sys.schemas schema_info on schema_info.schema_id = table_info.schema_id
            left join sys.sql_modules module_info on module_info.object_id = trigger_info.object_id
            where schema_info.name = @schema
              and trigger_info.is_ms_shipped = 0
              and table_info.is_ms_shipped = 0
            order by trigger_info.name
            """;

        return await QueryAsync(connection, sql, schema, reader => new TriggerMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = GetNullableString(reader, 1) ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        SqlConnection connection,
        string sql,
        string schema,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@schema", SqlDbType.NVarChar, 128).Value = schema;

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(map(reader));
        }

        return rows;
    }

    private static int? GetLength(string nativeType, short maxLength)
    {
        if (maxLength < 0)
        {
            return null;
        }

        return nativeType.ToLowerInvariant() switch
        {
            "nchar" or "nvarchar" => maxLength / 2,
            "text" or "ntext" or "image" => null,
            _ => maxLength
        };
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DefaultValueKind GetDefaultValueKind(string expression)
    {
        var unwrapped = UnwrapSqlServerDefault(expression);

        if (unwrapped.StartsWith("NEXT VALUE FOR", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultValueKind.Sequence;
        }

        if (unwrapped.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            || unwrapped.StartsWith("getdate(", StringComparison.OrdinalIgnoreCase)
            || unwrapped.StartsWith("sysdatetime(", StringComparison.OrdinalIgnoreCase)
            || unwrapped.StartsWith("newid(", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultValueKind.SystemFunction;
        }

        if (unwrapped.StartsWith("N'", StringComparison.OrdinalIgnoreCase)
            || unwrapped.StartsWith('\'')
            || unwrapped is "0" or "1"
            || decimal.TryParse(unwrapped, out _))
        {
            return DefaultValueKind.Literal;
        }

        return DefaultValueKind.Expression;
    }

    private static string UnwrapSqlServerDefault(string expression)
    {
        var current = expression.Trim();

        while (current.Length > 1 && current[0] == '(' && current[^1] == ')')
        {
            current = current[1..^1].Trim();
        }

        return current;
    }

    private static IReadOnlyList<ColumnMetadata> MarkPrimaryKeyColumns(
        IReadOnlyList<ColumnMetadata> columns,
        PrimaryKeyMetadata? primaryKey)
    {
        if (primaryKey is null)
        {
            return columns;
        }

        return columns
            .Select(column => column with
            {
                IsPrimaryKey = primaryKey.Columns.Contains(column.Name)
            })
            .ToArray();
    }
}
