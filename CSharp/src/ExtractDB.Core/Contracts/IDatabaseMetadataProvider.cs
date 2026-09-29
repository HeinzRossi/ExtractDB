using ExtractDB.Core.Metadata;

namespace ExtractDB.Core.Contracts;

public interface IDatabaseMetadataProvider
{
    string ProviderName { get; }

    Task TestConnectionAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken);

    Task<DatabaseMetadata> ReadAsync(
        DatabaseConnectionOptions options,
        IProgress<MetadataProgress>? progress,
        CancellationToken cancellationToken);
}
