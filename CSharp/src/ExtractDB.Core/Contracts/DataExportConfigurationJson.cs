using System.Text.Json;

namespace ExtractDB.Core.Contracts;

public static class DataExportConfigurationJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static DataExportConfiguration Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var configuration = JsonSerializer.Deserialize<DataExportConfiguration>(json, Options)
            ?? new DataExportConfiguration();

        return configuration with
        {
            MaxRowsPerTable = configuration.MaxRowsPerTable > 0
                ? configuration.MaxRowsPerTable
                : DataExportConfiguration.DefaultMaxRowsPerTable,
            Tables = configuration.Tables
                .Where(group => group.Name.Count > 0)
                .ToArray()
        };
    }
}
