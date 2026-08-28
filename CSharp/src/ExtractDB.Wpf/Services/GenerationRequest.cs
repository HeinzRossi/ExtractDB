using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.Services;

public sealed record GenerationRequest
{
    public required string NamespaceBase { get; init; }
    public required string OutputDirectory { get; init; }
    public IReadOnlyList<SelectedObject> SelectedObjects { get; init; } = [];
}

public sealed record SelectedObject(
    DatabaseObjectType ObjectType,
    string? Schema,
    string Name);
