using System.Text;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// A StringBuilder that keeps track of indentation, so emitters can be written as nested blocks
/// rather than as strings carrying their own spaces.
/// </summary>
internal sealed class SourceWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public SourceWriter Line()
    {
        _builder.AppendLine();
        return this;
    }

    public SourceWriter Line(string text)
    {
        if (text.Length > 0)
            _builder.Append(' ', _indent * 4);

        _builder.AppendLine(text);
        return this;
    }

    public Block Braces(string header)
    {
        // An empty header is a block whose declaration was already written, most often because a
        // preprocessor directive had to sit between the two.
        if (header.Length > 0)
            Line(header);

        Line("{");
        _indent++;
        return new Block(this);
    }

    public override string ToString() => _builder.ToString();

    internal readonly struct Block(SourceWriter writer) : System.IDisposable
    {
        public void Dispose()
        {
            writer._indent--;
            writer.Line("}");
        }
    }
}
