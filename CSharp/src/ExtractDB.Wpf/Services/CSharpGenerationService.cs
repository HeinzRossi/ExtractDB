using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Generators.CSharp;
using System.IO;

namespace ExtractDB.Wpf.Services;

public sealed class CSharpGenerationService : ICSharpGenerationService
{
    private readonly CSharpDatabaseGenerator generator;
    private readonly IDataExportProviderFactory dataExportProviderFactory;

    public CSharpGenerationService(
        CSharpDatabaseGenerator generator,
        IDataExportProviderFactory dataExportProviderFactory)
    {
        this.generator = generator;
        this.dataExportProviderFactory = dataExportProviderFactory;
    }

    public async Task<GenerationResult> GenerateAsync(
        DatabaseMetadata metadata,
        GenerationRequest request,
        CancellationToken cancellationToken)
    {
        var selectedTables = request.SelectedObjects
            .Where(item => item.ObjectType == DatabaseObjectType.Table)
            .Select(item => item.Name)
            .ToArray();

        var filtered = metadata with
        {
            Tables = metadata.Tables.Where(table => IsSelected(request, DatabaseObjectType.Table, table.Schema, table.Name)).ToArray(),
            Views = metadata.Views.Where(view => IsSelected(request, DatabaseObjectType.View, view.Schema, view.Name)).ToArray(),
            Procedures = metadata.Procedures.Where(procedure => IsSelected(request, DatabaseObjectType.Procedure, procedure.Schema, procedure.Name)).ToArray(),
            Triggers = metadata.Triggers.Where(trigger => IsSelected(request, DatabaseObjectType.Trigger, trigger.Schema, trigger.Name)).ToArray(),
            Sequences = metadata.Sequences.Where(sequence => IsSelected(request, DatabaseObjectType.Sequence, sequence.Schema, sequence.Name)).ToArray()
        };

        var context = new GenerationContext
        {
            NamespaceBase = request.NamespaceBase,
            OutputDirectory = request.OutputDirectory,
            SelectedTables = selectedTables
        };

        var generationResult = generator.Generate(filtered, context);

        if (string.IsNullOrWhiteSpace(request.DataExportConfigurationPath))
        {
            return generationResult;
        }

        if (request.ConnectionOptions is null)
        {
            return Combine(
                generationResult,
                new GenerationResult
                {
                    SuccessCount = 0,
                    WarningCount = 0,
                    ErrorCount = 1,
                    Messages =
                    [
                        new GenerationMessage
                        {
                            Severity = GenerationMessageSeverity.Error,
                            ObjectType = DatabaseObjectType.Table,
                            ObjectName = "DataExport",
                            Stage = "DataExportConfig",
                            Message = "Connection options are required to export data."
                        }
                    ]
                });
        }

        var configuration = DataExportConfigurationJson.Parse(
            await File.ReadAllTextAsync(request.DataExportConfigurationPath, cancellationToken));
        var exportProvider = dataExportProviderFactory.Create(request.Provider);
        var exportResult = await exportProvider.ExportAsync(
            new DataExportRequest
            {
                ConnectionOptions = request.ConnectionOptions,
                Metadata = metadata,
                OutputDirectory = request.OutputDirectory,
                Configuration = configuration
            },
            cancellationToken);

        return Combine(generationResult, exportResult);
    }

    private static bool IsSelected(
        GenerationRequest request,
        DatabaseObjectType objectType,
        string? schema,
        string name)
        => SystemObjectFilter.IsUserObject(request.Provider, schema, name)
            && request.SelectedObjects.Any(item =>
                item.ObjectType == objectType
                && string.Equals(item.Schema, schema, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

    private static GenerationResult Combine(GenerationResult first, GenerationResult second)
    {
        var messages = first.Messages.Concat(second.Messages).ToArray();

        return new GenerationResult
        {
            SuccessCount = first.SuccessCount + second.SuccessCount,
            WarningCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Warning),
            ErrorCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Error),
            Messages = messages
        };
    }
}
