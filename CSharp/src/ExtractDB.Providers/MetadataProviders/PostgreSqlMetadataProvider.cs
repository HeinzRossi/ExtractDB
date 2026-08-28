using System.Data.Common;
using System.Text.RegularExpressions;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using Npgsql;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class PostgreSqlMetadataProvider : DatabaseMetadataProviderBase
{
    private static readonly Regex SequenceDefaultRegex = new(
        @"nextval\('(?<name>[^']+)'::regclass\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PostgreSqlMetadataProvider()
        : base(new PostgreSqlConnectionStringBuilder())
    {
    }

    public override string ProviderName => "PostgreSQL";

    public override DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    public override string DefaultSchema => "public";

    public override bool IsSystemSchema(string? schemaName)
        => string.Equals(schemaName, "pg_catalog", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaName, "information_schema", StringComparison.OrdinalIgnoreCase);

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return IsSystemSchema(schemaName);
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
            Message = "Connecting to PostgreSQL."
        });

        await using var connection = new NpgsqlConnection(BuildConnectionString(options));
        await connection.OpenAsync(cancellationToken);

        var schema = await ResolveDefaultSchemaAsync(connection, cancellationToken) ?? DefaultSchema;

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTables,
            Message = "Reading PostgreSQL tables."
        });
        var tables = await ReadTablesAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingColumns,
            Message = "Reading PostgreSQL columns."
        });
        var columns = await ReadColumnsAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingPrimaryKeys,
            Message = "Reading PostgreSQL primary keys."
        });
        var primaryKeys = await ReadPrimaryKeysAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingForeignKeys,
            Message = "Reading PostgreSQL foreign keys."
        });
        var foreignKeys = await ReadForeignKeysAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingSequences,
            Message = "Reading PostgreSQL sequences."
        });
        var sequences = await ReadSequencesAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingViews,
            Message = "Reading PostgreSQL views."
        });
        var views = await ReadViewsAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingProcedures,
            Message = "Reading PostgreSQL procedures."
        });
        var procedures = await ReadProceduresAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTriggers,
            Message = "Reading PostgreSQL triggers."
        });
        var triggers = await ReadTriggersAsync(connection, schema, cancellationToken);
        var enums = await ReadEnumsAsync(connection, schema, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.Completed,
            Message = "PostgreSQL metadata read completed."
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
            Triggers = triggers,
            Sequences = sequences,
            Enums = enums
        };
    }

    public static CommonDbType MapNativeType(
        string nativeType,
        string? udtName = null,
        string? userDefinedTypeKind = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeType);

        return nativeType.ToLowerInvariant() switch
        {
            "smallint" => CommonDbType.SmallInt,
            "integer" => CommonDbType.Integer,
            "bigint" => CommonDbType.BigInt,
            "numeric" or "decimal" => CommonDbType.Decimal,
            "real" => CommonDbType.Float,
            "double precision" => CommonDbType.Double,
            "boolean" => CommonDbType.Boolean,
            "character" => CommonDbType.Char,
            "character varying" => CommonDbType.VarChar,
            "text" => CommonDbType.Text,
            "date" => CommonDbType.Date,
            "time without time zone" or "time" => CommonDbType.Time,
            "timestamp without time zone" or "timestamp" => CommonDbType.DateTime,
            "bytea" => CommonDbType.Blob,
            "uuid" => CommonDbType.Guid,
            "json" or "jsonb" => CommonDbType.Json,
            "user-defined" when string.Equals(userDefinedTypeKind, "e", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(udtName) => CommonDbType.Enum,
            _ => CommonDbType.Unknown
        };
    }

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

    public static ValueGenerationMetadata BuildValueGeneration(
        string? identityGeneration,
        string? defaultExpression)
    {
        if (!string.IsNullOrWhiteSpace(identityGeneration))
        {
            return new ValueGenerationMetadata
            {
                Strategy = ValueGenerationStrategy.Identity
            };
        }

        var sequenceName = ExtractSequenceName(defaultExpression);

        return sequenceName is null
            ? new ValueGenerationMetadata { Strategy = ValueGenerationStrategy.None }
            : new ValueGenerationMetadata
            {
                Strategy = ValueGenerationStrategy.Sequence,
                SequenceName = sequenceName
            };
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    protected override async Task<string?> ResolveDefaultSchemaAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_schema()";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as string ?? DefaultSchema;
    }

    private static async Task<IReadOnlyList<TableMetadata>> ReadTablesAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select table_name
            from information_schema.tables
            where table_schema = @schema
              and table_type = 'BASE TABLE'
            order by table_name
            """;

        var rows = await QueryAsync(connection, sql, schema, reader => new TableMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Columns = []
        }, cancellationToken);

        return rows.Where(row => !IsInternal(schema, row.Name)).ToArray();
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<ColumnMetadata>>> ReadColumnsAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                c.table_name,
                c.column_name,
                c.ordinal_position,
                c.data_type,
                c.udt_name,
                c.character_maximum_length,
                c.numeric_precision,
                c.numeric_scale,
                c.is_nullable,
                c.column_default,
                c.identity_generation,
                c.is_generated,
                c.generation_expression,
                type_info.typtype
            from information_schema.columns c
            join information_schema.tables t
              on t.table_schema = c.table_schema
             and t.table_name = c.table_name
             and t.table_type = 'BASE TABLE'
            left join pg_namespace schema_info
              on schema_info.nspname = c.udt_schema
            left join pg_type type_info
              on type_info.typnamespace = schema_info.oid
             and type_info.typname = c.udt_name
            where c.table_schema = @schema
            order by c.table_name, c.ordinal_position
            """;

        var rows = await QueryAsync(connection, sql, schema, reader =>
        {
            var tableName = reader.GetString(0);
            var dataType = reader.GetString(3);
            var udtName = GetNullableString(reader, 4);
            var defaultExpression = GetNullableString(reader, 9);
            var isGenerated = string.Equals(GetNullableString(reader, 11), "ALWAYS", StringComparison.OrdinalIgnoreCase);

            return new
            {
                TableName = tableName,
                Column = new ColumnMetadata
                {
                    Name = reader.GetString(1),
                    OrdinalPosition = reader.GetInt32(2),
                    NativeType = dataType,
                    DbType = MapNativeType(dataType, udtName, GetNullableString(reader, 13)),
                    Length = GetNullableInt32(reader, 5),
                    Precision = GetNullableInt32(reader, 6),
                    Scale = GetNullableInt32(reader, 7),
                    IsUnicode = IsUnicode(dataType),
                    IsNullable = string.Equals(reader.GetString(8), "YES", StringComparison.OrdinalIgnoreCase),
                    IsComputed = isGenerated,
                    ComputedExpression = isGenerated ? GetNullableString(reader, 12) : null,
                    DefaultValue = ParseDefaultValue(defaultExpression),
                    ValueGeneration = BuildValueGeneration(GetNullableString(reader, 10), defaultExpression)
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
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                table_name,
                constraint_name,
                array_agg(column_name order by ordinal_position) as columns
            from information_schema.key_column_usage kcu
            where table_schema = @schema
              and exists (
                  select 1
                  from information_schema.table_constraints tc
                  where tc.constraint_schema = kcu.constraint_schema
                    and tc.constraint_name = kcu.constraint_name
                    and tc.table_schema = kcu.table_schema
                    and tc.table_name = kcu.table_name
                    and tc.constraint_type = 'PRIMARY KEY'
              )
            group by table_name, constraint_name
            """;

        var rows = await QueryAsync(connection, sql, schema, reader => new
        {
            TableName = reader.GetString(0),
            PrimaryKey = new PrimaryKeyMetadata
            {
                Name = reader.GetString(1),
                Columns = reader.GetFieldValue<string[]>(2)
            }
        }, cancellationToken);

        return rows.ToDictionary(row => row.TableName, row => row.PrimaryKey);
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<ForeignKeyMetadata>>> ReadForeignKeysAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                source_table.relname as source_table,
                constraint_info.conname,
                array_agg(source_column.attname order by source_key.ordinality) as source_columns,
                target_schema.nspname as target_schema,
                target_table.relname as target_table,
                array_agg(target_column.attname order by source_key.ordinality) as target_columns,
                bool_or(source_column.attnotnull = false) as is_nullable
            from pg_constraint constraint_info
            join pg_class source_table on source_table.oid = constraint_info.conrelid
            join pg_namespace source_schema on source_schema.oid = source_table.relnamespace
            join pg_class target_table on target_table.oid = constraint_info.confrelid
            join pg_namespace target_schema on target_schema.oid = target_table.relnamespace
            join unnest(constraint_info.conkey) with ordinality source_key(attnum, ordinality) on true
            join unnest(constraint_info.confkey) with ordinality target_key(attnum, ordinality)
              on target_key.ordinality = source_key.ordinality
            join pg_attribute source_column
              on source_column.attrelid = source_table.oid
             and source_column.attnum = source_key.attnum
            join pg_attribute target_column
              on target_column.attrelid = target_table.oid
             and target_column.attnum = target_key.attnum
            where constraint_info.contype = 'f'
              and source_schema.nspname = @schema
            group by
                source_table.relname,
                constraint_info.conname,
                target_schema.nspname,
                target_table.relname
            order by source_table.relname, constraint_info.conname
            """;

        var rows = await QueryAsync(connection, sql, schema, reader => new
        {
            SourceTable = reader.GetString(0),
            ForeignKey = new ForeignKeyMetadata
            {
                Name = reader.GetString(1),
                SourceSchema = schema,
                SourceTable = reader.GetString(0),
                SourceColumns = reader.GetFieldValue<string[]>(2),
                TargetSchema = reader.GetString(3),
                TargetTable = reader.GetString(4),
                TargetColumns = reader.GetFieldValue<string[]>(5),
                IsNullable = reader.GetBoolean(6)
            }
        }, cancellationToken);

        return rows
            .GroupBy(row => row.SourceTable)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ForeignKeyMetadata>)group.Select(row => row.ForeignKey).ToArray());
    }

    private static async Task<IReadOnlyList<SequenceMetadata>> ReadSequencesAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select sequence_name
            from information_schema.sequences
            where sequence_schema = @schema
            order by sequence_name
            """;

        return await QueryAsync(connection, sql, schema, reader => new SequenceMetadata
        {
            Schema = schema,
            Name = reader.GetString(0)
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<ViewMetadata>> ReadViewsAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select table_name, view_definition
            from information_schema.views
            where table_schema = @schema
            order by table_name
            """;

        return await QueryAsync(connection, sql, schema, reader => new ViewMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = GetNullableString(reader, 1) ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<ProcedureMetadata>> ReadProceduresAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select p.proname, pg_get_functiondef(p.oid)
            from pg_proc p
            join pg_namespace n on n.oid = p.pronamespace
            where n.nspname = @schema
              and p.prokind in ('f', 'p')
            order by p.proname
            """;

        return await QueryAsync(connection, sql, schema, reader => new ProcedureMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = reader.GetString(1)
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<TriggerMetadata>> ReadTriggersAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select trigger_info.tgname, pg_get_triggerdef(trigger_info.oid, true)
            from pg_trigger trigger_info
            join pg_class table_info on table_info.oid = trigger_info.tgrelid
            join pg_namespace schema_info on schema_info.oid = table_info.relnamespace
            where schema_info.nspname = @schema
              and not trigger_info.tgisinternal
            order by trigger_info.tgname
            """;

        return await QueryAsync(connection, sql, schema, reader => new TriggerMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Sql = reader.GetString(1)
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<EnumMetadata>> ReadEnumsAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select type_info.typname, array_agg(enum_info.enumlabel order by enum_info.enumsortorder)
            from pg_type type_info
            join pg_enum enum_info on enum_info.enumtypid = type_info.oid
            join pg_namespace schema_info on schema_info.oid = type_info.typnamespace
            where schema_info.nspname = @schema
            group by type_info.typname
            order by type_info.typname
            """;

        return await QueryAsync(connection, sql, schema, reader => new EnumMetadata
        {
            Schema = schema,
            Name = reader.GetString(0),
            Values = reader.GetFieldValue<string[]>(1)
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        NpgsqlConnection connection,
        string sql,
        string schema,
        Func<NpgsqlDataReader, T> map,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("schema", schema);

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(map(reader));
        }

        return rows;
    }

    private static int? GetNullableInt32(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static string? GetNullableString(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static bool IsUnicode(string nativeType)
        => nativeType is "character" or "character varying" or "text";

    private static DefaultValueKind GetDefaultValueKind(string expression)
    {
        if (ExtractSequenceName(expression) is not null)
        {
            return DefaultValueKind.Sequence;
        }

        if (expression.StartsWith("CURRENT_", StringComparison.OrdinalIgnoreCase)
            || expression.Equals("now()", StringComparison.OrdinalIgnoreCase)
            || expression.EndsWith("()", StringComparison.Ordinal))
        {
            return DefaultValueKind.SystemFunction;
        }

        if (expression.StartsWith('\'')
            || bool.TryParse(expression, out _)
            || decimal.TryParse(expression, out _))
        {
            return DefaultValueKind.Literal;
        }

        return DefaultValueKind.Expression;
    }

    private static string? ExtractSequenceName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var match = SequenceDefaultRegex.Match(expression);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups["name"].Value.Split('.').Last().Trim('"');
    }

    private static bool IsInternal(string schemaName, string objectName)
        => string.Equals(schemaName, "pg_catalog", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaName, "information_schema", StringComparison.OrdinalIgnoreCase)
            || objectName.StartsWith("pg_", StringComparison.OrdinalIgnoreCase);

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
