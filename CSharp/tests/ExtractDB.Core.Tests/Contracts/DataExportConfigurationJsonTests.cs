using ExtractDB.Core.Contracts;

namespace ExtractDB.Core.Tests.Contracts;

public sealed class DataExportConfigurationJsonTests
{
    [Fact]
    public void Parse_reads_grouped_table_names_by_schema()
    {
        const string json = """
            {
              "maxRowsPerTable": 10000,
              "tables": [
                { "schema": "public", "name": ["cliente_tipo", "cidade"] },
                { "name": ["pais"] }
              ]
            }
            """;

        var configuration = DataExportConfigurationJson.Parse(json);

        Assert.Equal(10000, configuration.MaxRowsPerTable);
        Assert.Equal("public", configuration.Tables[0].Schema);
        Assert.Equal(["cliente_tipo", "cidade"], configuration.Tables[0].Name);
        Assert.Null(configuration.Tables[1].Schema);
        Assert.Equal(["pais"], configuration.Tables[1].Name);
    }

    [Fact]
    public void Parse_uses_default_limit_when_limit_is_invalid()
    {
        const string json = """
            {
              "maxRowsPerTable": 0,
              "tables": [
                { "schema": "", "name": ["CIDADE"] }
              ]
            }
            """;

        var configuration = DataExportConfigurationJson.Parse(json);

        Assert.Equal(DataExportConfiguration.DefaultMaxRowsPerTable, configuration.MaxRowsPerTable);
    }
}
