using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Providers.ConnectionStrings;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace ExtractDB.Providers.IntegrationTests.ConnectionStrings;

public sealed class ConnectionStringBuilderTests
{
    [Fact]
    public void PostgreSql_builder_maps_connection_options()
    {
        var connectionString = new PostgreSqlConnectionStringBuilder().Build(
            CreateOptions(DatabaseProvider.PostgreSql, 5432));

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("localhost", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("ExtractDB", builder.Database);
        Assert.Equal("extract_user", builder.Username);
        Assert.Equal("secret", builder.Password);
    }

    [Fact]
    public void SqlServer_builder_maps_connection_options()
    {
        var connectionString = new SqlServerConnectionStringBuilder().Build(
            CreateOptions(DatabaseProvider.SqlServer, 1433));

        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("localhost,1433", builder.DataSource);
        Assert.Equal("ExtractDB", builder.InitialCatalog);
        Assert.Equal("extract_user", builder.UserID);
        Assert.Equal("secret", builder.Password);
        Assert.False(builder.Encrypt);
        Assert.True(builder.TrustServerCertificate);
    }

    [Fact]
    public void Firebird_builder_maps_connection_options()
    {
        var connectionString = new FirebirdConnectionStringBuilder().Build(
            CreateOptions(DatabaseProvider.Firebird, 3050));

        var builder = new FbConnectionStringBuilder(connectionString);

        Assert.Equal("localhost", builder.DataSource);
        Assert.Equal(3050, builder.Port);
        Assert.Equal("ExtractDB", builder.Database);
        Assert.Equal("extract_user", builder.UserID);
        Assert.Equal("secret", builder.Password);
        Assert.Equal("UTF8", builder.Charset);
    }

    [Theory]
    [InlineData(DatabaseProvider.PostgreSql, typeof(PostgreSqlConnectionStringBuilder))]
    [InlineData(DatabaseProvider.SqlServer, typeof(SqlServerConnectionStringBuilder))]
    [InlineData(DatabaseProvider.Firebird, typeof(FirebirdConnectionStringBuilder))]
    public void Factory_creates_builder_for_provider(
        DatabaseProvider provider,
        Type expectedType)
    {
        var factory = new ConnectionStringBuilderFactory();

        Assert.IsType(expectedType, factory.Create(provider));
    }

    private static DatabaseConnectionOptions CreateOptions(
        DatabaseProvider provider,
        int port)
        => new()
        {
            Provider = provider,
            Server = "localhost",
            Port = port,
            Database = "ExtractDB",
            UserName = "extract_user",
            Password = "secret"
        };
}
