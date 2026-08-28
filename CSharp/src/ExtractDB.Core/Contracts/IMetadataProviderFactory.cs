using ExtractDB.Core.Types;

namespace ExtractDB.Core.Contracts;

public interface IMetadataProviderFactory
{
    IDatabaseMetadataProvider Create(DatabaseProvider provider);
}
