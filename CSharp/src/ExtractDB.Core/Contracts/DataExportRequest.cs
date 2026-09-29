using ExtractDB.Core.Metadata;

namespace ExtractDB.Core.Contracts;

public sealed record DataExportRequest
{
    public required DatabaseConnectionOptions ConnectionOptions { get; init; }

    public required DatabaseMetadata Metadata { get; init; }

    public required string OutputDirectory { get; init; }

    public required DataExportConfiguration Configuration { get; init; }
}
