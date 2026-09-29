using ExtractDB.Core.Types;

namespace ExtractDB.Core.Metadata;

public sealed record DatabaseMetadata
{
    public required DatabaseProvider Provider { get; init; }
    public required string DatabaseName { get; init; }
    public string? DefaultSchema { get; init; }
    public IReadOnlyList<TableMetadata> Tables { get; init; } = [];
    public IReadOnlyList<ViewMetadata> Views { get; init; } = [];
    public IReadOnlyList<ProcedureMetadata> Procedures { get; init; } = [];
    public IReadOnlyList<TriggerMetadata> Triggers { get; init; } = [];
    public IReadOnlyList<SequenceMetadata> Sequences { get; init; } = [];
    public IReadOnlyList<EnumMetadata> Enums { get; init; } = [];
}
