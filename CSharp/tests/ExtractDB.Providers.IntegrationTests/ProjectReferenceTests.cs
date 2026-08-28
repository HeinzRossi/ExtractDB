using ExtractDB.Core.Types;

namespace ExtractDB.Providers.IntegrationTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Provider_tests_can_reference_core_contracts()
    {
        Assert.Equal(DatabaseProvider.PostgreSql, DatabaseProvider.PostgreSql);
    }
}
