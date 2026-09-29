using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;
using FirebirdSql.Data.FirebirdClient;

namespace ExtractDB.Providers.IntegrationTests.Firebird;

public sealed class FirebirdMetadataProviderIntegrationTests
{
    [Fact]
    public async Task ReadAsync_reads_sprint5_fixture_when_connection_is_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("EXTRACTDB_FIREBIRD_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await ApplyFixtureAsync(connectionString, CancellationToken.None);

        var provider = new FirebirdMetadataProvider();
        var progress = new ProgressRecorder();

        var metadata = await provider.ReadAsync(
            CreateOptions(connectionString),
            progress,
            CancellationToken.None);

        Assert.Equal(DatabaseProvider.Firebird, metadata.Provider);
        Assert.Null(metadata.DefaultSchema);
        Assert.Contains(metadata.Tables, table => table.Name == "CLIENTE");
        Assert.Contains(metadata.Tables, table => table.Name == "TABELA_SEM_PK" && table.PrimaryKey is null);
        Assert.Contains(metadata.Sequences, sequence =>
            sequence.Name == "GEN_CODIGO"
            && sequence.Sql.Contains("CREATE SEQUENCE GEN_CODIGO START WITH 0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(metadata.Views, view => view.Name == "VW_CLIENTE");
        Assert.Contains(metadata.Procedures, procedure => procedure.Name == "SP_CLIENTE_NOME");
        Assert.Contains(metadata.Triggers, trigger => trigger.Name == "TR_CLIENTE_BI");
        Assert.Empty(metadata.Enums);

        var cliente = metadata.Tables.Single(table => table.Name == "CLIENTE");
        Assert.Equal("PK_CLIENTE", cliente.PrimaryKey?.Name);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "ID_CLIENTE"
            && column.IsPrimaryKey
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.Identity);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "FOTO"
            && column.DbType == CommonDbType.Blob);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "OBSERVACAO"
            && column.DbType == CommonDbType.Text);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "CODIGO_GUID"
            && column.DbType == CommonDbType.Guid);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "EMAIL"
            && column.DefaultValue?.Kind == DefaultValueKind.Literal);

        var clienteLegado = metadata.Tables.Single(table => table.Name == "CLIENTE_LEGADO");
        Assert.Contains(clienteLegado.Columns, column =>
            column.Name == "CODIGO"
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.None);

        var pedido = metadata.Tables.Single(table => table.Name == "PEDIDO");
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["ID_CLIENTE"])
            && fk.TargetTable == "CLIENTE"
            && !fk.IsNullable);
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["ID_CLIENTE_INDICADOR"])
            && fk.TargetTable == "CLIENTE"
            && fk.IsNullable);
        Assert.Contains(pedido.Columns, column =>
            column.Name == "ID_PEDIDO"
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.None
            && column.DefaultValue?.Kind == DefaultValueKind.Sequence);

        var loteItem = metadata.Tables.Single(table => table.Name == "LOTE_ITEM");
        Assert.Contains(loteItem.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["ID_LOTE", "ITEM"])
            && fk.TargetColumns.SequenceEqual(["ID_LOTE", "ITEM"]));

        var totalPedido = metadata.Tables.Single(table => table.Name == "TOTAL_PEDIDO");
        Assert.Contains(totalPedido.Columns, column =>
            column.Name == "TOTAL"
            && column.IsComputed
            && !string.IsNullOrWhiteSpace(column.ComputedExpression));

        Assert.Equal(
            [
                MetadataProgressStage.Connecting,
                MetadataProgressStage.ReadingTables,
                MetadataProgressStage.ReadingColumns,
                MetadataProgressStage.ReadingPrimaryKeys,
                MetadataProgressStage.ReadingForeignKeys,
                MetadataProgressStage.ReadingSequences,
                MetadataProgressStage.ReadingViews,
                MetadataProgressStage.ReadingProcedures,
                MetadataProgressStage.ReadingTriggers,
                MetadataProgressStage.Completed
            ],
            progress.Stages);
    }

    private static async Task ApplyFixtureAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Firebird",
            "Fixtures",
            "sprint5_schema.sql");

        var sql = await File.ReadAllTextAsync(fixturePath, cancellationToken);

        await using var connection = new FbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var statement in SplitFirebirdScript(sql))
        {
            await using var command = new FbCommand(statement, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IEnumerable<string> SplitFirebirdScript(string sql)
    {
        var terminator = ";";
        var statement = new List<string>();

        foreach (var rawLine in sql.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.Trim();

            if (trimmed.StartsWith("set term", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                terminator = parts[2];
                continue;
            }

            statement.Add(line);

            if (!trimmed.EndsWith(terminator, StringComparison.Ordinal))
            {
                continue;
            }

            statement[^1] = line[..^terminator.Length];
            var text = string.Join(Environment.NewLine, statement).Trim();
            statement.Clear();

            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static DatabaseConnectionOptions CreateOptions(string connectionString)
    {
        var builder = new FbConnectionStringBuilder(connectionString);

        return new DatabaseConnectionOptions
        {
            Provider = DatabaseProvider.Firebird,
            Server = builder.DataSource ?? "localhost",
            Port = builder.Port,
            Database = builder.Database ?? string.Empty,
            UserName = builder.UserID ?? "SYSDBA",
            Password = builder.Password ?? string.Empty
        };
    }

    private sealed class ProgressRecorder : IProgress<MetadataProgress>
    {
        private readonly List<MetadataProgressStage> stages = [];

        public IReadOnlyList<MetadataProgressStage> Stages => stages;

        public void Report(MetadataProgress value)
            => stages.Add(value.Stage);
    }
}
