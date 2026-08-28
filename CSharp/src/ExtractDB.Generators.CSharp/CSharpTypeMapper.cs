using ExtractDB.Core.Metadata;
using ExtractDB.Core.Naming;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp;

public sealed class CSharpTypeMapper : ICSharpTypeMapper
{
    private readonly INameConverter nameConverter;

    public CSharpTypeMapper()
        : this(new NameConverter())
    {
    }

    public CSharpTypeMapper(INameConverter nameConverter)
        => this.nameConverter = nameConverter;

    public string Map(ColumnMetadata column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var typeName = column.DbType switch
        {
            CommonDbType.SmallInt => "short",
            CommonDbType.Integer => "int",
            CommonDbType.BigInt => "long",
            CommonDbType.Decimal => "decimal",
            CommonDbType.Float => "float",
            CommonDbType.Double => "double",
            CommonDbType.Boolean => "bool",
            CommonDbType.Char or CommonDbType.VarChar or CommonDbType.Text or CommonDbType.Json => "string",
            CommonDbType.Date => "DateOnly",
            CommonDbType.Time => "TimeOnly",
            CommonDbType.DateTime => "DateTime",
            CommonDbType.Guid => "Guid",
            CommonDbType.Blob or CommonDbType.Binary => "byte[]",
            CommonDbType.Enum => nameConverter.ToPascalCase(column.NativeType),
            CommonDbType.Unknown => "object",
            _ => "object"
        };

        return column.IsNullable && IsNullableAnnotationRequired(typeName)
            ? $"{typeName}?"
            : typeName;
    }

    public static bool RequiresUnknownWarning(ColumnMetadata column)
        => column.DbType == CommonDbType.Unknown;

    public static bool RequiresSystemUsing(string typeName)
        => typeName.TrimEnd('?') is "DateOnly" or "TimeOnly" or "DateTime" or "Guid";

    private static bool IsNullableAnnotationRequired(string typeName)
        => true;
}
