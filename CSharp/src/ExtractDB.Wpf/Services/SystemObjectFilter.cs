using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.Services;

public static class SystemObjectFilter
{
    private static readonly string[] FirebirdSystemPrefixes = ["RDB$", "MON$", "SEC$"];

    public static bool IsUserObject(
        DatabaseProvider provider,
        string? schema,
        string name)
        => !IsSystemObject(provider, schema, name);

    public static bool IsSystemObject(
        DatabaseProvider provider,
        string? schema,
        string name)
        => provider switch
        {
            DatabaseProvider.PostgreSql => IsPostgreSqlSystemSchema(schema),
            DatabaseProvider.SqlServer => IsSqlServerSystemSchema(schema),
            DatabaseProvider.Firebird => FirebirdSystemPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };

    private static bool IsPostgreSqlSystemSchema(string? schema)
        => string.Equals(schema, "pg_catalog", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schema, "information_schema", StringComparison.OrdinalIgnoreCase);

    private static bool IsSqlServerSystemSchema(string? schema)
        => string.Equals(schema, "sys", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schema, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);
}
