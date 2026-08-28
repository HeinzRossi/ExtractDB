using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Wpf.Services;

namespace ExtractDB.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IMetadataProviderFactory providerFactory;
    private readonly ICSharpGenerationService generationService;
    private readonly IFolderPickerService folderPickerService;
    private WizardStep currentStep = WizardStep.Connection;
    private bool isBusy;
    private string statusMessage = "Informe os dados de conexão.";
    private string progressMessage = string.Empty;
    private CancellationTokenSource? cancellationTokenSource;
    private DatabaseMetadata? metadata;

    public MainWindowViewModel(
        IMetadataProviderFactory providerFactory,
        ICSharpGenerationService generationService,
        IFolderPickerService folderPickerService)
    {
        this.providerFactory = providerFactory;
        this.generationService = generationService;
        this.folderPickerService = folderPickerService;

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, CanUseConnection);
        ReadMetadataCommand = new AsyncRelayCommand(ReadMetadataAsync, CanUseConnection);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync, CanGenerate);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        BrowseOutputDirectoryCommand = new RelayCommand(BrowseOutputDirectory);
        SelectAllCommand = new RelayCommand(SelectAll);
        UnselectAllCommand = new RelayCommand(UnselectAll);
        InvertSelectionCommand = new RelayCommand(InvertSelection);
        GoToConnectionCommand = new RelayCommand(() => CurrentStep = WizardStep.Connection, () => !IsBusy);
        GoToSelectionCommand = new RelayCommand(() => CurrentStep = WizardStep.ObjectSelection, () => !IsBusy && metadata is not null);
        GoToConfigurationCommand = new RelayCommand(() => CurrentStep = WizardStep.GenerationConfiguration, () => !IsBusy && metadata is not null);

        Connection.PropertyChanged += (_, _) => NotifyCommandStateChanged();
        GenerationConfiguration.PropertyChanged += (_, _) => NotifyCommandStateChanged();
        ObjectSelection.Items.CollectionChanged += OnSelectionItemsChanged;
    }

    public ConnectionViewModel Connection { get; } = new();

    public ObjectSelectionViewModel ObjectSelection { get; } = new();

    public GenerationConfigurationViewModel GenerationConfiguration { get; } = new();

    public ResultViewModel Result { get; } = new();

    public IAsyncRelayCommand TestConnectionCommand { get; }

    public IAsyncRelayCommand ReadMetadataCommand { get; }

    public IAsyncRelayCommand GenerateCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand BrowseOutputDirectoryCommand { get; }

    public IRelayCommand SelectAllCommand { get; }

    public IRelayCommand UnselectAllCommand { get; }

    public IRelayCommand InvertSelectionCommand { get; }

    public IRelayCommand GoToConnectionCommand { get; }

    public IRelayCommand GoToSelectionCommand { get; }

    public IRelayCommand GoToConfigurationCommand { get; }

    public WizardStep CurrentStep
    {
        get => currentStep;
        private set
        {
            if (SetProperty(ref currentStep, value))
            {
                OnPropertyChanged(nameof(IsConnectionStep));
                OnPropertyChanged(nameof(IsMetadataReadingStep));
                OnPropertyChanged(nameof(IsObjectSelectionStep));
                OnPropertyChanged(nameof(IsGenerationConfigurationStep));
                OnPropertyChanged(nameof(IsGenerationStep));
                OnPropertyChanged(nameof(IsResultStep));
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ProgressMessage
    {
        get => progressMessage;
        private set => SetProperty(ref progressMessage, value);
    }

    public bool IsConnectionStep => CurrentStep == WizardStep.Connection;

    public bool IsMetadataReadingStep => CurrentStep == WizardStep.MetadataReading;

    public bool IsObjectSelectionStep => CurrentStep == WizardStep.ObjectSelection;

    public bool IsGenerationConfigurationStep => CurrentStep == WizardStep.GenerationConfiguration;

    public bool IsGenerationStep => CurrentStep == WizardStep.Generation;

    public bool IsResultStep => CurrentStep == WizardStep.Result;

    public async Task TestConnectionAsync()
    {
        await RunBusyAsync(WizardStep.Connection, async cancellationToken =>
        {
            var provider = providerFactory.Create(Connection.Provider);
            await provider.TestConnectionAsync(Connection.ToConnectionOptions(), cancellationToken);
            StatusMessage = "Conexão testada com sucesso.";
        });
    }

    public async Task ReadMetadataAsync()
    {
        await RunBusyAsync(WizardStep.MetadataReading, async cancellationToken =>
        {
            var provider = providerFactory.Create(Connection.Provider);
            var progress = new Progress<MetadataProgress>(ReportProgress);

            metadata = await provider.ReadAsync(Connection.ToConnectionOptions(), progress, cancellationToken);
            ObjectSelection.Load(metadata);
            StatusMessage = $"Metadata lido: {ObjectSelection.Items.Count} objetos selecionáveis.";
            CurrentStep = WizardStep.ObjectSelection;
        });
    }

    public async Task GenerateAsync()
    {
        if (metadata is null)
        {
            StatusMessage = "Leia o metadata antes de gerar.";
            return;
        }

        await RunBusyAsync(WizardStep.Generation, cancellationToken =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var selectedObjects = ObjectSelection.Items
                .Where(item => item.IsSelected)
                .Select(item => new SelectedObject(item.ObjectType, item.Schema, item.Name))
                .ToArray();

            var result = generationService.Generate(
                metadata,
                new GenerationRequest
                {
                    NamespaceBase = GenerationConfiguration.NamespaceBase,
                    OutputDirectory = GenerationConfiguration.OutputDirectory,
                    SelectedObjects = selectedObjects
                });

            Result.Load(result);
            StatusMessage = "Geração concluída.";
            CurrentStep = WizardStep.Result;

            return Task.CompletedTask;
        });
    }

    private async Task RunBusyAsync(
        WizardStep busyStep,
        Func<CancellationToken, Task> action)
    {
        cancellationTokenSource = new CancellationTokenSource();
        IsBusy = true;
        ProgressMessage = string.Empty;
        CurrentStep = busyStep;

        try
        {
            await action(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operação cancelada.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            IsBusy = false;
        }
    }

    private void ReportProgress(MetadataProgress progress)
    {
        var objectName = string.IsNullOrWhiteSpace(progress.ObjectName) ? string.Empty : $" - {progress.ObjectName}";
        var count = progress.CompletedCount is null || progress.TotalCount is null
            ? string.Empty
            : $" ({progress.CompletedCount}/{progress.TotalCount})";

        ProgressMessage = $"{progress.Stage}{objectName}{count}";
    }

    private void Cancel()
        => cancellationTokenSource?.Cancel();

    private void BrowseOutputDirectory()
    {
        var folder = folderPickerService.PickFolder(GenerationConfiguration.OutputDirectory);

        if (!string.IsNullOrWhiteSpace(folder))
        {
            GenerationConfiguration.OutputDirectory = folder;
        }
    }

    private void SelectAll()
        => ObjectSelection.SelectAllVisible();

    private void UnselectAll()
        => ObjectSelection.UnselectAllVisible();

    private void InvertSelection()
        => ObjectSelection.InvertVisible();

    private void OnSelectionItemsChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (SelectableDatabaseObjectViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnSelectionItemPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (SelectableDatabaseObjectViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnSelectionItemPropertyChanged;
            }
        }

        NotifyCommandStateChanged();
    }

    private void OnSelectionItemPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableDatabaseObjectViewModel.IsSelected))
        {
            NotifyCommandStateChanged();
        }
    }

    private bool CanUseConnection()
        => !IsBusy
            && !string.IsNullOrWhiteSpace(Connection.Server)
            && !string.IsNullOrWhiteSpace(Connection.Database)
            && !string.IsNullOrWhiteSpace(Connection.UserName);

    private bool CanGenerate()
        => !IsBusy
            && metadata is not null
            && !string.IsNullOrWhiteSpace(GenerationConfiguration.NamespaceBase)
            && !string.IsNullOrWhiteSpace(GenerationConfiguration.OutputDirectory)
            && ObjectSelection.Items.Any(item => item.IsSelected);

    private void NotifyCommandStateChanged()
    {
        TestConnectionCommand.NotifyCanExecuteChanged();
        ReadMetadataCommand.NotifyCanExecuteChanged();
        GenerateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        GoToConnectionCommand.NotifyCanExecuteChanged();
        GoToSelectionCommand.NotifyCanExecuteChanged();
        GoToConfigurationCommand.NotifyCanExecuteChanged();
    }
}
