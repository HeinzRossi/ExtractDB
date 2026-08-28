using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp.Tests;

public sealed class CSharpEntityCodeGeneratorTests
{
    private readonly CSharpEntityCodeGenerator generator = new();

    [Fact]
    public void Generate_writes_model_attributes_columns_and_initializers()
    {
        var table = new TableMetadata
        {
            Schema = "public",
            Name = "cliente",
            Columns =
            [
                Column("id_cliente", 1, CommonDbType.Integer, "integer", isPrimaryKey: true, valueGeneration: new ValueGenerationMetadata
                {
                    Strategy = ValueGenerationStrategy.Identity
                }),
                Column("nome", 2, CommonDbType.VarChar, "varchar", length: 120),
                Column("foto", 3, CommonDbType.Blob, "bytea", isNullable: true),
                Column("saldo", 4, CommonDbType.Decimal, "numeric", precision: 18, scale: 2, defaultValue: new DefaultValueMetadata
                {
                    RawExpression = "0",
                    Kind = DefaultValueKind.Literal
                }),
                Column("saldo_calculado", 5, CommonDbType.Decimal, "numeric", isComputed: true, computedExpression: "saldo * 2"),
                Column("status", 6, CommonDbType.Enum, "status_pedido")
            ]
        };

        var source = generator.Generate(table, Context("cliente"));

        Assert.Contains("using Demo.Metadata;", source);
        Assert.Contains("using Demo.Models.Enums;", source);
        Assert.Contains("[Table(\"cliente\", Schema = \"public\")]", source);
        Assert.Contains("public sealed class Cliente", source);
        Assert.Contains("[Key]", source);
        Assert.Contains("[DatabaseGenerated(DatabaseGeneratedOption.Identity)]", source);
        Assert.Contains("[Required]", source);
        Assert.Contains("[MaxLength(120)]", source);
        Assert.Contains("public string Nome { get; set; } = string.Empty;", source);
        Assert.Contains("public byte[]? Foto { get; set; }", source);
        Assert.Contains("[Column(\"saldo\", TypeName = \"decimal(18,2)\")]", source);
        Assert.Contains("[DatabaseDefault(\"0\")]", source);
        Assert.Contains("[DatabaseComputed(\"saldo * 2\")]", source);
        Assert.Contains("public StatusPedido Status { get; set; }", source);
    }

    [Fact]
    public void Generate_writes_simple_nullable_navigation_only_for_selected_target()
    {
        var table = new TableMetadata
        {
            Name = "pedido",
            Columns =
            [
                Column("id_pedido", 1, CommonDbType.Integer, "integer", isPrimaryKey: true),
                Column("id_cliente", 2, CommonDbType.Integer, "integer", isNullable: true),
                Column("id_auditoria", 3, CommonDbType.Integer, "integer")
            ],
            ForeignKeys =
            [
                new ForeignKeyMetadata
                {
                    Name = "fk_pedido_cliente",
                    SourceTable = "pedido",
                    SourceColumns = ["id_cliente"],
                    TargetTable = "cliente",
                    TargetColumns = ["id_cliente"],
                    IsNullable = true
                },
                new ForeignKeyMetadata
                {
                    Name = "fk_pedido_auditoria",
                    SourceTable = "pedido",
                    SourceColumns = ["id_auditoria"],
                    TargetTable = "auditoria",
                    TargetColumns = ["id_auditoria"]
                }
            ]
        };

        var source = generator.Generate(table, Context("pedido", "cliente"));

        Assert.Contains("[ForeignKey(nameof(IdCliente))]", source);
        Assert.Contains("public Cliente? Cliente { get; set; }", source);
        Assert.DoesNotContain("public Auditoria Auditoria { get; set; }", source);
    }

    [Fact]
    public void Generate_writes_composite_foreign_key_attribute()
    {
        var table = new TableMetadata
        {
            Name = "pedido_item",
            Columns =
            [
                Column("empresa_id", 1, CommonDbType.Integer, "integer", isPrimaryKey: true),
                Column("documento_id", 2, CommonDbType.Integer, "integer", isPrimaryKey: true)
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
        };

        var source = generator.Generate(table, Context("pedido_item", "pedido"));

        Assert.Contains("using Demo.Metadata;", source);
        Assert.Contains("[CompositeForeignKey(nameof(EmpresaId), nameof(DocumentoId))]", source);
        Assert.Contains("public Pedido Pedido { get; set; } = null!;", source);
    }

    private static GenerationContext Context(params string[] selectedTables)
        => new()
        {
            NamespaceBase = "Demo",
            OutputDirectory = Path.GetTempPath(),
            SelectedTables = selectedTables
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
