namespace ExtractDB.Core.Contracts;

public sealed record DataExportConfiguration
{
    public const int DefaultMaxRowsPerTable = 10000;

    public int MaxRowsPerTable { get; init; } = DefaultMaxRowsPerTable;

    public IReadOnlyList<DataExportTableGroup> Tables { get; init; } = [];
}

public sealed record DataExportTableGroup
{
    public string? Schema { get; init; }

    public IReadOnlyList<string> Name { get; init; } = [];
}

public sealed record DataExportTable(
    string? Schema,
    string Name);
