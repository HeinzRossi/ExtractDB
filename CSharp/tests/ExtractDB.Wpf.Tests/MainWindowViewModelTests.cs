using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Wpf.Services;
using ExtractDB.Wpf.ViewModels;

namespace ExtractDB.Wpf.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ReadMetadata_loads_objects_and_moves_to_selection_step()
    {
        var provider = new FakeMetadataProvider();
        var viewModel = CreateViewModel(provider);
        FillConnection(viewModel.Connection);

        await viewModel.ReadMetadataAsync();

        Assert.Equal(WizardStep.ObjectSelection, viewModel.CurrentStep);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(2, viewModel.ObjectSelection.Items.Count);
        Assert.Contains("Metadata lido", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Generate_uses_selected_objects_and_loads_result()
    {
        var provider = new FakeMetadataProvider();
        var generation = new FakeGenerationService();
        var viewModel = CreateViewModel(provider, generation);
        FillConnection(viewModel.Connection);
        viewModel.GenerationConfiguration.NamespaceBase = "Demo";
        viewModel.GenerationConfiguration.OutputDirectory = Path.GetTempPath();
        await viewModel.ReadMetadataAsync();
        viewModel.ObjectSelection.Items.Single(item => item.ObjectType == DatabaseObjectType.View).IsSelected = false;

        await viewModel.GenerateAsync();

        Assert.Equal(WizardStep.Result, viewModel.CurrentStep);
        Assert.Equal(1, viewModel.Result.SuccessCount);
        Assert.Single(generation.Request!.SelectedObjects);
        Assert.Equal(DatabaseObjectType.Table, generation.Request.SelectedObjects[0].ObjectType);
    }

    [Fact]
    public async Task Cancel_stops_running_operation_and_releases_busy_state()
    {
        var provider = new FakeMetadataProvider
        {
            DelayUntilCanceled = true
        };
        var viewModel = CreateViewModel(provider);
        FillConnection(viewModel.Connection);

        var readTask = viewModel.ReadMetadataAsync();
        await provider.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(viewModel.IsBusy);

        viewModel.CancelCommand.Execute(null);
        await readTask;

        Assert.False(viewModel.IsBusy);
        Assert.Equal("Operação cancelada.", viewModel.StatusMessage);
    }

    [Fact]
    public void ResultViewModel_loads_generation_counts_and_messages()
    {
        var result = new ResultViewModel();

        result.Load(new GenerationResult
        {
            SuccessCount = 2,
            WarningCount = 1,
            ErrorCount = 1,
            Messages =
            [
                new GenerationMessage
                {
                    Severity = GenerationMessageSeverity.Warning,
                    ObjectType = DatabaseObjectType.Table,
                    ObjectName = "cliente",
                    Stage = "Test",
                    Message = "warning"
                }
            ]
        });

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.WarningCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Single(result.Messages);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeMetadataProvider provider,
        ICSharpGenerationService? generationService = null)
        => new(
            new FakeMetadataProviderFactory(provider),
            generationService ?? new FakeGenerationService(),
            new FakeFolderPickerService());

    private static void FillConnection(ConnectionViewModel connection)
    {
        connection.Server = "localhost";
        connection.Database = "demo";
        connection.UserName = "user";
        connection.Password = "password";
    }

    private sealed class FakeMetadataProviderFactory : IMetadataProviderFactory
    {
        private readonly IDatabaseMetadataProvider provider;

        public FakeMetadataProviderFactory(IDatabaseMetadataProvider provider)
            => this.provider = provider;

        public IDatabaseMetadataProvider Create(DatabaseProvider provider)
            => this.provider;
    }

    private sealed class FakeMetadataProvider : IDatabaseMetadataProvider
    {
        public TaskCompletionSource ReadStarted { get; } = new();

        public bool DelayUntilCanceled { get; init; }

        public string ProviderName => "Fake";

        public Task TestConnectionAsync(
            DatabaseConnectionOptions options,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public async Task<DatabaseMetadata> ReadAsync(
            DatabaseConnectionOptions options,
            IProgress<MetadataProgress>? progress,
            CancellationToken cancellationToken)
        {
            ReadStarted.TrySetResult();

            if (DelayUntilCanceled)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            progress?.Report(new MetadataProgress
            {
                Stage = MetadataProgressStage.Completed,
                Message = "done"
            });

            return new DatabaseMetadata
            {
                Provider = DatabaseProvider.PostgreSql,
                DatabaseName = "demo",
                Tables =
                [
                    new TableMetadata
                    {
                        Schema = "public",
                        Name = "cliente",
                        Columns = []
                    }
                ],
                Views =
                [
                    new ViewMetadata
                    {
                        Schema = "public",
                        Name = "vw_cliente",
                        Sql = "select 1"
                    }
                ]
            };
        }
    }

    private sealed class FakeGenerationService : ICSharpGenerationService
    {
        public GenerationRequest? Request { get; private set; }

        public GenerationResult Generate(
            DatabaseMetadata metadata,
            GenerationRequest request)
        {
            Request = request;

            return new GenerationResult
            {
                SuccessCount = 1,
                WarningCount = 0,
                ErrorCount = 0,
                Messages = []
            };
        }
    }

    private sealed class FakeFolderPickerService : IFolderPickerService
    {
        public string? PickFolder(string? initialDirectory)
            => initialDirectory;
    }
}
