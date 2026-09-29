using ExtractDB.Core.Metadata;

namespace ExtractDB.Generators.CSharp;

public interface IEntityCodeGenerator
{
    string Generate(
        TableMetadata table,
        GenerationContext context);
}
