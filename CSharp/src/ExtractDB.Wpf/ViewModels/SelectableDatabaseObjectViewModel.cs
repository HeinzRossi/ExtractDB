using CommunityToolkit.Mvvm.ComponentModel;
using ExtractDB.Core.Types;

namespace ExtractDB.Wpf.ViewModels;

public sealed class SelectableDatabaseObjectViewModel : ObservableObject
{
    private bool isSelected = true;

    public SelectableDatabaseObjectViewModel(
        DatabaseObjectType objectType,
        string? schema,
        string name)
    {
        ObjectType = objectType;
        Schema = schema;
        Name = name;
    }

    public DatabaseObjectType ObjectType { get; }

    public string? Schema { get; }

    public string Name { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Schema)
        ? Name
        : $"{Schema}.{Name}";

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
