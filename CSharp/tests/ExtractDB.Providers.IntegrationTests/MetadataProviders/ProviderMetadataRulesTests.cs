using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers.IntegrationTests.MetadataProviders;

public sealed class ProviderMetadataRulesTests
{
    [Fact]
    public void PostgreSql_rules_use_public_as_default_schema_and_filter_internal_schemas()
    {
        var provider = new PostgreSqlMetadataProvider();

        Assert.Equal("public", provider.DefaultSchema);
        Assert.True(provider.IsSystemSchema("pg_catalog"));
        Assert.True(provider.IsSystemSchema("information_schema"));
        Assert.True(provider.IsSystemObject("pg_catalog", "pg_type"));
        Assert.False(provider.IsSystemObject("public", "clientes"));
    }

    [Fact]
    public void SqlServer_rules_use_dbo_as_default_schema_and_filter_internal_schemas()
    {
        var provider = new SqlServerMetadataProvider();

        Assert.Equal("dbo", provider.DefaultSchema);
        Assert.True(provider.IsSystemSchema("sys"));
        Assert.True(provider.IsSystemSchema("INFORMATION_SCHEMA"));
        Assert.True(provider.IsSystemObject("sys", "tables"));
        Assert.False(provider.IsSystemObject("dbo", "Clientes"));
    }

    [Fact]
    public void Firebird_rules_do_not_expose_schema_and_filter_internal_prefixes()
    {
        var provider = new FirebirdMetadataProvider();

        Assert.Null(provider.DefaultSchema);
        Assert.False(provider.IsSystemSchema(null));
        Assert.True(provider.IsSystemObject(null, "RDB$RELATIONS"));
        Assert.True(provider.IsSystemObject(null, "MON$ATTACHMENTS"));
        Assert.True(provider.IsSystemObject(null, "SEC$USERS"));
        Assert.False(provider.IsSystemObject(null, "CLIENTES"));
    }
}
