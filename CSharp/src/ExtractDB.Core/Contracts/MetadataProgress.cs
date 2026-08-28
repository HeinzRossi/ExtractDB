using ExtractDB.Core.Types;

namespace ExtractDB.Core.Contracts;

public sealed record MetadataProgress
{
    public required MetadataProgressStage Stage { get; init; }
    public string? ObjectName { get; init; }
    public int? CompletedCount { get; init; }
    public int? TotalCount { get; init; }
    public string? Message { get; init; }
}
