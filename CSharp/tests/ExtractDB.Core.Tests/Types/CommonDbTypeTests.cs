using ExtractDB.Core.Types;

namespace ExtractDB.Core.Tests.Types;

public sealed class CommonDbTypeTests
{
    [Fact]
    public void CommonDbType_contains_documented_values()
    {
        var names = Enum.GetNames<CommonDbType>();

        Assert.Equal(
            [
                "Unknown",
                "SmallInt",
                "Integer",
                "BigInt",
                "Decimal",
                "Float",
                "Double",
                "Boolean",
                "Char",
                "VarChar",
                "Text",
                "Date",
                "Time",
                "DateTime",
                "Binary",
                "Blob",
                "Guid",
                "Json",
                "Enum"
            ],
            names);
    }
}
