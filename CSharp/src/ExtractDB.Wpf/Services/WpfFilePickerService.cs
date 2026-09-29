using Microsoft.Win32;

namespace ExtractDB.Wpf.Services;

public sealed class WpfFilePickerService : IFilePickerService
{
    public string? PickJsonFile(string? initialFile)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos JSON (*.json)|*.json|Todos os arquivos (*.*)|*.*",
            FileName = initialFile
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
