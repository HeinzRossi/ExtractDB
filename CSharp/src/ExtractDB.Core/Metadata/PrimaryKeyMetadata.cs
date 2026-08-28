namespace ExtractDB.Core.Metadata;

public sealed record PrimaryKeyMetadata
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
}
