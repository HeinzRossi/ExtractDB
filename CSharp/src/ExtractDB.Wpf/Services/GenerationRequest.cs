using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.Services;

public sealed record GenerationRequest
{
    public required DatabaseProvider Provider { get; init; }
    public DatabaseConnectionOptions? ConnectionOptions { get; init; }
    public required string NamespaceBase { get; init; }
    public required string OutputDirectory { get; init; }
    public string? DataExportConfigurationPath { get; init; }
    public IReadOnlyList<SelectedObject> SelectedObjects { get; init; } = [];
}

public sealed record SelectedObject(
    DatabaseObjectType ObjectType,
    string? Schema,
    string Name);
