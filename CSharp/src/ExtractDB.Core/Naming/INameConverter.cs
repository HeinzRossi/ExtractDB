namespace ExtractDB.Core.Naming;

public interface INameConverter
{
    string ToPascalCase(string databaseName);

    IReadOnlyList<string> ToUniquePascalCase(IEnumerable<string> databaseNames);
}
