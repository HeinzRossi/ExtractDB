using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;

namespace ExtractDB.Core.Tests.Metadata;

public sealed class ColumnMetadataTests
{
    [Fact]
    public void ColumnMetadata_preserves_documented_values()
    {
        var defaultValue = new DefaultValueMetadata
        {
            RawExpression = "CURRENT_TIMESTAMP",
            Kind = DefaultValueKind.SystemFunction
        };

        var valueGeneration = new ValueGenerationMetadata
        {
            Strategy = ValueGenerationStrategy.TriggerSequence,
            SequenceName = "GEN_CLIENTE_ID",
            TriggerName = "TR_CLIENTE_BI"
        };

        var column = new ColumnMetadata
        {
            Name = "VALOR",
            OrdinalPosition = 3,
            NativeType = "numeric",
            DbType = CommonDbType.Decimal,
            Length = 12,
            Precision = 18,
            Scale = 4,
            IsUnicode = true,
            IsNullable = false,
            IsPrimaryKey = true,
            IsComputed = true,
            ComputedExpression = "A + B",
            DefaultValue = defaultValue,
            ValueGeneration = valueGeneration
        };

        Assert.Equal("VALOR", column.Name);
        Assert.Equal(3, column.OrdinalPosition);
        Assert.Equal("numeric", column.NativeType);
        Assert.Equal(CommonDbType.Decimal, column.DbType);
        Assert.Equal(12, column.Length);
        Assert.Equal(18, column.Precision);
        Assert.Equal(4, column.Scale);
        Assert.True(column.IsUnicode);
        Assert.False(column.IsNullable);
        Assert.True(column.IsPrimaryKey);
        Assert.True(column.IsComputed);
        Assert.Equal("A + B", column.ComputedExpression);
        Assert.Same(defaultValue, column.DefaultValue);
        Assert.Same(valueGeneration, column.ValueGeneration);
    }
}
