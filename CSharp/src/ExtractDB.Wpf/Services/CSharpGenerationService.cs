using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Types;
using ExtractDB.Generators.CSharp;

namespace ExtractDB.Wpf.Services;

public sealed class CSharpGenerationService : ICSharpGenerationService
{
    private readonly CSharpDatabaseGenerator generator;

    public CSharpGenerationService(CSharpDatabaseGenerator generator)
        => this.generator = generator;

    public GenerationResult Generate(
        DatabaseMetadata metadata,
        GenerationRequest request)
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
            Triggers = metadata.Triggers.Where(trigger => IsSelected(request, DatabaseObjectType.Trigger, trigger.Schema, trigger.Name)).ToArray()
        };

        var context = new GenerationContext
        {
            NamespaceBase = request.NamespaceBase,
            OutputDirectory = request.OutputDirectory,
            SelectedTables = selectedTables
        };

        return generator.Generate(filtered, context);
    }

    private static bool IsSelected(
        GenerationRequest request,
        DatabaseObjectType objectType,
        string? schema,
        string name)
        => request.SelectedObjects.Any(item =>
            item.ObjectType == objectType
            && string.Equals(item.Schema, schema, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
}
