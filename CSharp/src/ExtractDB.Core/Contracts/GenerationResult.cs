namespace ExtractDB.Core.Contracts;

public sealed record GenerationResult
{
    public required int SuccessCount { get; init; }
    public required int WarningCount { get; init; }
    public required int ErrorCount { get; init; }
    public IReadOnlyList<GenerationMessage> Messages { get; init; } = [];
}
