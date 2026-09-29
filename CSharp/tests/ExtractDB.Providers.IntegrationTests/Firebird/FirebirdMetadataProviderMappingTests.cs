using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;

namespace ExtractDB.Providers.IntegrationTests.Firebird;

public sealed class FirebirdMetadataProviderMappingTests
{
    [Theory]
    [InlineData(7, 0, CommonDbType.SmallInt)]
    [InlineData(8, 0, CommonDbType.Integer)]
    [InlineData(16, 0, CommonDbType.BigInt)]
    [InlineData(7, 1, CommonDbType.Decimal)]
    [InlineData(8, 2, CommonDbType.Decimal)]
    [InlineData(16, 1, CommonDbType.Decimal)]
    [InlineData(10, 0, CommonDbType.Float)]
    [InlineData(27, 0, CommonDbType.Double)]
    [InlineData(23, 0, CommonDbType.Boolean)]
    [InlineData(14, 0, CommonDbType.Char)]
    [InlineData(37, 0, CommonDbType.VarChar)]
    [InlineData(12, 0, CommonDbType.Date)]
    [InlineData(13, 0, CommonDbType.Time)]
    [InlineData(35, 0, CommonDbType.DateTime)]
    [InlineData(261, 0, CommonDbType.Blob)]
    [InlineData(261, 1, CommonDbType.Text)]
    [InlineData(999, 0, CommonDbType.Unknown)]
    public void MapFieldType_maps_documented_firebird_types(
        int fieldType,
        int fieldSubType,
        CommonDbType expected)
    {
        Assert.Equal(expected, FirebirdMetadataProvider.MapFieldType(fieldType, fieldSubType));
    }

    [Fact]
    public void MapFieldType_detects_guid_convention()
    {
        Assert.Equal(
            CommonDbType.Guid,
            FirebirdMetadataProvider.MapFieldType(14, 0, 16, "OCTETS"));
    }

    [Theory]
    [InlineData("'abc'", DefaultValueKind.Literal)]
    [InlineData("123", DefaultValueKind.Literal)]
    [InlineData("true", DefaultValueKind.Literal)]
    [InlineData("CURRENT_TIMESTAMP", DefaultValueKind.SystemFunction)]
    [InlineData("CURRENT_DATE", DefaultValueKind.SystemFunction)]
    [InlineData("next value for GEN_CODIGO", DefaultValueKind.Sequence)]
    [InlineData("gen_id(GEN_CODIGO, 1)", DefaultValueKind.Sequence)]
    [InlineData("(VALOR + IMPOSTO)", DefaultValueKind.Expression)]
    public void ParseDefaultValue_classifies_firebird_defaults(
        string expression,
        DefaultValueKind expected)
    {
        var defaultValue = FirebirdMetadataProvider.ParseDefaultValue(expression);

        Assert.NotNull(defaultValue);
        Assert.Equal(expected, defaultValue.Kind);
        Assert.Equal(expression, defaultValue.RawExpression);
    }

    [Fact]
    public void BuildValueGeneration_detects_identity()
    {
        var valueGeneration = FirebirdMetadataProvider.BuildValueGeneration(true, null);

        Assert.Equal(ValueGenerationStrategy.Identity, valueGeneration.Strategy);
    }

    [Fact]
    public void BuildValueGeneration_does_not_bind_sequence_default_to_column()
    {
        var valueGeneration = FirebirdMetadataProvider.BuildValueGeneration(false, "next value for GEN_CODIGO");

        Assert.Equal(ValueGenerationStrategy.None, valueGeneration.Strategy);
        Assert.Null(valueGeneration.SequenceName);
    }

    [Fact]
    public void BuildValueGeneration_does_not_bind_trigger_sequence_to_column()
    {
        var valueGeneration = FirebirdMetadataProvider.BuildValueGeneration(
            false,
            null,
            "CODIGO",
            [("TR_CLIENTE_BI", "new.CODIGO = gen_id(GEN_CODIGO, 1);")]);

        Assert.Equal(ValueGenerationStrategy.None, valueGeneration.Strategy);
        Assert.Null(valueGeneration.SequenceName);
        Assert.Null(valueGeneration.TriggerName);
    }
}
