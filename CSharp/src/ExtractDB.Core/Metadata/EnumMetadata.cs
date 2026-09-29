namespace ExtractDB.Core.Metadata;

public sealed record EnumMetadata
{
    public string? Schema { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Values { get; init; }
}
