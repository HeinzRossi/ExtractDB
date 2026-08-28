namespace ExtractDB.Core.Metadata;

public sealed record ForeignKeyMetadata
{
    public required string Name { get; init; }
    public string? SourceSchema { get; init; }
    public required string SourceTable { get; init; }
    public required IReadOnlyList<string> SourceColumns { get; init; }
    public string? TargetSchema { get; init; }
    public required string TargetTable { get; init; }
    public required IReadOnlyList<string> TargetColumns { get; init; }
    public bool IsNullable { get; init; }
}
