using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using FirebirdSql.Data.FirebirdClient;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class FirebirdMetadataProvider : DatabaseMetadataProviderBase
{
    private static readonly string[] SystemObjectPrefixes = ["RDB$", "MON$", "SEC$"];

    private static readonly Regex NextValueForRegex = new(
        @"next\s+value\s+for\s+(?<name>[A-Za-z0-9_$""\.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GenIdRegex = new(
        @"gen_id\s*\(\s*(?<name>[A-Za-z0-9_$""\.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public FirebirdMetadataProvider()
        : base(new FirebirdConnectionStringBuilder())
    {
    }

    public override string ProviderName => "Firebird";

    public override DatabaseProvider Provider => DatabaseProvider.Firebird;

    public override string? DefaultSchema => null;

    public override bool IsSystemSchema(string? schemaName)
        => false;

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return SystemObjectPrefixes.Any(
            prefix => objectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
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
            Message = "Connecting to Firebird."
        });

        await using var connection = new FbConnection(BuildConnectionString(options));
        await connection.OpenAsync(cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTables,
            Message = "Reading Firebird tables."
        });
        var tables = await ReadTablesAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingColumns,
            Message = "Reading Firebird columns."
        });
        var columns = await ReadColumnsAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingPrimaryKeys,
            Message = "Reading Firebird primary keys."
        });
        var primaryKeys = await ReadPrimaryKeysAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingForeignKeys,
            Message = "Reading Firebird foreign keys."
        });
        var foreignKeys = await ReadForeignKeysAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingSequences,
            Message = "Reading Firebird generators."
        });
        var sequences = await ReadSequencesAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingViews,
            Message = "Reading Firebird views."
        });
        var views = await ReadViewsAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingProcedures,
            Message = "Reading Firebird procedures."
        });
        var procedures = await ReadProceduresAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.ReadingTriggers,
            Message = "Reading Firebird triggers."
        });
        var triggerInfos = await ReadTriggersAsync(connection, cancellationToken);

        progress?.Report(new MetadataProgress
        {
            Stage = MetadataProgressStage.Completed,
            Message = "Firebird metadata read completed."
        });

        return new DatabaseMetadata
        {
            Provider = Provider,
            DatabaseName = options.Database,
            DefaultSchema = null,
            Tables = tables
                .Select(table => table with
                {
                    Columns = EnrichColumns(
                        table.Name,
                        columns.GetValueOrDefault(table.Name, []),
                        primaryKeys.GetValueOrDefault(table.Name),
                        triggerInfos),
                    PrimaryKey = primaryKeys.GetValueOrDefault(table.Name),
                    ForeignKeys = foreignKeys.GetValueOrDefault(table.Name, [])
                })
                .ToArray(),
            Views = views,
            Procedures = procedures,
            Triggers = triggerInfos.Select(trigger => trigger.Metadata).ToArray(),
            Sequences = sequences
        };
    }

    public static CommonDbType MapFieldType(
        int fieldType,
        int fieldSubType,
        int? fieldLength = null,
        string? characterSetName = null)
        => fieldType switch
        {
            7 when fieldSubType is 1 or 2 => CommonDbType.Decimal,
            8 when fieldSubType is 1 or 2 => CommonDbType.Decimal,
            16 when fieldSubType is 1 or 2 => CommonDbType.Decimal,
            7 => CommonDbType.SmallInt,
            8 => CommonDbType.Integer,
            16 => CommonDbType.BigInt,
            10 => CommonDbType.Float,
            27 => CommonDbType.Double,
            23 => CommonDbType.Boolean,
            14 when fieldLength == 16
                && string.Equals(characterSetName, "OCTETS", StringComparison.OrdinalIgnoreCase)
                => CommonDbType.Guid,
            14 => CommonDbType.Char,
            37 => CommonDbType.VarChar,
            12 => CommonDbType.Date,
            13 => CommonDbType.Time,
            35 => CommonDbType.DateTime,
            261 when fieldSubType == 1 => CommonDbType.Text,
            261 => CommonDbType.Blob,
            _ => CommonDbType.Unknown
        };

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
        bool isIdentity,
        string? defaultExpression,
        string? columnName = null,
        IReadOnlyList<(string TriggerName, string Sql)>? triggers = null)
    {
        if (isIdentity)
        {
            return new ValueGenerationMetadata
            {
                Strategy = ValueGenerationStrategy.Identity
            };
        }

        var defaultSequenceName = ExtractSequenceName(defaultExpression);

        if (defaultSequenceName is not null)
        {
            return new ValueGenerationMetadata
            {
                Strategy = ValueGenerationStrategy.Sequence,
                SequenceName = defaultSequenceName
            };
        }

        if (columnName is null || triggers is null)
        {
            return new ValueGenerationMetadata { Strategy = ValueGenerationStrategy.None };
        }

        foreach (var trigger in triggers)
        {
            var sequenceName = DetectTriggerSequence(columnName, trigger.Sql);

            if (sequenceName is not null)
            {
                return new ValueGenerationMetadata
                {
                    Strategy = ValueGenerationStrategy.TriggerSequence,
                    SequenceName = sequenceName,
                    TriggerName = trigger.TriggerName
                };
            }
        }

        return new ValueGenerationMetadata { Strategy = ValueGenerationStrategy.None };
    }

    public static string? DetectTriggerSequence(string columnName, string triggerSql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        if (string.IsNullOrWhiteSpace(triggerSql))
        {
            return null;
        }

        var normalizedColumn = Regex.Escape(columnName.Trim());
        var hasColumnAssignment = Regex.IsMatch(
            triggerSql,
            @$"\bnew\s*\.\s*""?{normalizedColumn}""?\s*=",
            RegexOptions.IgnoreCase);

        if (!hasColumnAssignment)
        {
            return null;
        }

        return ExtractSequenceName(triggerSql);
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new FbConnection(connectionString);

    private static async Task<IReadOnlyList<TableMetadata>> ReadTablesAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select rdb$relation_name
            from rdb$relations
            where rdb$view_blr is null
              and coalesce(rdb$system_flag, 0) = 0
            order by rdb$relation_name
            """;

        return await QueryAsync(connection, sql, reader => new TableMetadata
        {
            Schema = null,
            Name = TrimIdentifier(reader.GetString(0)),
            Columns = []
        }, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<ColumnMetadata>>> ReadColumnsAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                relation_fields.rdb$relation_name,
                relation_fields.rdb$field_name,
                relation_fields.rdb$field_position,
                fields.rdb$field_type,
                fields.rdb$field_sub_type,
                fields.rdb$field_length,
                fields.rdb$field_precision,
                fields.rdb$field_scale,
                relation_fields.rdb$null_flag,
                coalesce(relation_fields.rdb$default_source, fields.rdb$default_source),
                relation_fields.rdb$identity_type,
                fields.rdb$computed_source,
                character_sets.rdb$character_set_name
            from rdb$relation_fields relation_fields
            join rdb$fields fields
              on fields.rdb$field_name = relation_fields.rdb$field_source
            join rdb$relations relations
              on relations.rdb$relation_name = relation_fields.rdb$relation_name
            left join rdb$character_sets character_sets
              on character_sets.rdb$character_set_id = fields.rdb$character_set_id
            where relations.rdb$view_blr is null
              and coalesce(relations.rdb$system_flag, 0) = 0
            order by relation_fields.rdb$relation_name, relation_fields.rdb$field_position
            """;

        var rows = await QueryAsync(connection, sql, reader =>
        {
            var tableName = TrimIdentifier(reader.GetString(0));
            var columnName = TrimIdentifier(reader.GetString(1));
            var fieldType = GetInt32(reader, 3);
            var fieldSubType = GetNullableInt32(reader, 4) ?? 0;
            var fieldLength = GetNullableInt32(reader, 5);
            var characterSetName = TrimIdentifierNullable(GetNullableString(reader, 12));
            var defaultExpression = GetNullableString(reader, 9);
            var computedExpression = GetNullableString(reader, 11);
            var isIdentity = !reader.IsDBNull(10);

            return new
            {
                TableName = tableName,
                Column = new ColumnMetadata
                {
                    Name = columnName,
                    OrdinalPosition = GetInt32(reader, 2) + 1,
                    NativeType = GetNativeType(fieldType, fieldSubType),
                    DbType = MapFieldType(fieldType, fieldSubType, fieldLength, characterSetName),
                    Length = GetLength(fieldType, fieldSubType, fieldLength),
                    Precision = GetNullableInt32(reader, 6),
                    Scale = GetScale(reader, 7),
                    IsUnicode = false,
                    IsNullable = reader.IsDBNull(8),
                    IsComputed = computedExpression is not null,
                    ComputedExpression = computedExpression,
                    DefaultValue = ParseDefaultValue(defaultExpression),
                    ValueGeneration = BuildValueGeneration(isIdentity, defaultExpression)
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
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                relation_constraints.rdb$relation_name,
                relation_constraints.rdb$constraint_name,
                index_segments.rdb$field_name
            from rdb$relation_constraints relation_constraints
            join rdb$index_segments index_segments
              on index_segments.rdb$index_name = relation_constraints.rdb$index_name
            where relation_constraints.rdb$constraint_type = 'PRIMARY KEY'
            order by
                relation_constraints.rdb$relation_name,
                relation_constraints.rdb$constraint_name,
                index_segments.rdb$field_position
            """;

        var rows = await QueryAsync(connection, sql, reader => new
        {
            TableName = TrimIdentifier(reader.GetString(0)),
            KeyName = TrimIdentifier(reader.GetString(1)),
            ColumnName = TrimIdentifier(reader.GetString(2))
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
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                source_constraints.rdb$relation_name,
                source_constraints.rdb$constraint_name,
                source_segments.rdb$field_name,
                target_constraints.rdb$relation_name,
                target_segments.rdb$field_name,
                relation_fields.rdb$null_flag
            from rdb$relation_constraints source_constraints
            join rdb$ref_constraints ref_constraints
              on ref_constraints.rdb$constraint_name = source_constraints.rdb$constraint_name
            join rdb$relation_constraints target_constraints
              on target_constraints.rdb$constraint_name = ref_constraints.rdb$const_name_uq
            join rdb$index_segments source_segments
              on source_segments.rdb$index_name = source_constraints.rdb$index_name
            join rdb$index_segments target_segments
              on target_segments.rdb$index_name = target_constraints.rdb$index_name
             and target_segments.rdb$field_position = source_segments.rdb$field_position
            join rdb$relation_fields relation_fields
              on relation_fields.rdb$relation_name = source_constraints.rdb$relation_name
             and relation_fields.rdb$field_name = source_segments.rdb$field_name
            where source_constraints.rdb$constraint_type = 'FOREIGN KEY'
            order by
                source_constraints.rdb$relation_name,
                source_constraints.rdb$constraint_name,
                source_segments.rdb$field_position
            """;

        var rows = await QueryAsync(connection, sql, reader => new
        {
            SourceTable = TrimIdentifier(reader.GetString(0)),
            Name = TrimIdentifier(reader.GetString(1)),
            SourceColumn = TrimIdentifier(reader.GetString(2)),
            TargetTable = TrimIdentifier(reader.GetString(3)),
            TargetColumn = TrimIdentifier(reader.GetString(4)),
            SourceColumnIsNullable = reader.IsDBNull(5)
        }, cancellationToken);

        return rows
            .GroupBy(row => new { row.SourceTable, row.Name, row.TargetTable })
            .GroupBy(
                group => group.Key.SourceTable,
                group => new ForeignKeyMetadata
                {
                    Name = group.Key.Name,
                    SourceSchema = null,
                    SourceTable = group.Key.SourceTable,
                    SourceColumns = group.Select(row => row.SourceColumn).ToArray(),
                    TargetSchema = null,
                    TargetTable = group.Key.TargetTable,
                    TargetColumns = group.Select(row => row.TargetColumn).ToArray(),
                    IsNullable = group.Any(row => row.SourceColumnIsNullable)
                })
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ForeignKeyMetadata>)group.ToArray());
    }

    private static async Task<IReadOnlyList<SequenceMetadata>> ReadSequencesAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select rdb$generator_name
            from rdb$generators
            where coalesce(rdb$system_flag, 0) = 0
            order by rdb$generator_name
            """;

        return await QueryAsync(connection, sql, reader => new SequenceMetadata
        {
            Schema = null,
            Name = TrimIdentifier(reader.GetString(0))
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<ViewMetadata>> ReadViewsAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select rdb$relation_name, rdb$view_source
            from rdb$relations
            where rdb$view_blr is not null
              and coalesce(rdb$system_flag, 0) = 0
            order by rdb$relation_name
            """;

        return await QueryAsync(connection, sql, reader => new ViewMetadata
        {
            Schema = null,
            Name = TrimIdentifier(reader.GetString(0)),
            Sql = GetNullableString(reader, 1)?.Trim() ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<ProcedureMetadata>> ReadProceduresAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select rdb$procedure_name, rdb$procedure_source
            from rdb$procedures
            where coalesce(rdb$system_flag, 0) = 0
            order by rdb$procedure_name
            """;

        return await QueryAsync(connection, sql, reader => new ProcedureMetadata
        {
            Schema = null,
            Name = TrimIdentifier(reader.GetString(0)),
            Sql = GetNullableString(reader, 1)?.Trim() ?? string.Empty
        }, cancellationToken);
    }

    private static async Task<IReadOnlyList<FirebirdTriggerInfo>> ReadTriggersAsync(
        FbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select rdb$trigger_name, rdb$relation_name, rdb$trigger_source
            from rdb$triggers
            where coalesce(rdb$system_flag, 0) = 0
              and rdb$relation_name is not null
            order by rdb$trigger_name
            """;

        return await QueryAsync(connection, sql, reader => new FirebirdTriggerInfo(
            TrimIdentifier(reader.GetString(1)),
            new TriggerMetadata
            {
                Schema = null,
                Name = TrimIdentifier(reader.GetString(0)),
                Sql = GetNullableString(reader, 2)?.Trim() ?? string.Empty
            }), cancellationToken);
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        FbConnection connection,
        string sql,
        Func<FbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var command = new FbCommand(sql, connection);

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(map(reader));
        }

        return rows;
    }

    private static IReadOnlyList<ColumnMetadata> EnrichColumns(
        string tableName,
        IReadOnlyList<ColumnMetadata> columns,
        PrimaryKeyMetadata? primaryKey,
        IReadOnlyList<FirebirdTriggerInfo> triggers)
    {
        var tableTriggers = triggers
            .Where(trigger => string.Equals(trigger.TableName, tableName, StringComparison.OrdinalIgnoreCase))
            .Select(trigger => (trigger.Metadata.Name, trigger.Metadata.Sql))
            .ToArray();

        return columns
            .Select(column =>
            {
                var valueGeneration = column.ValueGeneration?.Strategy == ValueGenerationStrategy.None
                    ? BuildValueGeneration(false, null, column.Name, tableTriggers)
                    : column.ValueGeneration;

                return column with
                {
                    IsPrimaryKey = primaryKey?.Columns.Contains(column.Name) == true,
                    ValueGeneration = valueGeneration
                };
            })
            .ToArray();
    }

    private static int? GetLength(int fieldType, int fieldSubType, int? fieldLength)
        => fieldType switch
        {
            14 or 37 => fieldLength,
            261 => null,
            7 or 8 or 16 when fieldSubType is 1 or 2 => null,
            _ => fieldLength
        };

    private static int? GetScale(FbDataReader reader, int ordinal)
    {
        var scale = GetNullableInt32(reader, ordinal);

        return scale is null ? null : Math.Abs(scale.Value);
    }

    private static int GetInt32(FbDataReader reader, int ordinal)
        => Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static int? GetNullableInt32(FbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal)
            ? null
            : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string? GetNullableString(FbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string TrimIdentifier(string value)
        => value.Trim();

    private static string? TrimIdentifierNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetNativeType(int fieldType, int fieldSubType)
        => fieldType switch
        {
            7 when fieldSubType == 1 => "NUMERIC",
            7 when fieldSubType == 2 => "DECIMAL",
            8 when fieldSubType == 1 => "NUMERIC",
            8 when fieldSubType == 2 => "DECIMAL",
            16 when fieldSubType == 1 => "NUMERIC",
            16 when fieldSubType == 2 => "DECIMAL",
            7 => "SMALLINT",
            8 => "INTEGER",
            16 => "BIGINT",
            10 => "FLOAT",
            27 => "DOUBLE PRECISION",
            23 => "BOOLEAN",
            14 => "CHAR",
            37 => "VARCHAR",
            12 => "DATE",
            13 => "TIME",
            35 => "TIMESTAMP",
            261 when fieldSubType == 1 => "BLOB SUB_TYPE TEXT",
            261 => "BLOB",
            _ => $"UNKNOWN({fieldType})"
        };

    private static DefaultValueKind GetDefaultValueKind(string expression)
    {
        if (ExtractSequenceName(expression) is not null)
        {
            return DefaultValueKind.Sequence;
        }

        if (expression.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase)
            || expression.Equals("CURRENT_DATE", StringComparison.OrdinalIgnoreCase)
            || expression.Equals("CURRENT_TIME", StringComparison.OrdinalIgnoreCase)
            || expression.StartsWith("current_timestamp", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultValueKind.SystemFunction;
        }

        if (expression.StartsWith('\'')
            || expression.Equals("true", StringComparison.OrdinalIgnoreCase)
            || expression.Equals("false", StringComparison.OrdinalIgnoreCase)
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

        var nextValueMatch = NextValueForRegex.Match(expression);

        if (nextValueMatch.Success)
        {
            return CleanIdentifier(nextValueMatch.Groups["name"].Value);
        }

        var genIdMatch = GenIdRegex.Match(expression);

        return genIdMatch.Success
            ? CleanIdentifier(genIdMatch.Groups["name"].Value)
            : null;
    }

    private static string CleanIdentifier(string value)
        => value.Split('.').Last().Trim().Trim('"');

    private sealed record FirebirdTriggerInfo(
        string TableName,
        TriggerMetadata Metadata);
}
