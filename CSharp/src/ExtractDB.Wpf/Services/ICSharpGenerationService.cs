using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;

namespace ExtractDB.Wpf.Services;

public interface ICSharpGenerationService
{
    Task<GenerationResult> GenerateAsync(
        DatabaseMetadata metadata,
        GenerationRequest request,
        CancellationToken cancellationToken);
}
