using ExtractDB.Core.Metadata;
using ExtractDB.Core.Naming;
using ExtractDB.Core.Types;

namespace ExtractDB.Generators.CSharp;

public sealed class CSharpEntityCodeGenerator : IEntityCodeGenerator
{
    private readonly ICSharpTypeMapper typeMapper;
    private readonly INameConverter nameConverter;

    public CSharpEntityCodeGenerator()
        : this(new CSharpTypeMapper(), new NameConverter())
    {
    }

    public CSharpEntityCodeGenerator(
        ICSharpTypeMapper typeMapper,
        INameConverter nameConverter)
    {
        this.typeMapper = typeMapper;
        this.nameConverter = nameConverter;
    }

    public string Generate(
        TableMetadata table,
        GenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(context);

        var className = nameConverter.ToPascalCase(table.Name);
        var propertyNames = BuildColumnPropertyNames(table);
        var relationshipNames = BuildRelationshipNames(table);
        var usings = BuildUsings(table, context, propertyNames);
        var source = new SourceText();

        foreach (var usingName in usings.Order(StringComparer.Ordinal))
        {
            source.WriteLine($"using {usingName};");
        }

        if (usings.Count > 0)
        {
            source.WriteLine();
        }

        source.WriteLine($"namespace {context.NamespaceBase}.Models;");
        source.WriteLine();
        WriteTableAttribute(source, table);
        source.WriteLine($"public sealed class {className}");
        source.WriteLine("{");
        source.Indent();

        foreach (var column in table.Columns.OrderBy(column => column.OrdinalPosition))
        {
            WriteColumn(source, column, propertyNames[column.Name]);
        }

        foreach (var foreignKey in table.ForeignKeys)
        {
            if (context.IsTableSelected(foreignKey.TargetTable))
            {
                WriteNavigation(source, foreignKey, relationshipNames[foreignKey.Name], propertyNames);
            }
        }

        source.Unindent();
        source.WriteLine("}");

        return source.ToString();
    }

