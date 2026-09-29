using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;

namespace ExtractDB.Core.Tests.Contracts;

public sealed class DatabaseConnectionOptionsTests
{
    [Theory]
    [InlineData(DatabaseProvider.PostgreSql, 5432)]
    [InlineData(DatabaseProvider.SqlServer, 1433)]
    [InlineData(DatabaseProvider.Firebird, 3050)]
    public void GetDefaultPort_returns_documented_provider_port(
        DatabaseProvider provider,
        int expectedPort)
    {
        Assert.Equal(expectedPort, DatabaseConnectionOptions.GetDefaultPort(provider));
    }
}
