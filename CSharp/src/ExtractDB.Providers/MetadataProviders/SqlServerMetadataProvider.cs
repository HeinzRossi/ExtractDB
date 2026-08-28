using System.Data.Common;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using Microsoft.Data.SqlClient;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class SqlServerMetadataProvider : DatabaseMetadataProviderBase
{
    public SqlServerMetadataProvider()
        : base(new SqlServerConnectionStringBuilder())
    {
    }

    public override string ProviderName => "SQL Server";

    public override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public override string DefaultSchema => "dbo";

    public override bool IsSystemSchema(string? schemaName)
        => string.Equals(schemaName, "sys", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaName, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return IsSystemSchema(schemaName);
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    protected override async Task<string?> ResolveDefaultSchemaAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "select schema_name()";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as string ?? DefaultSchema;
    }
}
