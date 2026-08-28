using ExtractDB.Core.Contracts;
using ExtractDB.Core.Types;
using ExtractDB.Providers.MetadataProviders;
using Microsoft.Data.SqlClient;

namespace ExtractDB.Providers.IntegrationTests.SqlServer;

public sealed class SqlServerMetadataProviderIntegrationTests
{
    [Fact]
    public async Task ReadAsync_reads_sprint4_fixture_when_connection_is_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("EXTRACTDB_SQLSERVER_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await ApplyFixtureAsync(connectionString, CancellationToken.None);

        var provider = new SqlServerMetadataProvider();
        var progress = new ProgressRecorder();

        var metadata = await provider.ReadAsync(
            CreateOptions(connectionString),
            progress,
            CancellationToken.None);

        Assert.Equal(DatabaseProvider.SqlServer, metadata.Provider);
        Assert.Equal("dbo", metadata.DefaultSchema);
        Assert.Contains(metadata.Tables, table => table.Name == "cliente");
        Assert.Contains(metadata.Tables, table => table.Name == "tabela_sem_pk" && table.PrimaryKey is null);
        Assert.Contains(metadata.Views, view => view.Name == "vw_cliente");
        Assert.Contains(metadata.Procedures, procedure => procedure.Name == "sp_cliente_nome");
        Assert.Contains(metadata.Triggers, trigger => trigger.Name == "tr_cliente_bu");
        Assert.Empty(metadata.Sequences);
        Assert.Empty(metadata.Enums);

        var cliente = metadata.Tables.Single(table => table.Name == "cliente");
        Assert.Equal("pk_cliente", cliente.PrimaryKey?.Name);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "id_cliente"
            && column.IsPrimaryKey
            && column.ValueGeneration?.Strategy == ValueGenerationStrategy.Identity);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "nome_unicode"
            && column.DbType == CommonDbType.VarChar
            && column.IsUnicode);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "legado"
            && column.DbType == CommonDbType.Unknown);
        Assert.Contains(cliente.Columns, column =>
            column.Name == "email"
            && column.DefaultValue?.Kind == DefaultValueKind.Literal);

        var pedido = metadata.Tables.Single(table => table.Name == "pedido");
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["id_cliente"])
            && fk.TargetTable == "cliente"
            && !fk.IsNullable);
        Assert.Contains(pedido.ForeignKeys, fk =>
            fk.SourceColumns.SequenceEqual(["id_cliente_indicador"])
            && fk.TargetTable == "cliente"
            && fk.IsNullable);

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
            "SqlServer",
            "Fixtures",
            "sprint4_schema.sql");

        var sql = await File.ReadAllTextAsync(fixturePath, cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DatabaseConnectionOptions CreateOptions(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        return new DatabaseConnectionOptions
        {
            Provider = DatabaseProvider.SqlServer,
            Server = builder.DataSource,
            Port = DatabaseConnectionOptions.GetDefaultPort(DatabaseProvider.SqlServer),
            Database = builder.InitialCatalog,
            UserName = builder.UserID,
            Password = builder.Password
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
