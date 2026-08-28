using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp.Tests;

public sealed class CSharpTypeMapperTests
{
    private readonly CSharpTypeMapper mapper = new();

    [Theory]
    [InlineData(CommonDbType.SmallInt, "smallint", false, "short")]
    [InlineData(CommonDbType.Integer, "integer", false, "int")]
    [InlineData(CommonDbType.BigInt, "bigint", false, "long")]
    [InlineData(CommonDbType.Decimal, "numeric", false, "decimal")]
    [InlineData(CommonDbType.Float, "real", false, "float")]
    [InlineData(CommonDbType.Double, "double precision", false, "double")]
    [InlineData(CommonDbType.Boolean, "boolean", false, "bool")]
    [InlineData(CommonDbType.Char, "char", false, "string")]
    [InlineData(CommonDbType.VarChar, "varchar", false, "string")]
    [InlineData(CommonDbType.Text, "text", false, "string")]
    [InlineData(CommonDbType.Json, "jsonb", false, "string")]
    [InlineData(CommonDbType.Date, "date", false, "DateOnly")]
    [InlineData(CommonDbType.Time, "time", false, "TimeOnly")]
    [InlineData(CommonDbType.DateTime, "timestamp", false, "DateTime")]
    [InlineData(CommonDbType.Guid, "uuid", false, "Guid")]
    [InlineData(CommonDbType.Blob, "blob", false, "byte[]")]
    [InlineData(CommonDbType.Binary, "bytea", false, "byte[]")]
    [InlineData(CommonDbType.Enum, "status_pedido", false, "StatusPedido")]
    [InlineData(CommonDbType.Unknown, "weird", false, "object")]
    [InlineData(CommonDbType.Integer, "integer", true, "int?")]
    [InlineData(CommonDbType.Decimal, "numeric", true, "decimal?")]
    [InlineData(CommonDbType.VarChar, "varchar", true, "string?")]
    [InlineData(CommonDbType.Blob, "blob", true, "byte[]?")]
    [InlineData(CommonDbType.Unknown, "weird", true, "object?")]
    public void Map_returns_documented_csharp_type(
        CommonDbType dbType,
        string nativeType,
        bool isNullable,
        string expected)
    {
        var column = Column(dbType, nativeType, isNullable);

        var typeName = mapper.Map(column);

        Assert.Equal(expected, typeName);
    }

    [Fact]
    public void RequiresUnknownWarning_returns_true_only_for_unknown_type()
    {
        Assert.True(CSharpTypeMapper.RequiresUnknownWarning(Column(CommonDbType.Unknown, "weird")));
        Assert.False(CSharpTypeMapper.RequiresUnknownWarning(Column(CommonDbType.Integer, "integer")));
    }

    private static ColumnMetadata Column(
        CommonDbType dbType,
        string nativeType,
        bool isNullable = false)
        => new()
        {
            Name = "COLUNA",
            OrdinalPosition = 1,
            NativeType = nativeType,
            DbType = dbType,
            IsNullable = isNullable
        };
}
