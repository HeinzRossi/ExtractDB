using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtractDB.Wpf.ViewModels;

public sealed class GenerationConfigurationViewModel : ObservableObject
{
    private string namespaceBase = "ExtractDB.Generated";
    private string outputDirectory = string.Empty;
    private string dataExportConfigurationPath = string.Empty;

    public string NamespaceBase
    {
        get => namespaceBase;
        set => SetProperty(ref namespaceBase, value);
    }

    public string OutputDirectory
    {
        get => outputDirectory;
        set => SetProperty(ref outputDirectory, value);
    }

    public string DataExportConfigurationPath
    {
        get => dataExportConfigurationPath;
        set => SetProperty(ref dataExportConfigurationPath, value);
    }
}
