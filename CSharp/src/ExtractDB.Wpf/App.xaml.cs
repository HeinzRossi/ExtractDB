using System.Windows;
using ExtractDB.Core.Contracts;
using ExtractDB.Generators.CSharp;
using ExtractDB.Providers;
using ExtractDB.Wpf.Services;
using ExtractDB.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractDB.Wpf;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        serviceProvider = services.BuildServiceProvider();
        var window = serviceProvider.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMetadataProviderFactory, MetadataProviderFactory>();
        services.AddSingleton<CSharpDatabaseGenerator>();
        services.AddSingleton<ICSharpGenerationService, CSharpGenerationService>();
        services.AddSingleton<IFolderPickerService, WpfFolderPickerService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
