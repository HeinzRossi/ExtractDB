using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers.IntegrationTests.SqlServer;

public sealed class SqlServerMetadataProviderMappingTests
{
    [Theory]
    [InlineData("tinyint", CommonDbType.SmallInt)]
    [InlineData("smallint", CommonDbType.SmallInt)]
    [InlineData("int", CommonDbType.Integer)]
    [InlineData("bigint", CommonDbType.BigInt)]
    [InlineData("decimal", CommonDbType.Decimal)]
    [InlineData("numeric", CommonDbType.Decimal)]
    [InlineData("money", CommonDbType.Decimal)]
    [InlineData("real", CommonDbType.Float)]
    [InlineData("float", CommonDbType.Double)]
    [InlineData("bit", CommonDbType.Boolean)]
    [InlineData("char", CommonDbType.Char)]
    [InlineData("nchar", CommonDbType.Char)]
    [InlineData("varchar", CommonDbType.VarChar)]
    [InlineData("nvarchar", CommonDbType.VarChar)]
    [InlineData("text", CommonDbType.Text)]
    [InlineData("ntext", CommonDbType.Text)]
    [InlineData("date", CommonDbType.Date)]
    [InlineData("time", CommonDbType.Time)]
    [InlineData("datetime", CommonDbType.DateTime)]
    [InlineData("datetime2", CommonDbType.DateTime)]
    [InlineData("smalldatetime", CommonDbType.DateTime)]
    [InlineData("binary", CommonDbType.Binary)]
    [InlineData("varbinary", CommonDbType.Binary)]
    [InlineData("image", CommonDbType.Blob)]
    [InlineData("uniqueidentifier", CommonDbType.Guid)]
    [InlineData("hierarchyid", CommonDbType.Unknown)]
    public void MapNativeType_maps_documented_sql_server_types(
        string nativeType,
        CommonDbType expected)
    {
        Assert.Equal(expected, SqlServerMetadataProvider.MapNativeType(nativeType));
    }

    [Theory]
    [InlineData("nchar", true)]
    [InlineData("nvarchar", true)]
    [InlineData("ntext", true)]
    [InlineData("char", false)]
    [InlineData("varchar", false)]
    [InlineData("text", false)]
    public void IsUnicodeType_identifies_sql_server_unicode_types(
        string nativeType,
        bool expected)
    {
        Assert.Equal(expected, SqlServerMetadataProvider.IsUnicodeType(nativeType));
    }

    [Theory]
    [InlineData("('abc')", DefaultValueKind.Literal)]
    [InlineData("(N'abc')", DefaultValueKind.Literal)]
    [InlineData("((123))", DefaultValueKind.Literal)]
    [InlineData("((1))", DefaultValueKind.Literal)]
    [InlineData("(sysdatetime())", DefaultValueKind.SystemFunction)]
    [InlineData("(getdate())", DefaultValueKind.SystemFunction)]
    [InlineData("(newid())", DefaultValueKind.SystemFunction)]
    [InlineData("(NEXT VALUE FOR dbo.seq_codigo)", DefaultValueKind.Sequence)]
    [InlineData("([valor]+[imposto])", DefaultValueKind.Expression)]
    public void ParseDefaultValue_classifies_sql_server_defaults(
        string expression,
        DefaultValueKind expected)
    {
        var defaultValue = SqlServerMetadataProvider.ParseDefaultValue(expression);

        Assert.NotNull(defaultValue);
        Assert.Equal(expected, defaultValue.Kind);
        Assert.Equal(expression, defaultValue.RawExpression);
    }

    [Fact]
    public void BuildValueGeneration_detects_identity()
    {
        var valueGeneration = SqlServerMetadataProvider.BuildValueGeneration(true);

        Assert.Equal(ValueGenerationStrategy.Identity, valueGeneration.Strategy);
    }
}
