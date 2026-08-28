using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp.Tests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Generator_tests_can_reference_core_contracts()
    {
        Assert.Equal(CommonDbType.Integer, CommonDbType.Integer);
    }
}
