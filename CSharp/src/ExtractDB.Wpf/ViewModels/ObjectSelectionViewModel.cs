using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.ViewModels;

public sealed class ObjectSelectionViewModel : ObservableObject
{
    private string searchText = string.Empty;
    private string objectTypeFilter = AllFilter;

    public const string AllFilter = "All";

    public IReadOnlyList<string> ObjectTypeFilters { get; } =
    [
        AllFilter,
        nameof(DatabaseObjectType.Table),
        nameof(DatabaseObjectType.View),
        nameof(DatabaseObjectType.Procedure),
        nameof(DatabaseObjectType.Trigger)
    ];

    public ObservableCollection<SelectableDatabaseObjectViewModel> Items { get; } = [];

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                OnPropertyChanged(nameof(VisibleItems));
            }
        }
    }

    public string ObjectTypeFilter
    {
        get => objectTypeFilter;
        set
        {
            if (SetProperty(ref objectTypeFilter, value))
            {
                OnPropertyChanged(nameof(VisibleItems));
            }
        }
    }

    public IReadOnlyList<SelectableDatabaseObjectViewModel> VisibleItems
        => Items.Where(IsVisible).ToArray();

    public IReadOnlyList<string> SelectedTables
        => Items
            .Where(item => item.IsSelected && item.ObjectType == DatabaseObjectType.Table)
            .Select(item => item.Name)
            .ToArray();

    public void Load(DatabaseMetadata metadata)
    {
        Items.Clear();

        foreach (var table in metadata.Tables)
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Table, table.Schema, table.Name));
        }

        foreach (var view in metadata.Views)
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.View, view.Schema, view.Name));
        }

        foreach (var procedure in metadata.Procedures)
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Procedure, procedure.Schema, procedure.Name));
        }

        foreach (var trigger in metadata.Triggers)
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Trigger, trigger.Schema, trigger.Name));
        }

        OnPropertyChanged(nameof(VisibleItems));
    }

    public void SelectAllVisible()
    {
        foreach (var item in VisibleItems)
        {
            item.IsSelected = true;
        }
    }

    public void UnselectAllVisible()
    {
        foreach (var item in VisibleItems)
        {
            item.IsSelected = false;
        }
    }

    public void InvertVisible()
    {
        foreach (var item in VisibleItems)
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    private bool IsVisible(SelectableDatabaseObjectViewModel item)
    {
        var matchesType = ObjectTypeFilter == AllFilter
            || string.Equals(item.ObjectType.ToString(), ObjectTypeFilter, StringComparison.OrdinalIgnoreCase);
        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || item.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        return matchesType && matchesSearch;
    }
}
