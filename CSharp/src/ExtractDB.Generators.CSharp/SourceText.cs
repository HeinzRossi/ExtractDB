using System.Text;

namespace ExtractDB.Generators.CSharp;

internal sealed class SourceText
{
    private readonly StringBuilder builder = new();
    private int indent;

    public void Indent()
        => indent++;

    public void Unindent()
        => indent = Math.Max(0, indent - 1);

    public void WriteLine()
        => builder.Append("\r\n");

    public void WriteLine(string value)
    {
        if (value.Length > 0)
        {
            builder.Append(new string(' ', indent * 4));
        }

        builder.Append(value);
        builder.Append("\r\n");
    }

    public override string ToString()
        => builder.ToString();
}
