using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Wpf.Services;

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
        nameof(DatabaseObjectType.Trigger),
        nameof(DatabaseObjectType.Sequence)
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

        foreach (var table in metadata.Tables.Where(table =>
                     SystemObjectFilter.IsUserObject(metadata.Provider, table.Schema, table.Name)))
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Table, table.Schema, table.Name));
        }

        foreach (var view in metadata.Views.Where(view =>
                     SystemObjectFilter.IsUserObject(metadata.Provider, view.Schema, view.Name)))
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.View, view.Schema, view.Name));
        }

        foreach (var procedure in metadata.Procedures.Where(procedure =>
                     SystemObjectFilter.IsUserObject(metadata.Provider, procedure.Schema, procedure.Name)))
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Procedure, procedure.Schema, procedure.Name));
        }

        foreach (var trigger in metadata.Triggers.Where(trigger =>
                     SystemObjectFilter.IsUserObject(metadata.Provider, trigger.Schema, trigger.Name)))
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Trigger, trigger.Schema, trigger.Name));
        }

        foreach (var sequence in metadata.Sequences.Where(sequence =>
                     SystemObjectFilter.IsUserObject(metadata.Provider, sequence.Schema, sequence.Name)))
        {
            Items.Add(new SelectableDatabaseObjectViewModel(DatabaseObjectType.Sequence, sequence.Schema, sequence.Name));
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
