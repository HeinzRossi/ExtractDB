using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers;

public sealed class MetadataProviderFactory : IMetadataProviderFactory
{
    public IDatabaseMetadataProvider Create(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.PostgreSql => new PostgreSqlMetadataProvider(),
            DatabaseProvider.SqlServer => new SqlServerMetadataProvider(),
            DatabaseProvider.Firebird => new FirebirdMetadataProvider(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
}
