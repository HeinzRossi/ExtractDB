namespace ExtractDB.Generators.CSharp;

public sealed record GenerationContext
{
    public required string NamespaceBase { get; init; }
    public required string OutputDirectory { get; init; }
    public required IReadOnlyCollection<string> SelectedTables { get; init; }

    public bool IsTableSelected(string tableName)
        => SelectedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
}
