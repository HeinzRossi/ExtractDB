using ExtractDB.Core.Types;

namespace ExtractDB.Core.Contracts;

public interface IDataExportProvider
{
    Task<GenerationResult> ExportAsync(
        DataExportRequest request,
        CancellationToken cancellationToken);
}

public interface IDataExportProviderFactory
{
    IDataExportProvider Create(DatabaseProvider provider);
}
