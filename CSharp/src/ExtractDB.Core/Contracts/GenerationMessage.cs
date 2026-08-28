using ExtractDB.Core.Types;

namespace ExtractDB.Core.Contracts;

public sealed record GenerationMessage
{
    public required GenerationMessageSeverity Severity { get; init; }
    public required DatabaseObjectType ObjectType { get; init; }
    public string? Schema { get; init; }
    public required string ObjectName { get; init; }
    public required string Stage { get; init; }
    public required string Message { get; init; }
}
