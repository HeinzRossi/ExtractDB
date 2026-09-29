using ExtractDB.Core.Types;

namespace ExtractDB.Core.Metadata;

public sealed record ColumnMetadata
{
    public required string Name { get; init; }
    public required int OrdinalPosition { get; init; }
    public required string NativeType { get; init; }
    public required CommonDbType DbType { get; init; }

    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }

    public bool IsUnicode { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsComputed { get; init; }

    public string? ComputedExpression { get; init; }
    public DefaultValueMetadata? DefaultValue { get; init; }
    public ValueGenerationMetadata? ValueGeneration { get; init; }
}
