using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers.IntegrationTests.PostgreSql;

public sealed class PostgreSqlMetadataProviderMappingTests
{
    [Theory]
    [InlineData("smallint", CommonDbType.SmallInt)]
    [InlineData("integer", CommonDbType.Integer)]
    [InlineData("bigint", CommonDbType.BigInt)]
    [InlineData("numeric", CommonDbType.Decimal)]
    [InlineData("decimal", CommonDbType.Decimal)]
    [InlineData("real", CommonDbType.Float)]
    [InlineData("double precision", CommonDbType.Double)]
    [InlineData("boolean", CommonDbType.Boolean)]
    [InlineData("character", CommonDbType.Char)]
    [InlineData("character varying", CommonDbType.VarChar)]
    [InlineData("text", CommonDbType.Text)]
    [InlineData("date", CommonDbType.Date)]
    [InlineData("time without time zone", CommonDbType.Time)]
    [InlineData("timestamp without time zone", CommonDbType.DateTime)]
    [InlineData("bytea", CommonDbType.Blob)]
    [InlineData("uuid", CommonDbType.Guid)]
    [InlineData("json", CommonDbType.Json)]
    [InlineData("jsonb", CommonDbType.Json)]
    [InlineData("timestamp with time zone", CommonDbType.Unknown)]
    public void MapNativeType_maps_documented_postgresql_types(
        string nativeType,
        CommonDbType expected)
    {
        Assert.Equal(expected, PostgreSqlMetadataProvider.MapNativeType(nativeType));
    }

    [Fact]
    public void MapNativeType_maps_user_defined_type_as_enum()
    {
        Assert.Equal(
            CommonDbType.Enum,
            PostgreSqlMetadataProvider.MapNativeType("USER-DEFINED", "status_pedido", "e"));
    }

    [Fact]
    public void MapNativeType_maps_non_enum_user_defined_type_as_unknown()
    {
        Assert.Equal(
            CommonDbType.Unknown,
            PostgreSqlMetadataProvider.MapNativeType("USER-DEFINED", "endereco_composto", "c"));
    }

    [Theory]
    [InlineData("'abc'::text", DefaultValueKind.Literal)]
    [InlineData("123", DefaultValueKind.Literal)]
    [InlineData("true", DefaultValueKind.Literal)]
    [InlineData("CURRENT_TIMESTAMP", DefaultValueKind.SystemFunction)]
    [InlineData("now()", DefaultValueKind.SystemFunction)]
    [InlineData("nextval('seq_codigo'::regclass)", DefaultValueKind.Sequence)]
    [InlineData("(valor + imposto)", DefaultValueKind.Expression)]
    public void ParseDefaultValue_classifies_postgresql_defaults(
        string expression,
        DefaultValueKind expected)
    {
        var defaultValue = PostgreSqlMetadataProvider.ParseDefaultValue(expression);

        Assert.NotNull(defaultValue);
        Assert.Equal(expected, defaultValue.Kind);
        Assert.Equal(expression, defaultValue.RawExpression);
    }

    [Fact]
    public void BuildValueGeneration_detects_identity()
    {
        var valueGeneration = PostgreSqlMetadataProvider.BuildValueGeneration("ALWAYS", null);

        Assert.Equal(ValueGenerationStrategy.Identity, valueGeneration.Strategy);
    }

    [Fact]
    public void BuildValueGeneration_does_not_bind_sequence_default_to_column()
    {
        var valueGeneration = PostgreSqlMetadataProvider.BuildValueGeneration(
            null,
            "nextval('extractdb_sprint3.seq_codigo'::regclass)");

        Assert.Equal(ValueGenerationStrategy.None, valueGeneration.Strategy);
        Assert.Null(valueGeneration.SequenceName);
    }
}
