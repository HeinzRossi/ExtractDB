using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;

namespace ExtractDB.Wpf.Services;

public interface ICSharpGenerationService
{
    GenerationResult Generate(
        DatabaseMetadata metadata,
        GenerationRequest request);
}
