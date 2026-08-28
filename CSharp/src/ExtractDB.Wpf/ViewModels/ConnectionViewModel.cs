using CommunityToolkit.Mvvm.ComponentModel;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.ViewModels;

public sealed class ConnectionViewModel : ObservableObject
{
    private DatabaseProvider provider = DatabaseProvider.PostgreSql;
    private string server = "localhost";
    private int port = DatabaseConnectionOptions.GetDefaultPort(DatabaseProvider.PostgreSql);
    private string database = string.Empty;
    private string userName = string.Empty;
    private string password = string.Empty;

    public IReadOnlyList<DatabaseProvider> ProviderOptions { get; } =
    [
        DatabaseProvider.PostgreSql,
        DatabaseProvider.SqlServer,
        DatabaseProvider.Firebird
    ];

    public DatabaseProvider Provider
    {
        get => provider;
        set
        {
            if (SetProperty(ref provider, value))
            {
                Port = DatabaseConnectionOptions.GetDefaultPort(value);
            }
        }
    }

    public string Server
    {
        get => server;
        set => SetProperty(ref server, value);
    }

    public int Port
    {
        get => port;
        set => SetProperty(ref port, value);
    }

    public string Database
    {
        get => database;
        set => SetProperty(ref database, value);
    }

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public DatabaseConnectionOptions ToConnectionOptions()
        => new()
        {
            Provider = Provider,
            Server = Server,
            Port = Port,
            Database = Database,
            UserName = UserName,
            Password = Password
        };
}
