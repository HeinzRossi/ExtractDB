using System.Windows;
using ExtractDB.Wpf.ViewModels;

namespace ExtractDB.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
