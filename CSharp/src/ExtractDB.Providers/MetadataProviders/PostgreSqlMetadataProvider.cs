using System.Data.Common;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using Npgsql;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class PostgreSqlMetadataProvider : DatabaseMetadataProviderBase
{
    public PostgreSqlMetadataProvider()
        : base(new PostgreSqlConnectionStringBuilder())
    {
    }

    public override string ProviderName => "PostgreSQL";

    public override DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    public override string DefaultSchema => "public";

    public override bool IsSystemSchema(string? schemaName)
        => string.Equals(schemaName, "pg_catalog", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaName, "information_schema", StringComparison.OrdinalIgnoreCase);

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return IsSystemSchema(schemaName);
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    protected override async Task<string?> ResolveDefaultSchemaAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_schema()";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as string ?? DefaultSchema;
    }
}
