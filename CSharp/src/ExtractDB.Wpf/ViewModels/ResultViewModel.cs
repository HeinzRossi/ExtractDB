using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtractDB.Core.Contracts;

namespace ExtractDB.Wpf.ViewModels;

public sealed class ResultViewModel : ObservableObject
{
    private int successCount;
    private int warningCount;
    private int errorCount;

    public int SuccessCount
    {
        get => successCount;
        private set => SetProperty(ref successCount, value);
    }

    public int WarningCount
    {
        get => warningCount;
        private set => SetProperty(ref warningCount, value);
    }

    public int ErrorCount
    {
        get => errorCount;
        private set => SetProperty(ref errorCount, value);
    }

    public ObservableCollection<GenerationMessage> Messages { get; } = [];

    public void Load(GenerationResult result)
    {
        SuccessCount = result.SuccessCount;
        WarningCount = result.WarningCount;
        ErrorCount = result.ErrorCount;

        Messages.Clear();
        foreach (var message in result.Messages)
        {
            Messages.Add(message);
        }
    }
}
