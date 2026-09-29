namespace ExtractDB.Generated.Metadata;

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
public sealed class DatabaseDefaultAttribute : System.Attribute
{
    public DatabaseDefaultAttribute(string expression)
        => Expression = expression;

    public string Expression { get; }
}