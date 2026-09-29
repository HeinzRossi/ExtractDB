using ExtractDB.Core.Metadata;

namespace ExtractDB.Generators.CSharp;

public interface ICSharpTypeMapper
{
    string Map(ColumnMetadata column);
}
