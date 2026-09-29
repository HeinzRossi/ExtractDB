using ExtractDB.Core.Types;

namespace ExtractDB.Core.Metadata;

public sealed record ValueGenerationMetadata
{
    public required ValueGenerationStrategy Strategy { get; init; }
    public string? SequenceName { get; init; }
    public string? TriggerName { get; init; }
}
