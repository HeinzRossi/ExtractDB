using System.Text;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp.Tests;

public sealed class CSharpDatabaseGeneratorTests
{
    [Fact]
    public void Generate_writes_selected_models_enums_metadata_and_scripts()
    {
        using var directory = TempDirectory.Create();
        var database = CreateDatabase();
        var generator = new CSharpDatabaseGenerator();

        var result = generator.Generate(database, Context(directory.Path, "cliente", "pedido", "pedido_item"));

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, result.WarningCount);
        Assert.Contains("unknown database type", result.Messages.Single().Message);
        Assert.True(File.Exists(Path.Combine(directory.Path, "Models", "Cliente.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Models", "Pedido.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Models", "PedidoItem.cs")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "Models", "Auditoria.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Models", "Enums", "StatusPedido.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Metadata", "SequenceAttribute.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Metadata", "GeneratedByTriggerAttribute.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Metadata", "DatabaseDefaultAttribute.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Metadata", "DatabaseComputedAttribute.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Metadata", "CompositeForeignKeyAttribute.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Views", "public", "vw_cliente.sql")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Procedures", "public", "sp_cliente.sql")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Triggers", "tr_cliente.sql")));
    }

    [Fact]
    public void Generate_writes_utf8_without_bom_crlf_and_overwrites()
    {
        using var directory = TempDirectory.Create();
        var path = Path.Combine(directory.Path, "Models", "Cliente.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "old content");

        var generator = new CSharpDatabaseGenerator();
        generator.Generate(CreateDatabase(), Context(directory.Path, "cliente"));

        var bytes = File.ReadAllBytes(path);
        var text = Encoding.UTF8.GetString(bytes);

        Assert.False(StartsWith(bytes, Encoding.UTF8.GetPreamble()));
        Assert.DoesNotContain("old content", text);
        Assert.Contains("\r\n", text);
        Assert.DoesNotContain("\nnamespace Demo.Models;\n", text);
    }

    [Fact]
    public void Generate_uses_schema_subfolders_only_when_schema_exists()
    {
        using var directory = TempDirectory.Create();
        var database = new DatabaseMetadata
        {
            Provider = DatabaseProvider.Firebird,
            DatabaseName = "firebird",
            Tables = [],
            Views =
            [
                new ViewMetadata
                {
                    Name = "VW_CLIENTE",
                    Sql = "select 1 from rdb$database"
                }
            ]
        };

        new CSharpDatabaseGenerator().Generate(database, Context(directory.Path));

        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Views", "VW_CLIENTE.sql")));
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "Scripts", "Views", "public")));
    }

    private static GenerationContext Context(string outputDirectory, params string[] selectedTables)
        => new()
        {
            NamespaceBase = "Demo",
            OutputDirectory = outputDirectory,
            SelectedTables = selectedTables
        };

    private static bool StartsWith(byte[] bytes, byte[] prefix)
        => bytes.Length >= prefix.Length && bytes.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    internal static DatabaseMetadata CreateDatabase()
        => new()
        {
            Provider = DatabaseProvider.PostgreSql,
            DatabaseName = "demo",
            DefaultSchema = "public",
            Tables =
            [
                new TableMetadata
                {
                    Schema = "public",
                    Name = "cliente",
                    Columns =
                    [
                        Column("id_cliente", 1, CommonDbType.Integer, "integer", isPrimaryKey: true, valueGeneration: new ValueGenerationMetadata
                        {
                            Strategy = ValueGenerationStrategy.TriggerSequence,
                            SequenceName = "cliente_id_seq",
                            TriggerName = "tr_cliente_bi"
                        }),
                        Column("nome", 2, CommonDbType.VarChar, "varchar", length: 120),
                        Column("apelido", 3, CommonDbType.VarChar, "varchar", isNullable: true),
                        Column("payload", 4, CommonDbType.Json, "jsonb", isNullable: true),
                        Column("binario", 5, CommonDbType.Binary, "bytea"),
                        Column("misterio", 6, CommonDbType.Unknown, "mystery_type", isNullable: true)
                    ]
                },
                new TableMetadata
                {
                    Schema = "public",
                    Name = "pedido",
                    Columns =
                    [
                        Column("id_pedido", 1, CommonDbType.Integer, "integer", isPrimaryKey: true, valueGeneration: new ValueGenerationMetadata
                        {
                            Strategy = ValueGenerationStrategy.Identity
                        }),
                        Column("id_cliente", 2, CommonDbType.Integer, "integer"),
                        Column("status", 3, CommonDbType.Enum, "status_pedido"),
                        Column("total", 4, CommonDbType.Decimal, "numeric", precision: 18, scale: 2, defaultValue: new DefaultValueMetadata
                        {
                            RawExpression = "0",
                            Kind = DefaultValueKind.Literal
                        }),
                        Column("total_com_taxa", 5, CommonDbType.Decimal, "numeric", isComputed: true, computedExpression: "total * 1.1")
                    ],
                    ForeignKeys =
                    [
                        new ForeignKeyMetadata
                        {
                            Name = "fk_pedido_cliente",
                            SourceTable = "pedido",
                            SourceColumns = ["id_cliente"],
                            TargetTable = "cliente",
                            TargetColumns = ["id_cliente"]
                        }
                    ]
                },
                new TableMetadata
                {
                    Schema = "public",
                    Name = "pedido_item",
                    Columns =
                    [
                        Column("empresa_id", 1, CommonDbType.Integer, "integer", isPrimaryKey: true),
                        Column("documento_id", 2, CommonDbType.Integer, "integer", isPrimaryKey: true),
                        Column("produto_id", 3, CommonDbType.Integer, "integer", isPrimaryKey: true)
                    ],
                    ForeignKeys =
                    [
                        new ForeignKeyMetadata
                        {
                            Name = "fk_item_pedido",
                            SourceTable = "pedido_item",
                            SourceColumns = ["empresa_id", "documento_id"],
                            TargetTable = "pedido",
                            TargetColumns = ["empresa_id", "documento_id"]
                        }
                    ]
                },
                new TableMetadata
                {
                    Schema = "public",
                    Name = "auditoria",
                    Columns =
                    [
                        Column("id_auditoria", 1, CommonDbType.Integer, "integer", isPrimaryKey: true)
                    ]
                }
            ],
            Views =
            [
                new ViewMetadata
                {
                    Schema = "public",
                    Name = "vw_cliente",
                    Sql = "select * from public.cliente"
                }
            ],
            Procedures =
            [
                new ProcedureMetadata
                {
                    Schema = "public",
                    Name = "sp_cliente",
                    Sql = "select 1"
                }
            ],
            Triggers =
            [
                new TriggerMetadata
                {
                    Name = "tr_cliente",
                    Sql = "begin end"
                }
            ],
            Enums =
            [
                new EnumMetadata
                {
                    Schema = "public",
                    Name = "status_pedido",
                    Values = ["aberto", "pago", "cancelado"]
                }
            ]
        };

    private static ColumnMetadata Column(
        string name,
        int ordinalPosition,
        CommonDbType dbType,
        string nativeType,
        bool isNullable = false,
        bool isPrimaryKey = false,
        int? length = null,
        int? precision = null,
        int? scale = null,
        bool isComputed = false,
        string? computedExpression = null,
        DefaultValueMetadata? defaultValue = null,
        ValueGenerationMetadata? valueGeneration = null)
        => new()
        {
            Name = name,
            OrdinalPosition = ordinalPosition,
            NativeType = nativeType,
            DbType = dbType,
            IsNullable = isNullable,
            IsPrimaryKey = isPrimaryKey,
            Length = length,
            Precision = precision,
            Scale = scale,
            IsComputed = isComputed,
            ComputedExpression = computedExpression,
            DefaultValue = defaultValue,
            ValueGeneration = valueGeneration
        };
}
