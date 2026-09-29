using System.Windows;
using ExtractDB.Wpf.ViewModels;

namespace ExtractDB.Wpf;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        this.viewModel = viewModel;

        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.PasswordBox passwordBox)
        {
            viewModel.Connection.Password = passwordBox.Password;
        }
    }
}
