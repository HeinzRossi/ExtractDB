namespace ExtractDB.Core.Metadata;

public sealed record ViewMetadata
{
    public string? Schema { get; init; }
    public required string Name { get; init; }
    public required string Sql { get; init; }
}
