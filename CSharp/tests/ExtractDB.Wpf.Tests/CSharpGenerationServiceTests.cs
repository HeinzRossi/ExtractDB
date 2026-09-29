using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Core.Contracts;
using ExtractDB.Generators.CSharp;
using ExtractDB.Providers.DataExport;
using ExtractDB.Wpf.Services;

namespace ExtractDB.Wpf.Tests;

public sealed class CSharpGenerationServiceTests
{
    [Fact]
    public async Task Generate_passes_only_selected_objects_to_csharp_generator()
    {
        using var directory = TempDirectory.Create();
        var service = new CSharpGenerationService(new CSharpDatabaseGenerator(), new DataExportProviderFactory());

        var result = await service.GenerateAsync(
            CreateMetadata(),
            new GenerationRequest
            {
                Provider = DatabaseProvider.PostgreSql,
                NamespaceBase = "Demo",
                OutputDirectory = directory.Path,
                SelectedObjects =
                [
                    new SelectedObject(DatabaseObjectType.Table, "public", "cliente"),
                    new SelectedObject(DatabaseObjectType.View, "public", "vw_cliente"),
                    new SelectedObject(DatabaseObjectType.Sequence, "public", "seq_cliente")
                ]
            },
            CancellationToken.None);

        Assert.Equal(0, result.ErrorCount);
        Assert.True(File.Exists(Path.Combine(directory.Path, "Models", "Cliente.cs")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "Models", "Pedido.cs")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Views", "public", "vw_cliente.sql")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "Scripts", "Sequences", "public", "seq_cliente.sql")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "Scripts", "Procedures", "public", "sp_cliente.sql")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "Scripts", "Triggers", "tr_cliente.sql")));
    }

    private static DatabaseMetadata CreateMetadata()
        => new()
        {
            Provider = DatabaseProvider.PostgreSql,
            DatabaseName = "demo",
            Tables =
            [
                Table("cliente"),
                Table("pedido")
            ],
            Views =
            [
                new ViewMetadata
                {
                    Schema = "public",
                    Name = "vw_cliente",
                    Sql = "select 1"
                }
            ],
            Procedures =
            [
                new ProcedureMetadata
                {
                    Schema = "public",
                    Name = "sp_cliente",
                    Sql = "select 1"
                }
            ],
            Triggers =
            [
                new TriggerMetadata
                {
                    Name = "tr_cliente",
                    Sql = "select 1"
                }
            ],
            Sequences =
            [
                new SequenceMetadata
                {
                    Schema = "public",
                    Name = "seq_cliente",
                    Sql = "create sequence public.seq_cliente start with 0;"
                }
            ]
        };

    private static TableMetadata Table(string name)
        => new()
        {
            Schema = "public",
            Name = name,
            Columns =
            [
                new ColumnMetadata
                {
                    Name = $"id_{name}",
                    OrdinalPosition = 1,
                    NativeType = "integer",
                    DbType = CommonDbType.Integer,
                    IsPrimaryKey = true
                }
            ]
        };
}
