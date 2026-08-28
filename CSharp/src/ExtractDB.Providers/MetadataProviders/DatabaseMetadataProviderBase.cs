using System.Data.Common;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;

namespace ExtractDB.Providers.MetadataProviders;

public abstract class DatabaseMetadataProviderBase : IDatabaseMetadataProvider
{
    private readonly IConnectionStringBuilder connectionStringBuilder;

    protected DatabaseMetadataProviderBase(IConnectionStringBuilder connectionStringBuilder)
        => this.connectionStringBuilder = connectionStringBuilder;

    public abstract string ProviderName { get; }

    public abstract DatabaseProvider Provider { get; }

    public abstract string? DefaultSchema { get; }

    public abstract bool IsSystemSchema(string? schemaName);

    public abstract bool IsSystemObject(string? schemaName, string objectName);

    public async Task TestConnectionAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(connectionStringBuilder.Build(options));
        await connection.OpenAsync(cancellationToken);
    }

    public virtual async Task<DatabaseMetadata> ReadAsync(
        DatabaseConnectionOptions options,
        IProgress<MetadataProgress>? progress,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(connectionStringBuilder.Build(options));
        await connection.OpenAsync(cancellationToken);

        var defaultSchema = await ResolveDefaultSchemaAsync(connection, cancellationToken);

        return new DatabaseMetadata
        {
            Provider = Provider,
            DatabaseName = options.Database,
            DefaultSchema = defaultSchema
        };
    }

    protected abstract DbConnection CreateConnection(string connectionString);

    protected string BuildConnectionString(DatabaseConnectionOptions options)
        => connectionStringBuilder.Build(options);

    protected virtual Task<string?> ResolveDefaultSchemaAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(DefaultSchema);
}
