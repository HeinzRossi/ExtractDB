using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;

namespace ExtractDB.Providers.ConnectionStrings;

public interface IConnectionStringBuilder
{
    DatabaseProvider Provider { get; }

    string Build(DatabaseConnectionOptions options);
}
