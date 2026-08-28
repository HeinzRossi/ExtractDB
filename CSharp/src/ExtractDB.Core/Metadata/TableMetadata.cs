namespace ExtractDB.Core.Metadata;

public sealed record TableMetadata
{
    public string? Schema { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<ColumnMetadata> Columns { get; init; }
    public PrimaryKeyMetadata? PrimaryKey { get; init; }
    public IReadOnlyList<ForeignKeyMetadata> ForeignKeys { get; init; } = [];
}
