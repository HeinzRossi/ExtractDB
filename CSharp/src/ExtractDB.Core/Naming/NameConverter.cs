using System.Text;

namespace ExtractDB.Core.Naming;

public sealed class NameConverter : INameConverter
{
    private static readonly string[] KnownAcronyms = ["CNPJ", "UUID", "CPF", "URL", "XML", "ID"];

    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
        "end"
    };

    public string ToPascalCase(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var builder = new StringBuilder();

        foreach (var rawPart in SplitParts(databaseName))
        {
            foreach (var token in SplitKnownAcronyms(rawPart))
            {
                builder.Append(ToPascalToken(token));
            }
        }

        var result = builder.Length == 0 ? "Value" : builder.ToString();
        return ReservedWords.Contains(result) ? $"{result}Value" : result;
    }

    public IReadOnlyList<string> ToUniquePascalCase(IEnumerable<string> databaseNames)
    {
        ArgumentNullException.ThrowIfNull(databaseNames);

        var usedNames = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var databaseName in databaseNames)
        {
            var baseName = ToPascalCase(databaseName);

            if (!usedNames.TryGetValue(baseName, out var count))
            {
                usedNames[baseName] = 1;
                result.Add(baseName);
                continue;
            }

            count++;
            usedNames[baseName] = count;
            result.Add($"{baseName}{count}");
        }

        return result;
    }

    private static IEnumerable<string> SplitParts(string databaseName)
    {
        var builder = new StringBuilder();

        foreach (var character in databaseName)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static IEnumerable<string> SplitKnownAcronyms(string part)
    {
        if (!part.All(char.IsUpper))
        {
            yield return part;
            yield break;
        }

        var remaining = part;

        while (remaining.Length > 0)
        {
            var acronym = KnownAcronyms.FirstOrDefault(
                candidate => remaining.StartsWith(candidate, StringComparison.Ordinal)
                    && remaining.Length > candidate.Length);

            if (acronym is null)
            {
                yield return remaining;
                yield break;
            }

            yield return acronym;
            remaining = remaining[acronym.Length..];
        }
    }

    private static string ToPascalToken(string token)
    {
        if (token.Length == 0)
        {
            return string.Empty;
        }

        var lower = token.ToLowerInvariant();
        return string.Create(lower.Length, lower, static (span, value) =>
        {
            value.AsSpan().CopyTo(span);
            span[0] = char.ToUpperInvariant(span[0]);
        });
    }
}
