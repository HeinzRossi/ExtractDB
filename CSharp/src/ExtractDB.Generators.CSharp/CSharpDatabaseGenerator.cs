using System.Text;
using ExtractDB.Core.Contracts;
using ExtractDB.Core.Metadata;
using ExtractDB.Core.Naming;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp;

public sealed class CSharpDatabaseGenerator
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private readonly IEntityCodeGenerator entityGenerator;
    private readonly INameConverter nameConverter;

    public CSharpDatabaseGenerator()
        : this(new CSharpEntityCodeGenerator(), new NameConverter())
    {
    }

    public CSharpDatabaseGenerator(
        IEntityCodeGenerator entityGenerator,
        INameConverter nameConverter)
    {
        this.entityGenerator = entityGenerator;
        this.nameConverter = nameConverter;
    }

    public GenerationResult Generate(
        DatabaseMetadata database,
        GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(context);

        Directory.CreateDirectory(context.OutputDirectory);

        var messages = new List<GenerationMessage>();
        var successCount = 0;

        foreach (var table in database.Tables.Where(table => context.IsTableSelected(table.Name)))
        {
            try
            {
                foreach (var column in table.Columns.Where(CSharpTypeMapper.RequiresUnknownWarning))
                {
                    messages.Add(CreateMessage(
                        GenerationMessageSeverity.Warning,
                        DatabaseObjectType.Table,
                        table.Schema,
                        table.Name,
                        "CSharpTypeMapper",
                        $"Column '{column.Name}' has unknown database type '{column.NativeType}'."));
                }

                WriteFile(GetModelPath(context, table.Name), entityGenerator.Generate(table, context));
                successCount++;
            }
            catch (Exception ex)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Error,
                    DatabaseObjectType.Table,
                    table.Schema,
                    table.Name,
                    "EntityGenerator",
                    ex.Message));
            }
        }

        successCount += GenerateEnums(database, context, messages);
        successCount += GenerateScripts(database.Views, context, "Views", DatabaseObjectType.View, messages);
        successCount += GenerateScripts(database.Procedures, context, "Procedures", DatabaseObjectType.Procedure, messages);
        successCount += GenerateScripts(database.Triggers, context, "Triggers", DatabaseObjectType.Trigger, messages);
        successCount += GenerateScripts(database.Sequences, context, "Sequences", DatabaseObjectType.Sequence, messages);
        successCount += GenerateMetadataAttributes(database, context, messages);

        return new GenerationResult
        {
            SuccessCount = successCount,
            WarningCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Warning),
            ErrorCount = messages.Count(message => message.Severity == GenerationMessageSeverity.Error),
            Messages = messages
        };
    }

    private int GenerateEnums(
        DatabaseMetadata database,
        GenerationContext context,
        ICollection<GenerationMessage> messages)
    {
        var count = 0;

        foreach (var enumMetadata in database.Enums)
        {
            try
            {
                WriteFile(GetEnumPath(context, enumMetadata.Name), GenerateEnum(enumMetadata, context));
                count++;
            }
            catch (Exception ex)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Error,
                    DatabaseObjectType.Enum,
                    enumMetadata.Schema,
                    enumMetadata.Name,
                    "EnumGenerator",
                    ex.Message));
            }
        }

        return count;
    }

    private int GenerateScripts<T>(
        IEnumerable<T> objects,
        GenerationContext context,
        string folderName,
        DatabaseObjectType objectType,
        ICollection<GenerationMessage> messages)
        where T : class
    {
        var count = 0;

        foreach (var item in objects)
        {
            var (schema, name, sql) = GetSqlObject(item);

            try
            {
                WriteFile(GetScriptPath(context, folderName, schema, name), sql);
                count++;
            }
            catch (Exception ex)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Error,
                    objectType,
                    schema,
                    name,
                    "SqlExport",
                    ex.Message));
            }
        }

        return count;
    }

    private int GenerateMetadataAttributes(
        DatabaseMetadata database,
        GenerationContext context,
        ICollection<GenerationMessage> messages)
    {
        var attributes = GetRequiredAttributes(database, context);
        var count = 0;

        foreach (var attribute in attributes)
        {
            try
            {
                WriteFile(
                    Path.Combine(context.OutputDirectory, "Metadata", $"{attribute.FileName}.cs"),
                    attribute.Source.Replace("__NAMESPACE__", $"{context.NamespaceBase}.Metadata", StringComparison.Ordinal));
                count++;
            }
            catch (Exception ex)
            {
                messages.Add(CreateMessage(
                    GenerationMessageSeverity.Error,
                    DatabaseObjectType.Table,
                    null,
                    attribute.FileName,
                    "AttributeWriter",
                    ex.Message));
            }
        }

        return count;
    }

    private string GenerateEnum(EnumMetadata enumMetadata, GenerationContext context)
    {
        var names = nameConverter.ToUniquePascalCase(enumMetadata.Values);
        var source = new SourceText();

        source.WriteLine($"namespace {context.NamespaceBase}.Models.Enums;");
        source.WriteLine();
        source.WriteLine($"public enum {nameConverter.ToPascalCase(enumMetadata.Name)}");
        source.WriteLine("{");
        source.Indent();

        for (var i = 0; i < names.Count; i++)
        {
            source.WriteLine(i == names.Count - 1 ? names[i] : $"{names[i]},");
        }

        source.Unindent();
        source.WriteLine("}");

        return source.ToString();
    }

    private IReadOnlyList<MetadataAttributeSource> GetRequiredAttributes(
        DatabaseMetadata database,
        GenerationContext context)
    {
        var selectedTables = database.Tables.Where(table => context.IsTableSelected(table.Name)).ToArray();
        var attributes = new List<MetadataAttributeSource>();

        if (selectedTables.SelectMany(table => table.Columns).Any(column =>
                column.ValueGeneration?.Strategy is ValueGenerationStrategy.Sequence or ValueGenerationStrategy.TriggerSequence))
        {
            attributes.Add(new("SequenceAttribute", """
                namespace __NAMESPACE__;

                [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
                public sealed class SequenceAttribute : System.Attribute
                {
                    public SequenceAttribute(string name)
                        => Name = name;

                    public string Name { get; }
                }
                """));
        }

        if (selectedTables.SelectMany(table => table.Columns).Any(column => column.ValueGeneration?.Strategy == ValueGenerationStrategy.TriggerSequence))
        {
            attributes.Add(new("GeneratedByTriggerAttribute", """
                namespace __NAMESPACE__;

                [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
                public sealed class GeneratedByTriggerAttribute : System.Attribute
                {
                    public GeneratedByTriggerAttribute(string name)
                        => Name = name;

                    public string Name { get; }
                }
                """));
        }

        if (selectedTables.SelectMany(table => table.Columns).Any(column => column.DefaultValue is not null))
        {
            attributes.Add(new("DatabaseDefaultAttribute", """
                namespace __NAMESPACE__;

                [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
                public sealed class DatabaseDefaultAttribute : System.Attribute
                {
                    public DatabaseDefaultAttribute(string expression)
                        => Expression = expression;

                    public string Expression { get; }
                }
                """));
        }

        if (selectedTables.SelectMany(table => table.Columns).Any(column => column.IsComputed))
        {
            attributes.Add(new("DatabaseComputedAttribute", """
                namespace __NAMESPACE__;

                [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
                public sealed class DatabaseComputedAttribute : System.Attribute
                {
                    public DatabaseComputedAttribute()
                    {
                    }

                    public DatabaseComputedAttribute(string expression)
                        => Expression = expression;

                    public string? Expression { get; }
                }
                """));
        }

        if (selectedTables.SelectMany(table => table.ForeignKeys).Any(foreignKey =>
                foreignKey.SourceColumns.Count > 1 && context.IsTableSelected(foreignKey.TargetTable)))
        {
            attributes.Add(new("CompositeForeignKeyAttribute", """
                namespace __NAMESPACE__;

                [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
                public sealed class CompositeForeignKeyAttribute : System.Attribute
                {
                    public CompositeForeignKeyAttribute(params string[] propertyNames)
                        => PropertyNames = propertyNames;

                    public System.Collections.Generic.IReadOnlyList<string> PropertyNames { get; }
                }
                """));
        }

        return attributes;
    }

    private string GetModelPath(GenerationContext context, string tableName)
        => Path.Combine(context.OutputDirectory, "Models", $"{nameConverter.ToPascalCase(tableName)}.cs");

    private string GetEnumPath(GenerationContext context, string enumName)
        => Path.Combine(context.OutputDirectory, "Models", "Enums", $"{nameConverter.ToPascalCase(enumName)}.cs");

    private static string GetScriptPath(
        GenerationContext context,
        string folderName,
        string? schema,
        string name)
    {
        var parts = string.IsNullOrWhiteSpace(schema)
            ? [context.OutputDirectory, "Scripts", folderName, $"{name}.sql"]
            : new[] { context.OutputDirectory, "Scripts", folderName, schema, $"{name}.sql" };

        return Path.Combine(parts);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.ReplaceLineEndings("\r\n"), Utf8NoBom);
    }

    private static (string? Schema, string Name, string Sql) GetSqlObject<T>(T item)
        => item switch
        {
            ViewMetadata view => (view.Schema, view.Name, view.Sql),
            ProcedureMetadata procedure => (procedure.Schema, procedure.Name, procedure.Sql),
            TriggerMetadata trigger => (trigger.Schema, trigger.Name, trigger.Sql),
            SequenceMetadata sequence => (sequence.Schema, sequence.Name, sequence.Sql),
            _ => throw new InvalidOperationException($"Unsupported SQL object type {typeof(T).Name}.")
        };

    private static GenerationMessage CreateMessage(
        GenerationMessageSeverity severity,
        DatabaseObjectType objectType,
        string? schema,
        string objectName,
        string stage,
        string message)
        => new()
        {
            Severity = severity,
            ObjectType = objectType,
            Schema = schema,
            ObjectName = objectName,
            Stage = stage,
            Message = message
        };

    private sealed record MetadataAttributeSource(
        string FileName,
        string Source);
}
