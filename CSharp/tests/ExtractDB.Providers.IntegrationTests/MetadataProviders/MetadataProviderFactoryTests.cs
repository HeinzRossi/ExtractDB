using ExtractDB.Core.Types;
using ExtractDB.Providers;
using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers.IntegrationTests.MetadataProviders;

public sealed class MetadataProviderFactoryTests
{
    [Theory]
    [InlineData(DatabaseProvider.PostgreSql, typeof(PostgreSqlMetadataProvider), "PostgreSQL")]
    [InlineData(DatabaseProvider.SqlServer, typeof(SqlServerMetadataProvider), "SQL Server")]
    [InlineData(DatabaseProvider.Firebird, typeof(FirebirdMetadataProvider), "Firebird")]
    public void Factory_creates_metadata_provider_for_provider(
        DatabaseProvider provider,
        Type expectedType,
        string expectedProviderName)
    {
        var factory = new MetadataProviderFactory();

        var metadataProvider = factory.Create(provider);

        Assert.IsType(expectedType, metadataProvider);
        Assert.Equal(expectedProviderName, metadataProvider.ProviderName);
    }
}
