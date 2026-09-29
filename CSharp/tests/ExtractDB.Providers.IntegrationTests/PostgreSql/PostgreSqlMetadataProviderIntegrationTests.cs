using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;
using Npgsql;

namespace ExtractDB.Providers.IntegrationTests.PostgreSql;

public sealed class PostgreSqlMetadataProviderIntegrationTests
{
    [Fact]
    public async Task ReadAsync_reads_sprint3_fixture_when_connection_is_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("EXTRACTDB_POSTGRESQL_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await ApplyFixtureAsync(connectionString, CancellationToken.None);

        var provider = new PostgreSqlMetadataProvider();
        var progress = new ProgressRecorder();

        var metadata = await provider.ReadAsync(
            CreateOptions(connectionString),
            progress,
            CancellationToken.None);

        Assert.Equal(DatabaseProvider.PostgreSql, metadata.Provider);
        Assert.Equal("public", metadata.DefaultSchema);
        Assert.Contains(metadata.Tables, table => table.Name == "cliente");
        Assert.Contains(metadata.Tables, table => table.Name == "tabela_sem_pk" && table.PrimaryKey is null);
        Assert.Contains(metadata.Sequences, sequence =>
            sequence.Name == "seq_codigo"
            && sequence.Sql.Contains("CREATE SEQUENCE public.seq_codigo START WITH 0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(metadata.Views, view => view.Name == "vw_cliente");
        Assert.Contains(metadata.Procedures, procedure => procedure.Name == "fn_cliente_nome");
        Assert.Contains(metadata.Triggers, trigger => trigger.Name == "tr_cliente_bu");
        Assert.Contains(metadata.Enums, enumType => enumType.Name == "status_pedido");

        var cliente = metadata.Tables.Single(table => table.Name == "cliente");
        Assert.Equal("cliente_pkey", cliente.PrimaryKey?.Name);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "id_cliente"
            && column.IsPrimaryKey
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.Identity);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "status"
            && column.DbType == CommonDbType.Enum
            && column.DefaultValue?.Kind == DefaultValueKind.Literal);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "legacy"
            && column.DbType == CommonDbType.Unknown);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "atualizado_em"
            && column.DbType == CommonDbType.Unknown);

        var pedido = metadata.Tables.Single(table => table.Name == "pedido");
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["id_cliente"])
            && fk.TargetTable == "cliente"
            && !fk.IsNullable);
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["id_cliente_indicador"])
            && fk.TargetTable == "cliente"
            && fk.IsNullable);
        Assert.Contains(pedido.Columns, column =>
            column.Name == "id_pedido"
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.None
            && column.DefaultValue?.Kind == DefaultValueKind.Sequence);

        var loteItem = metadata.Tables.Single(table => table.Name == "lote_item");
        Assert.Contains(loteItem.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["id_lote", "item"])
            && fk.TargetColumns.SequenceEqual(["id_lote", "item"]));

        var totalPedido = metadata.Tables.Single(table => table.Name == "total_pedido");
        Assert.Contains(totalPedido.Columns, column =>
            column.Name == "total"
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
            "PostgreSql",
            "Fixtures",
            "sprint3_schema.sql");

        var sql = await File.ReadAllTextAsync(fixturePath, cancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DatabaseConnectionOptions CreateOptions(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        return new DatabaseConnectionOptions
        {
            Provider = DatabaseProvider.PostgreSql,
            Server = builder.Host ?? "localhost",
            Port = builder.Port,
            Database = builder.Database ?? "postgres",
            UserName = builder.Username ?? "postgres",
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
