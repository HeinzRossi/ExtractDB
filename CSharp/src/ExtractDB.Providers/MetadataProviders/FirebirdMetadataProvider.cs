using System.Data.Common;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using FirebirdSql.Data.FirebirdClient;

namespace ExtractDB.Providers.MetadataProviders;

public sealed class FirebirdMetadataProvider : DatabaseMetadataProviderBase
{
    private static readonly string[] SystemObjectPrefixes = ["RDB$", "MON$", "SEC$"];

    public FirebirdMetadataProvider()
        : base(new FirebirdConnectionStringBuilder())
    {
    }

    public override string ProviderName => "Firebird";

    public override DatabaseProvider Provider => DatabaseProvider.Firebird;

    public override string? DefaultSchema => null;

    public override bool IsSystemSchema(string? schemaName)
        => false;

    public override bool IsSystemObject(string? schemaName, string objectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        return SystemObjectPrefixes.Any(
            prefix => objectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    protected override DbConnection CreateConnection(string connectionString)
        => new FbConnection(connectionString);
}
