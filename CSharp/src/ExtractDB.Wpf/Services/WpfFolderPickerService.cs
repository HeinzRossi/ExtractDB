using System.IO;
using Microsoft.Win32;

namespace ExtractDB.Wpf.Services;

public sealed class WpfFolderPickerService : IFolderPickerService
{
    public string? PickFolder(string? initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecione a pasta de saída",
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
