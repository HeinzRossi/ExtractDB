using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Wpf.ViewModels;

namespace ExtractDB.Wpf.Tests;

public sealed class ObjectSelectionViewModelTests
{
    [Fact]
    public void Load_adds_selectable_tables_views_procedures_and_triggers()
    {
        var viewModel = new ObjectSelectionViewModel();

        viewModel.Load(CreateMetadata());

        Assert.Equal(4, viewModel.Items.Count);
        Assert.All(viewModel.Items, item => Assert.True(item.IsSelected));
    }

    [Fact]
    public void Search_and_type_filter_limit_visible_items()
    {
        var viewModel = new ObjectSelectionViewModel();
        viewModel.Load(CreateMetadata());

        viewModel.ObjectTypeFilter = nameof(DatabaseObjectType.View);
        viewModel.SearchText = "cliente";

        var visible = viewModel.VisibleItems;

        Assert.Single(visible);
        Assert.Equal(DatabaseObjectType.View, visible[0].ObjectType);
        Assert.Equal("vw_cliente", visible[0].Name);
    }

    [Fact]
    public void Selection_commands_apply_only_to_visible_items()
    {
        var viewModel = new ObjectSelectionViewModel();
        viewModel.Load(CreateMetadata());
        viewModel.ObjectTypeFilter = nameof(DatabaseObjectType.Table);

        viewModel.UnselectAllVisible();

        Assert.False(viewModel.Items.Single(item => item.ObjectType == DatabaseObjectType.Table).IsSelected);
        Assert.True(viewModel.Items.Single(item => item.ObjectType == DatabaseObjectType.View).IsSelected);

        viewModel.InvertVisible();

        Assert.True(viewModel.Items.Single(item => item.ObjectType == DatabaseObjectType.Table).IsSelected);

        viewModel.SelectAllVisible();

        Assert.All(viewModel.Items, item => Assert.True(item.IsSelected));
    }

    private static DatabaseMetadata CreateMetadata()
        => new()
        {
            Provider = DatabaseProvider.PostgreSql,
            DatabaseName = "demo",
            Tables =
            [
                new TableMetadata
                {
                    Schema = "public",
                    Name = "cliente",
                    Columns = []
                }
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
            ]
        };
}
