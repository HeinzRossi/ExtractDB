using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Wpf.ViewModels;

namespace ExtractDB.Wpf.Tests;

public sealed class ConnectionViewModelTests
{
    [Theory]
    [InlineData(DatabaseProvider.PostgreSql, 5432)]
    [InlineData(DatabaseProvider.SqlServer, 1433)]
    [InlineData(DatabaseProvider.Firebird, 3050)]
    public void Provider_updates_default_port(DatabaseProvider provider, int expectedPort)
    {
        var viewModel = new ConnectionViewModel
        {
            Provider = provider
        };

        Assert.Equal(expectedPort, viewModel.Port);
    }

    [Fact]
    public void ToConnectionOptions_preserves_temporary_connection_values()
    {
        var viewModel = new ConnectionViewModel
        {
            Provider = DatabaseProvider.SqlServer,
            Server = "localhost",
            Port = 1433,
            Database = "ExtractDB",
            UserName = "sa",
            Password = "secret"
        };

        DatabaseConnectionOptions options = viewModel.ToConnectionOptions();

        Assert.Equal(DatabaseProvider.SqlServer, options.Provider);
        Assert.Equal("localhost", options.Server);
        Assert.Equal(1433, options.Port);
        Assert.Equal("ExtractDB", options.Database);
        Assert.Equal("sa", options.UserName);
        Assert.Equal("secret", options.Password);
    }
}
