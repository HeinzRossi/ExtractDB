using ExtractDB.Core.Types;

namespace ExtractDB.Core.Metadata;

public sealed record DefaultValueMetadata
{
    public required string RawExpression { get; init; }
    public required DefaultValueKind Kind { get; init; }
}