    private void WriteColumn(
        SourceText source,
        ColumnMetadata column,
        string propertyName)
    {
        var typeName = typeMapper.Map(column);

        source.WriteLine($"[Column(\"{Escape(column.Name)}\"{GetColumnTypeName(column)})]");

        if (column.IsPrimaryKey)
        {
            source.WriteLine("[Key]");
        }

        if (!column.IsNullable && IsReferenceType(typeName))
        {
            source.WriteLine("[Required]");
        }

        if (column.Length is > 0 && column.DbType is CommonDbType.Char or CommonDbType.VarChar or CommonDbType.Text)
        {
            source.WriteLine($"[MaxLength({column.Length.Value})]");
        }

        if (column.ValueGeneration?.Strategy == ValueGenerationStrategy.Identity)
        {
            source.WriteLine("[DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
        }

        if (column.ValueGeneration?.Strategy == ValueGenerationStrategy.Sequence
            && !string.IsNullOrWhiteSpace(column.ValueGeneration.SequenceName))
        {
            source.WriteLine($"[Sequence(\"{Escape(column.ValueGeneration.SequenceName)}\")]");
        }

        if (column.ValueGeneration?.Strategy == ValueGenerationStrategy.TriggerSequence
            && !string.IsNullOrWhiteSpace(column.ValueGeneration.SequenceName)
            && !string.IsNullOrWhiteSpace(column.ValueGeneration.TriggerName))
        {
            source.WriteLine($"[Sequence(\"{Escape(column.ValueGeneration.SequenceName)}\")]");
            source.WriteLine($"[GeneratedByTrigger(\"{Escape(column.ValueGeneration.TriggerName)}\")]");
        }

        if (column.DefaultValue is not null)
        {
            source.WriteLine($"[DatabaseDefault(\"{Escape(column.DefaultValue.RawExpression)}\")]");
        }

        if (column.IsComputed)
        {
            var expression = string.IsNullOrWhiteSpace(column.ComputedExpression)
                ? string.Empty
                : $"(\"{Escape(column.ComputedExpression)}\")";

            source.WriteLine($"[DatabaseComputed{expression}]");
        }

        source.WriteLine($"public {typeName} {propertyName} {{ get; set; }}{GetInitializer(typeName)}");
        source.WriteLine();
    }

    private void WriteNavigation(
        SourceText source,
        ForeignKeyMetadata foreignKey,
        string propertyName,
        IReadOnlyDictionary<string, string> propertyNames)
    {
        var targetType = nameConverter.ToPascalCase(foreignKey.TargetTable);
        var isNullable = foreignKey.IsNullable;

        if (foreignKey.SourceColumns.Count == 1)
        {
            source.WriteLine($"[ForeignKey(nameof({propertyNames[foreignKey.SourceColumns[0]]}))]");
        }
        else
        {
            var names = string.Join(", ", foreignKey.SourceColumns.Select(column => $"nameof({propertyNames[column]})"));
            source.WriteLine($"[CompositeForeignKey({names})]");
        }

        source.WriteLine($"public {targetType}{(isNullable ? "?" : string.Empty)} {propertyName} {{ get; set; }}{(isNullable ? string.Empty : " = null!")};");
        source.WriteLine();
    }

    private static void WriteTableAttribute(SourceText source, TableMetadata table)
    {
        var schemaPart = string.IsNullOrWhiteSpace(table.Schema)
            ? string.Empty
            : $", Schema = \"{Escape(table.Schema)}\"";

        source.WriteLine($"[Table(\"{Escape(table.Name)}\"{schemaPart})]");
    }

    private IReadOnlyDictionary<string, string> BuildColumnPropertyNames(TableMetadata table)
        => table.Columns
            .OrderBy(column => column.OrdinalPosition)
            .Zip(nameConverter.ToUniquePascalCase(table.Columns.OrderBy(column => column.OrdinalPosition).Select(column => column.Name)))
            .ToDictionary(item => item.First.Name, item => item.Second, StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, string> BuildRelationshipNames(TableMetadata table)
        => table.ForeignKeys
            .Zip(nameConverter.ToUniquePascalCase(table.ForeignKeys.Select(GetNavigationName)))
            .ToDictionary(item => item.First.Name, item => item.Second, StringComparer.OrdinalIgnoreCase);

    private string GetNavigationName(ForeignKeyMetadata foreignKey)
    {
        if (foreignKey.SourceColumns.Count == 1)
        {
            var sourceName = nameConverter.ToPascalCase(foreignKey.SourceColumns[0]);

            return sourceName.StartsWith("Id", StringComparison.Ordinal) && sourceName.Length > 2
                ? sourceName[2..]
                : sourceName;
        }

        return nameConverter.ToPascalCase(foreignKey.TargetTable);
    }

    private ISet<string> BuildUsings(
        TableMetadata table,
        GenerationContext context,
        IReadOnlyDictionary<string, string> propertyNames)
    {
        var usings = new SortedSet<string>(StringComparer.Ordinal)
        {
            "System.ComponentModel.DataAnnotations",
            "System.ComponentModel.DataAnnotations.Schema"
        };

        var needsMetadata = false;

        foreach (var column in table.Columns)
        {
            var typeName = typeMapper.Map(column);

            if (CSharpTypeMapper.RequiresSystemUsing(typeName))
            {
                usings.Add("System");
            }

            if (column.DbType == CommonDbType.Enum)
            {
                usings.Add($"{context.NamespaceBase}.Models.Enums");
            }

            needsMetadata |= column.ValueGeneration?.Strategy is ValueGenerationStrategy.Sequence or ValueGenerationStrategy.TriggerSequence;
            needsMetadata |= column.DefaultValue is not null;
            needsMetadata |= column.IsComputed;
        }

        foreach (var foreignKey in table.ForeignKeys)
        {
            needsMetadata |= foreignKey.SourceColumns.Count > 1 && context.IsTableSelected(foreignKey.TargetTable);
        }

        if (needsMetadata)
        {
            usings.Add($"{context.NamespaceBase}.Metadata");
        }

        return usings;
    }

    private static bool IsReferenceType(string typeName)
        => typeName.TrimEnd('?') is "string" or "byte[]" or "object";

    private static string GetInitializer(string typeName)
        => typeName switch
        {
            "string" => " = string.Empty;",
            "byte[]" => " = [];",
            "object" => " = new();",
            _ => string.Empty
        };

    private static string GetColumnTypeName(ColumnMetadata column)
    {
        if (column.DbType != CommonDbType.Decimal || column.Precision is null || column.Scale is null)
        {
            return string.Empty;
        }

        return $", TypeName = \"decimal({column.Precision.Value},{column.Scale.Value})\"";
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
